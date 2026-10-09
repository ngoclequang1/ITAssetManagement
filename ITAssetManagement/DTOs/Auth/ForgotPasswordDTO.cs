using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.DTOs.Auth;

public sealed class ForgotPasswordDTO
{
    [Required, StringLength(100)]
    public string LoginId { get; set; } = string.Empty;
}
