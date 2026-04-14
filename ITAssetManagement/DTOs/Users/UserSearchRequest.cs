namespace ITAssetManagement.DTOs.Users
{
    public class UserSearchRequest
    {
        public string? UserCode { get; set; }
        public string? Username { get; set; }
        public string? ConsoleLoginId { get; set; }
        public string? Email { get; set; }
        public string? DepartmentCode { get; set; }

        
        public List<int>? RoleIds { get; set; }

        public bool? AuditorFlag { get; set; }

        // Thêm phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}