using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace ITAssetManager.Web.Data
{
    public static class SeedData
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Create roles
            string[] roleNames = { "Administrator", "Developer", "GeneralStaff" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default admin user
            var adminEmail = "admin@codeinnovators.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Administrator");
                }
            }

            // Create default developer user
            var devEmail = "developer@codeinnovators.com";
            var devUser = await userManager.FindByEmailAsync(devEmail);

            if (devUser == null)
            {
                var newDev = new ApplicationUser
                {
                    UserName = devEmail,
                    Email = devEmail,
                    FullName = "Developer User",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(newDev, "Dev@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newDev, "Developer");
                }
            }

            // Create default general staff user
            var staffEmail = "staff@codeinnovators.com";
            var staffUser = await userManager.FindByEmailAsync(staffEmail);

            if (staffUser == null)
            {
                var newStaff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = "General Staff",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(newStaff, "Staff@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newStaff, "GeneralStaff");
                }
            }
        }
    }
}