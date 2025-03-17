using Jules.Util.Security.Contracts;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Jules.Util.Security
{
    /// <summary>
    /// Provides methods to interact with the current user's identity, claims, and roles.
    /// </summary>
    public class UserContext : IUserContext
    {
        // The UserManager is used for interacting with ASP.NET Core Identity and handling user-related tasks.
        private readonly UserManager<ApplicationUser> _userManager;

        // The IHttpContextAccessor is used to access the current HTTP context and user claims.
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContext"/> class.
        /// </summary>
        /// <param name="userManager">The UserManager to interact with Identity users.</param>
        /// <param name="httpContextAccessor">The IHttpContextAccessor to access the current HttpContext.</param>
        public UserContext(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the <see cref="ClaimsPrincipal"/> of the current user.
        /// The ClaimsPrincipal contains information about the user's identity and claims (e.g., username, roles).
        /// </summary>
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        /// <inheritdoc/>
        public string UserName => this.User.Identity.Name;

        /// <summary>
        /// Gets the UserId of the current user.
        /// Extracts the UserId from the claims, assuming it's stored under the "userId" claim.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the "userId" claim is missing or invalid.</exception>
        public Guid UserId
        {
            get
            {
                // Extracts the UserId from the claims, assuming it's stored in the claim "userId"
                var userIdClaim = User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    throw new InvalidOperationException("UserId claim not found or invalid.");
                }

                return userId;
            }
        }

        /// <summary>
        /// Checks if the current user is in a specific role.
        /// Uses ASP.NET Core Identity to check the user's role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user is in the specified role, otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown if the role is null or empty.</exception>
        public async Task<bool> IsInRole(string role)
        {
            // Validate that the role is not null or empty
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role must be provided.", nameof(role));

            // Get the current user using the UserManager
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return false;

            // Check if the user is in the specified role
            return await _userManager.IsInRoleAsync(user, role);
        }
    }
}