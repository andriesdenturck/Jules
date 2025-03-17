using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Jules.Util.Security.Service
{
    /// <summary>
    /// Helper service for generating JWT tokens.
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly string _secretKey = "Le Tour du monde en quatre-vingts jours"; // Store securely in a config file or secret store

        /// <summary>
        /// Generates a JWT token based on the provided claims.
        /// </summary>
        /// <param name="claims">The claims to include in the token.</param>
        /// <returns>A signed JWT token as a string.</returns>
        public string GenerateToken(Claim[] claims)
        {
            // Create a symmetric security key using the secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the token with the claims, expiration, and signing credentials
            var token = new JwtSecurityToken(
                issuer: "your_issuer",
                audience: "your_audience",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            // Return the serialized token string
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
    }
}