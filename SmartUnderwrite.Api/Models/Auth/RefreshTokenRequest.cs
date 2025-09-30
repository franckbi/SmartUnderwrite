using System.ComponentModel.DataAnnotations;

namespace SmartUnderwrite.Api.Models.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}