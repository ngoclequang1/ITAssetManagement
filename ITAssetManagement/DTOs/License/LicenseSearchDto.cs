namespace ITAssetManagement.DTOs.License
{
    public class LicenseSearchDto
    {
        // Bộ lọc bên sidebar
        public string? LicenseManagementNumber { get; set; }
        public string? InstallationName { get; set; }
        public string? PublisherName { get; set; }
        public string? SoftwareType { get; set; }
        public string? LicenseType { get; set; }
        public string? CountingMethod { get; set; }
        public string? LicenseStatus { get; set; }
        public int? ManagementDepartmentId { get; set; }

        // Lọc theo "Display License" toggle
        public bool? AcademicFlag { get; set; }

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
