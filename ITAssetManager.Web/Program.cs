using ITAssetManager.Web.Data;
using ITAssetManager.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1️⃣ Configure Services
// ======================================================
builder.Services.AddControllersWithViews(options =>
{
    // Apply global authorize filter by default
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddRazorPagesOptions(options =>
{
    // Allow anonymous access to Identity pages
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
});

builder.Services.AddRazorPages();

// --------------------- DATABASE ------------------------
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(cs, sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
});

// --------------------- IDENTITY ------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // required for /Identity pages

// --------------------- JWT (for APIs) -------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var issuer = jwtSettings["Issuer"] ?? "ITAssetManager.College";
var audience = jwtSettings["Audience"] ?? "ITAssetManagerUsers";

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// --------------------- CUSTOM SERVICES -----------------
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IBackupService, SqlBackupService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

// ======================================================
// 2️⃣ Build
// ======================================================
var app = builder.Build();

// ======================================================
// 3️⃣ Middleware
// ======================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ======================================================
// 4️⃣ Routes
// ======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers();

// ======================================================
// 5️⃣ Migrate & Seed
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    try
    {
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            await db.Database.MigrateAsync();

        if (app.Environment.IsDevelopment())
        {
            if (!db.People.Any())
            {
                db.People.AddRange(
                    new ITAssetManager.Web.Models.Person { FullName = "Alice Nguyen", Email = "alice@college.edu" },
                    new ITAssetManager.Web.Models.Person { FullName = "Bob Kumar", Email = "bob@college.edu" }
                );
            }

            if (!db.Locations.Any())
            {
                db.Locations.AddRange(
                    new ITAssetManager.Web.Models.Location { Building = "A", Room = "101" },
                    new ITAssetManager.Web.Models.Location { Building = "Lab", Room = "3" }
                );
            }

            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<IdentityUser>>();

            foreach (var role in new[] { "Admin", "Tech", "Viewer", "Staff", "User" })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = "admin@college.edu";
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var res = await userMgr.CreateAsync(admin, "Admin#12345");
                if (res.Succeeded)
                    await userMgr.AddToRoleAsync(admin, "Admin");
            }

            var testEmail = "test@example.com";
            var testUser = await userMgr.FindByEmailAsync(testEmail);
            if (testUser == null)
            {
                testUser = new IdentityUser
                {
                    UserName = testEmail,
                    Email = testEmail,
                    EmailConfirmed = true
                };
                var res = await userMgr.CreateAsync(testUser, "Test123!");
                if (res.Succeeded)
                    await userMgr.AddToRoleAsync(testUser, "User");
            }

            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ DB Seeding Error: {ex.Message}");
    }
}

Console.WriteLine("✅ Application starting successfully...");
app.Run();
