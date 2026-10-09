using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Auth;

public sealed class ResetPasswordDTO
{
    [Required, StringLength(100)]
    public string LoginId { get; set; } = string.Empty;

    [Required, RegularExpression("^[0-9]{6}$")]
    public string Otp { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
