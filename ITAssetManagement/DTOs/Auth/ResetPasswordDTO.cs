namespace ITAssetManagement.DTOs.Auth
{
    public class ResetPasswordDTO
    {
        public string LoginId { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
    }
}
