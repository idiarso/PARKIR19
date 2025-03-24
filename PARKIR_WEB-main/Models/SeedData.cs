using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkIRC.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParkIRC.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
                
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            // Create roles if they don't exist
            await CreateRolesAsync(roleManager);
            
            // Create default admin and staff users
            await CreateUsersAsync(userManager);
        }
        
        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Create roles if they don't exist
            string[] roleNames = { "Admin", "Staff", "User" };
            
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        
        private static async Task CreateUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Create a default admin user
            if (await userManager.FindByEmailAsync("admin@parking.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@parking.com",
                    Email = "admin@parking.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };
                
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            
            // Create a default staff user
            if (await userManager.FindByEmailAsync("staff@parking.com") == null)
            {
                var staffUser = new ApplicationUser
                {
                    UserName = "staff@parking.com",
                    Email = "staff@parking.com",
                    FullName = "Staff Member",
                    EmailConfirmed = true
                };
                
                var result = await userManager.CreateAsync(staffUser, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "Staff");
                }
            }
        }
    }
} 