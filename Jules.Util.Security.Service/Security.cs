using Jules.Util.Security.Contracts;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Jules.Util.Security.Service
{
    /// <summary>
    /// Implements the <see cref="ISecurity"/> interface to handle user authentication, registration, role management, and token generation.
    /// </summary>
    public class Security : ISecurity
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IUserContext userContext;
        private readonly IJwtService jwtService;

        private readonly string _secretKey = "your_secret_key_here"; // Store securely in a config file or secret store

        /// <summary>
        /// Initializes a new instance of the <see cref="Security"/> class with the specified dependencies.
        /// </summary>
        /// <param name="userManager">The UserManager instance to manage users.</param>
        /// <param name="roleManager">The RoleManager instance to manage roles.</param>
        /// <param name="userContext">The IUserContext instance to access user-specific information.</param>
        /// <param name="jwtService">The JwtService instance to generate JWT tokens.</param>
        public Security(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUserContext userContext,
            IJwtService jwtService)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            this.userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            this.jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        }

        /// <inheritdoc />
        public async Task<bool> Register(string userName, string userPassword)
        {
            // Check if the username already exists in the system
            var existingUser = await userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                throw new ArgumentException("The username is already taken.");
            }

            // Create a new user
            var user = new ApplicationUser
            {
                UserName = userName
            };

            // Register the user with the provided password
            var result = await userManager.CreateAsync(user, userPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("User registration failed.");
            }

            // Optionally, assign a default role to the user (e.g., "User")
            await userManager.AddToRoleAsync(user, "user");

            return true;
        }

        /// <inheritdoc />
        public async Task<string> Login(string userName, string userPassword, string userRole)
        {
            // Find the user by username and check the password
            var user = await userManager.FindByNameAsync(userName);
            if (user == null || !await userManager.CheckPasswordAsync(user, userPassword))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Create claims for the user
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, userRole),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Generate and return a JWT token using the claims
            return jwtService.GenerateToken(claims);
        }

        /// <inheritdoc />
        public async Task<bool> AssignRole(string userName, string userRole)
        {
            // Ensure the user has admin privileges before assigning roles
            if (!await userContext.IsInRole("admin"))
            {
                throw new UnauthorizedAccessException("User has no Admin privileges.");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(userRole))
            {
                throw new ArgumentException("Username and Role are required.");
            }

            // Find the user and role
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var role = await roleManager.FindByNameAsync(userRole);
            if (role == null)
            {
                throw new UnauthorizedAccessException("Role does not exist.");
            }

            // Assign the role to the user
            var result = await userManager.AddToRoleAsync(user, userRole);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Failed to assign role.");
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteUser(string userName)
        {
            // Ensure the user has admin privileges before deleting users
            if (!await userContext.IsInRole("admin"))
            {
                throw new UnauthorizedAccessException("User has no Admin privileges.");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Username is required.");
            }

            // Find the user to delete
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            // Delete the user
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Failed to delete user.");
            }

            return true;
        }
    }
}