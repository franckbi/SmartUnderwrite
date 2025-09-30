using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Models.Auth;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Api.Constants;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate role
            var validRoles = new[] { Roles.Admin, Roles.Underwriter, Roles.Affiliate };
            if (!validRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role specified" });
            }

            // Validate affiliate requirement for affiliate users
            if (request.Role == Roles.Affiliate && !request.AffiliateId.HasValue)
            {
                return BadRequest(new { message = "AffiliateId is required for affiliate users" });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            // Create new user
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                AffiliateId = request.AffiliateId,
                EmailConfirmed = true // Auto-confirm for admin registration
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create user {Email}: {Errors}", request.Email, errors);
                return BadRequest(new { message = "Failed to create user", errors = result.Errors.Select(e => e.Description) });
            }

            // Add role to user
            await _userManager.AddToRoleAsync(user, request.Role);

            // Generate tokens
            var roles = new List<string> { request.Role };
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            
            await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles,
                    AffiliateId = user.AffiliateId
                }
            };

            _logger.LogInformation("User {Email} registered successfully with role {Role}", request.Email, request.Role);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for user: {Email}. Reason: {Reason}", 
                    request.Email, 
                    result.IsLockedOut ? "Account locked" : 
                    result.IsNotAllowed ? "Account not allowed" : "Invalid password");
                
                if (result.IsLockedOut)
                {
                    return Unauthorized(new { message = "Account is locked due to multiple failed login attempts" });
                }
                
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            
            await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Should match JWT settings
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    AffiliateId = user.AffiliateId
                }
            };

            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(Request.Headers.Authorization.ToString().Replace("Bearer ", ""));
            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid access token" });
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims" });
            }

            var isValidRefreshToken = await _jwtService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValidRefreshToken)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Revoke old refresh token and save new one
            await _jwtService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
            await _jwtService.SaveRefreshTokenAsync(userId, newRefreshToken);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    AffiliateId = user.AffiliateId
                }
            };

            _logger.LogInformation("Token refreshed for user {UserId}", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                await _jwtService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
                _logger.LogInformation("User {UserId} logged out successfully", userId);
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                AffiliateId = user.AffiliateId
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred while getting user information" });
        }
    }
}