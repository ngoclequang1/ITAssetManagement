namespace ITAssetManagement.DTOs.Users
{
    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? ConsoleLoginId { get; set; }
        public int? RoleId { get; set; }
        public bool? AuditorFlag { get; set; }
    }
}
