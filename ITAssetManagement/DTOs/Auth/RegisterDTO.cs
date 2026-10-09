using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Auth;

public sealed class RegisterDTO
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
