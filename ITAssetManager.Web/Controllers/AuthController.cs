// Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ITAssetManager.Web.Services;
using ITAssetManager.Web.Models.ViewModels;
using System.Threading.Tasks;
using System.Linq;

namespace ITAssetManager.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(
            UserManager<IdentityUser> userManager,
            IJwtTokenService jwtTokenService,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid input data",
                    Token = null
                });
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "User with this email already exists",
                        Token = null
                    });
                }

                // Create new user
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true // For demo purposes
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "User");

                    // Generate token
                    var token = await _jwtTokenService.GenerateTokenAsync(user);

                    return Ok(new AuthResponse
                    {
                        Success = true,
                        Message = "User registered successfully",
                        Token = token,
                        UserId = user.Id,
                        Email = user.Email,
                        Roles = new List<string> { "User" }
                    });
                }

                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Token = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Token = null
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid input data",
                    Token = null
                });
            }

            try
            {
                // Validate user credentials
                var user = await _jwtTokenService.ValidateUserAsync(model.Email, model.Password);

                if (user == null)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Token = null
                    });
                }

                // Generate token
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Token = null
                });
            }
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken()
        {
            // This endpoint can be used to validate tokens
            // The [Authorize] attribute will handle validation
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles
            });
        }
    }
}
