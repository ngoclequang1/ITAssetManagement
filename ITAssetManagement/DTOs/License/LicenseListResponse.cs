namespace ITAssetManagement.DTOs.License
{
    // DTO trả về cho 1 thẻ License Card trong danh sách
    public class LicenseListResponse
    {
        public int LicenseId { get; set; }
 
        // Header card
        public string? LicenseManagementNumber { get; set; }
        public string? InstallationName { get; set; }
        public string? PublisherName { get; set; }
 
        // Status tags
        public string LicenseStatus { get; set; } = "Active";   // Active / Disposed / Pending
        public bool HasInventory { get; set; }                   // [Inventory Complete]
        public bool IsLinked { get; set; }                       // [LINK] – linked to software
        public bool IsUnstocked { get; set; }                    // [Unstocked] – no allocation made yet
 
        // Số liệu phân bổ
        public int NumberOfLicenses { get; set; }
        public int NumberAvailable { get; set; }
        public string? CountingMethod { get; set; }
 
        // Phân loại
        public string? SoftwareType { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseFormat { get; set; }
        public bool AcademicFlag { get; set; }
 
        // Bộ phận & người quản lý
        public string? ManagementDepartmentCode { get; set; }
        public string? ManagementDepartmentName { get; set; }
        public string? ManagerUsername { get; set; }
 
        // License gốc (nếu là license con từ Split)
        public int? ParentLicenseId { get; set; }
        public string? ParentLicenseManagementNumber { get; set; }
 
        // Ngày tháng
        public DateTime? ExpiryDate { get; set; }
        public DateTime? DisposalDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
 