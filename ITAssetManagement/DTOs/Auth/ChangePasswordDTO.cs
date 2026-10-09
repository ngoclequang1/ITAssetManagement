using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Auth;

public sealed class ChangePasswordDTO
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string NewPassword { get; set; } = string.Empty;
}
