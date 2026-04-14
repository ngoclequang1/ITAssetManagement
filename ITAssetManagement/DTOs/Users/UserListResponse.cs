namespace ITAssetManagement.DTOs.Users
{
    public class UserListResponse
    {
        public int UserId { get; set; }

        public string UserCode { get; set; }

        public string Username { get; set; }

        public string ConsoleLoginId { get; set; }

        public string SystemLoginId { get; set; }

        public string Email { get; set; }

        public string DepartmentCode { get; set; }

        public string DepartmentName { get; set; }

        public string RoleName { get; set; }

        public bool AuditorFlag { get; set; }
    }
}
