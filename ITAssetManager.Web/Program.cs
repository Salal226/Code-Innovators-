using ITAssetManager.Web.Data;
using ITAssetManager.Web.Models;
using ITAssetManager.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 1) Services
// -------------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// 1a) Database - SQL Server with retry logic
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(cs!, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// 1b) Identity with ApplicationUser
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Sign-in settings
        options.SignIn.RequireConfirmedAccount = false;

        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Cookie settings for web UI
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// 1c) JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var issuer = jwtSettings["Issuer"] ?? "ITAssetManager.CodeInnovators";
var audience = jwtSettings["Audience"] ?? "ITAssetManagerUsers";

// Add JWT Authentication scheme
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // Set to true in production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidAudience = audience,
            ValidIssuer = issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Register JWT Token Service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// -------------------------------------------------------
// 1d) AUTHORIZATION POLICIES - Role-Based Access Control
// -------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    // Administrator Only - Full system access
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrator"));

    // Administrator or Developer - Technical access
    options.AddPolicy("AdminOrDeveloper", policy =>
        policy.RequireRole("Administrator", "Developer"));

    // All Authenticated Users - Basic access
    options.AddPolicy("AllUsers", policy =>
        policy.RequireAuthenticatedUser());

    // Specific Feature Policies
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole("Administrator"));

    options.AddPolicy("CanManageAssets", policy =>
        policy.RequireRole("Administrator", "Developer"));

    options.AddPolicy("CanViewAssets", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CanManageTickets", policy =>
        policy.RequireRole("Administrator", "Developer"));

    options.AddPolicy("CanCreateTickets", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CanAccessReports", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CanManageSettings", policy =>
        policy.RequireRole("Administrator"));
});

var app = builder.Build();

// -------------------------------------------------------
// 2) Middleware pipeline
// -------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // Handles both cookies AND JWT
app.UseAuthorization();  // Enforces policies

// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// -------------------------------------------------------
// 3) Migrate & Seed
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure database is created and migrations are applied
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        // Seed data
        logger.LogInformation("Seeding database...");

        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles matching Product Backlog requirements
        var roles = new[] { "Administrator", "Developer", "GeneralStaff" };
        foreach (var roleName in roles)
        {
            if (!await roleMgr.RoleExistsAsync(roleName))
            {
                logger.LogInformation($"Creating role: {roleName}");
                await roleMgr.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Administrator user
        var adminEmail = "admin@codeinnovators.com";
        var adminUser = await userMgr.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            logger.LogInformation("Creating Administrator user...");
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userMgr.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userMgr.AddToRoleAsync(adminUser, "Administrator");
                logger.LogInformation("Administrator user created successfully.");
            }
            else
            {
                logger.LogError("Failed to create Administrator user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Seed Developer user
        var devEmail = "developer@codeinnovators.com";
        var devUser = await userMgr.FindByEmailAsync(devEmail);
        if (devUser == null)
        {
            logger.LogInformation("Creating Developer user...");
            devUser = new ApplicationUser
            {
                UserName = devEmail,
                Email = devEmail,
                FullName = "Developer User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userMgr.CreateAsync(devUser, "Dev@123");
            if (result.Succeeded)
            {
                await userMgr.AddToRoleAsync(devUser, "Developer");
                logger.LogInformation("Developer user created successfully.");
            }
        }

        // Seed General Staff user
        var staffEmail = "staff@codeinnovators.com";
        var staffUser = await userMgr.FindByEmailAsync(staffEmail);
        if (staffUser == null)
        {
            logger.LogInformation("Creating GeneralStaff user...");
            staffUser = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                FullName = "General Staff User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userMgr.CreateAsync(staffUser, "Staff@123");
            if (result.Succeeded)
            {
                await userMgr.AddToRoleAsync(staffUser, "GeneralStaff");
                logger.LogInformation("GeneralStaff user created successfully.");
            }
        }

        // Seed sample data (only in development)
        if (app.Environment.IsDevelopment())
        {
            // People
            if (!db.People.Any())
            {
                logger.LogInformation("Seeding People...");
                db.People.AddRange(
                    new Person { FullName = "Salal Alsalami", Email = "salal@codeinnovators.com" },
                    new Person { FullName = "Safroot", Email = "safroot@codeinnovators.com" }
                );
            }

            // Locations
            if (!db.Locations.Any())
            {
                logger.LogInformation("Seeding Locations...");
                db.Locations.AddRange(
                    new Location { Building = "Building A", Room = "101", LabNumber = null },
                    new Location { Building = "Lab", Room = "B1001", LabNumber = "21" }
                );
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Sample data seeded successfully.");
        }

        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");

        // In development, show detailed error
        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine($"Database seeding error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        throw; // Re-throw to prevent app from starting with incomplete setup
    }
}

// -------------------------------------------------------
app.Run();