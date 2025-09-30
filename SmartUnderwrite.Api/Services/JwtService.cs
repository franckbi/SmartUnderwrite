using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartUnderwrite.Api.Models;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartUnderwrite.Api.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SmartUnderwriteDbContext _context;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        IOptions<JwtSettings> jwtSettings,
        SmartUnderwriteDbContext context,
        ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _context = context;
        _logger = logger;
    }

    public Task<string> GenerateAccessTokenAsync(User user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add affiliate claim if user belongs to an affiliate
        if (user.AffiliateId.HasValue)
        {
            claims.Add(new Claim("affiliateId", user.AffiliateId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false // We don't validate lifetime here since we're dealing with expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate expired token");
            return null;
        }
    }

    public Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
    {
        // In a production system, you would store refresh tokens in the database
        // For this MVP, we'll implement a simple in-memory validation
        // This should be replaced with proper database storage for production
        
        // For now, we'll just check if the refresh token format is valid
        try
        {
            var bytes = Convert.FromBase64String(refreshToken);
            return Task.FromResult(bytes.Length == 64);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
    {
        // In a production system, you would save the refresh token to the database
        // with an expiration date and associate it with the user
        // For this MVP, we'll skip database storage
        _logger.LogInformation("Refresh token generated for user {UserId}", userId);
        await Task.CompletedTask;
    }

    public async Task RevokeRefreshTokenAsync(int userId, string refreshToken)
    {
        // In a production system, you would mark the refresh token as revoked in the database
        // For this MVP, we'll just log the revocation
        _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
        await Task.CompletedTask;
    }
}