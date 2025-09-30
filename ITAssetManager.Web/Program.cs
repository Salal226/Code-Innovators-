using ITAssetManager.Web.Data;
using ITAssetManager.Web.Services; // For JWT service
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT
using Microsoft.IdentityModel.Tokens; // For JWT
using Microsoft.EntityFrameworkCore;
using System.Text; // For encoding

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 1) Services
// -------------------------------------------------------
builder.Services.AddControllersWithViews();

// 1a) Database - UPDATED: Changed from MySQL to SQL Server
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(cs!, sqlServerOptions =>
    {
        // Enable retry on failure for SQL Server
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// 1b) Identity (users/roles) - UNCHANGED
builder.Services
    .AddDefaultIdentity<IdentityUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// 1c) JWT Configuration - UNCHANGED
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var issuer = jwtSettings["Issuer"] ?? "ITAssetManager.College";
var audience = jwtSettings["Audience"] ?? "ITAssetManagerUsers";

// Add JWT Authentication (in addition to existing Identity cookies)
builder.Services.AddAuthentication(options =>
{
    // Keep Identity cookies as default for web UI
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer("Bearer", options => // Add JWT scheme as "Bearer"
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters()
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

// If you scaffolded Identity UI
builder.Services.AddRazorPages();

var app = builder.Build();

// -------------------------------------------------------
// 2) Middleware pipeline - UNCHANGED
// -------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // This now handles both cookies AND JWT
app.UseAuthorization();

// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// -------------------------------------------------------
// 3) Migrate & seed (DEV convenience) - UNCHANGED
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    try
    {
        // ✅ Ensure schema exists (prevents "AspNetRoles doesn't exist")
        await db.Database.MigrateAsync();

        // Seed only in Dev to avoid accidental prod data
        if (app.Environment.IsDevelopment())
        {
            // People
            if (!db.People.Any())
            {
                db.People.AddRange(
                    new ITAssetManager.Web.Models.Person { FullName = "Alice Nguyen", Email = "alice@college.edu" },
                    new ITAssetManager.Web.Models.Person { FullName = "Bob Kumar", Email = "bob@college.edu" }
                );
            }

            // Locations
            if (!db.Locations.Any())
            {
                db.Locations.AddRange(
                    new ITAssetManager.Web.Models.Location { Building = "A", Room = "101" },
                    new ITAssetManager.Web.Models.Location { Building = "Lab", Room = "3" }
                );
            }

            // Roles + Admin user
            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<IdentityUser>>();

            // Create roles if they don't exist
            foreach (var role in new[] { "Admin", "Tech", "Viewer", "Staff" })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }

            // Create admin user if doesn't exist
            var adminEmail = "admin@college.edu";
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var createResult = await userMgr.CreateAsync(admin, "Admin#12345"); // change after first login
                if (createResult.Succeeded)
                {
                    await userMgr.AddToRoleAsync(admin, "Admin");
                }
            }

            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        // Log the exception in development
        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine($"Database seeding error: {ex.Message}");
        }
    }
}

// -------------------------------------------------------
app.Run();