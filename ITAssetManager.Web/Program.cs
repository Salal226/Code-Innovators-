using ITAssetManager.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 1) Services
// -------------------------------------------------------
builder.Services.AddControllersWithViews();

// 1a) Database (Pomelo + MySQL tuning)
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    options.UseMySql(
      cs!,
      ServerVersion.AutoDetect(cs),
      mysql =>
      {
          // Retry transient failures (recommended)
          mysql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
      });

});

// 1b) Identity (users/roles)
builder.Services
    .AddDefaultIdentity<IdentityUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// If you scaffolded Identity UI:
builder.Services.AddRazorPages();

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

app.UseAuthentication();
app.UseAuthorization();

// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// -------------------------------------------------------
// 3) Migrate & seed (DEV convenience)
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    // ✅ Ensure schema exists (prevents “AspNetRoles doesn’t exist”)
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

        foreach (var role in new[] { "Admin", "Tech", "Viewer" })
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        var adminEmail = "admin@college.edu";
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var create = await userMgr.CreateAsync(admin, "Admin#12345"); // change after first login
            if (create.Succeeded)
                await userMgr.AddToRoleAsync(admin, "Admin");
        }

        await db.SaveChangesAsync();
    }
}

// -------------------------------------------------------
app.Run();
