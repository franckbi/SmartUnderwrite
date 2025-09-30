using SmartUnderwrite.Core.Entities;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface IJwtService
{
    Task<string> GenerateAccessTokenAsync(User user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
    Task SaveRefreshTokenAsync(int userId, string refreshToken);
    Task RevokeRefreshTokenAsync(int userId, string refreshToken);
}