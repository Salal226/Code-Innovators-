// Services/JwtTokenService.cs
using ITAssetManager.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ITAssetManager.Web.Services
{
    /// <summary>
    /// Service for generating and validating JWT tokens for API authentication
    /// Integrates with ASP.NET Core Identity for user management
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token for the specified user with their roles and claims
        /// </summary>
        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            try
            {
                // Get JWT configuration settings
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
                var issuer = jwtSettings["Issuer"] ?? "ITAssetManager.CodeInnovators";
                var audience = jwtSettings["Audience"] ?? "ITAssetManagerUsers";

                // Support both ExpiryMinutes (from Program.cs) and ExpirationInMinutes (legacy)
                var expirationMinutes = int.Parse(
                    jwtSettings["ExpiryMinutes"] ??
                    jwtSettings["ExpirationInMinutes"] ??
                    "480");

                // Validate secret key length
                if (secretKey.Length < 32)
                {
                    throw new InvalidOperationException("JWT Secret Key must be at least 32 characters long");
                }

                // Create signing credentials
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Get user roles from Identity
                var roles = await _userManager.GetRolesAsync(user);

                // Create token claims
                var claims = new List<Claim>
                {
                    // Standard JWT claims
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64),
                    
                    // Custom claims for your application
                    new Claim("user_id", user.Id),
                    new Claim("email", user.Email ?? ""),
                    new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLower())
                };

                // Add FullName claim if available (from ApplicationUser)
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim("FullName", user.FullName));
                    claims.Add(new Claim("full_name", user.FullName));
                }

                // Add CreatedAt claim (from ApplicationUser)
                claims.Add(new Claim("created_at", user.CreatedAt.ToString("O")));

                // Add role claims for authorization
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                    claims.Add(new Claim("role", role)); // Alternative role claim format
                }

                // Create the JWT token
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("JWT token generated successfully for user {UserId} ({Email}) with roles: {Roles}",
                    user.Id, user.Email, string.Join(", ", roles));

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to generate JWT token", ex);
            }
        }

        /// <summary>
        /// Validates a JWT token and returns the claims principal if valid
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token validation failed: Token is null or empty");
                return null;
            }

            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
                var issuer = jwtSettings["Issuer"] ?? "ITAssetManager.CodeInnovators";
                var audience = jwtSettings["Audience"] ?? "ITAssetManagerUsers";

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // No tolerance for clock differences
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Verify it's a JWT token with correct algorithm
                if (validatedToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogDebug("JWT token validated successfully");
                    return principal;
                }

                _logger.LogWarning("JWT token validation failed: Invalid algorithm");
                return null;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("JWT token validation failed: Token has expired");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("JWT token validation failed: Invalid signature");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Gets the expiration time for new tokens based on configuration
        /// </summary>
        public DateTime GetTokenExpiration()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            // Support both ExpiryMinutes (from Program.cs) and ExpirationInMinutes (legacy)
            var expirationMinutes = int.Parse(
                jwtSettings["ExpiryMinutes"] ??
                jwtSettings["ExpirationInMinutes"] ??
                "480");

            return DateTime.UtcNow.AddMinutes(expirationMinutes);
        }
    }
}