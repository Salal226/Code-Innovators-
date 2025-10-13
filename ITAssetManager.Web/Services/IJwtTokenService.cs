using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ITAssetManager.Web.Services
{
    /// <summary>
    /// Service for generating and validating JWT tokens for API authentication
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        /// <param name="user">The Identity user to generate a token for</param>
        /// <returns>A JWT token string</returns>
        Task<string> GenerateTokenAsync(IdentityUser user);

        /// <summary>
        /// Validates user credentials and returns the user if authentication succeeds
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <param name="password">The user's password</param>
        /// <returns>IdentityUser object if credentials are valid, null if authentication fails</returns>
        Task<IdentityUser> ValidateUserAsync(string email, string password);

        /// <summary>
        /// Validates a JWT token and returns the claims principal
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Gets the expiration time for tokens (used for API responses)
        /// </summary>
        DateTime GetTokenExpiration();
    }
}
