using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Auth;

public sealed class LoginDTO
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
