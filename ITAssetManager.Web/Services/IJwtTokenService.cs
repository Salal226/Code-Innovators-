using ITAssetManager.Web.Models;
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
        /// <param name="user">The ApplicationUser to generate a token for</param>
        /// <returns>A JWT token string</returns>
        Task<string> GenerateTokenAsync(ApplicationUser user);

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