using System.Security.Claims;

namespace Jules.Util.Security.Service
{
    /// <summary>
    /// Interface for JSON Web Token (JWT) operations.
    /// This interface defines methods for generating a JWT token based on claims.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token based on the provided claims.
        /// The method takes an array of <see cref="Claim"> objects, which represent the user-specific data (e.g., username, roles).
        /// It returns a string containing the generated JWT token.
        /// </summary>
        /// <param name="claims">An array of <see cref="Claim"> objects representing the user data and associated roles or permissions.</param>
        /// <returns>A string containing the generated JWT token.</returns>
        string GenerateToken(Claim[] claims);
    }
}