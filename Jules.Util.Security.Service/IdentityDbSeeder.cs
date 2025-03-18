using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Identity;

namespace Jules.Util.Security;

public class IdentityDbSeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed users
        await SeedUsersAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roleNames = new[] { "admin", "user" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var role = new ApplicationRole(roleName);
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@example.com",
        };

        var existingUser = await userManager.FindByEmailAsync(adminUser.Email);
        if (existingUser == null)
        {
            await userManager.CreateAsync(adminUser, "P@ssw0rd");

            // Assign roles
            await userManager.AddToRoleAsync(adminUser, "admin");
            await userManager.AddToRoleAsync(adminUser, "user");
        }

        var regularUser = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com",
        };

        existingUser = await userManager.FindByEmailAsync(regularUser.Email);
        if (existingUser == null)
        {
            await userManager.CreateAsync(regularUser, "P@ssw0rd");

            // Assign role
            await userManager.AddToRoleAsync(regularUser, "User");
        }
    }
}