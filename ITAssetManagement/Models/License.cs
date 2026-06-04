using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("LICENSE")]
    public class License
    {
        [Key]
        [Column("license_id")]
        public int LicenseId { get; set; }

        // Mã quản lý tự sinh (LIC-00001)
        [Column("license_management_number")]
        public string? LicenseManagementNumber { get; set; }

        // Các cột cũ giữ nguyên
        [Column("license_key")]
        public string? LicenseKey { get; set; }

        [Column("license_count")]
        public int? LicenseCount { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        // Các cột mới thêm vào
        [Column("installation_name")]
        public string? InstallationName { get; set; }

        [Column("publisher_name")]
        public string? PublisherName { get; set; }

        [Column("software_type")]
        public string? SoftwareType { get; set; }

        [Column("license_type")]
        public string? LicenseType { get; set; }

        [Column("license_format")]
        public string? LicenseFormat { get; set; }

        [Column("counting_method")]
        public string? CountingMethod { get; set; }

        [Column("academic_flag")]
        public bool AcademicFlag { get; set; }

        [Column("number_of_licenses")]
        public int NumberOfLicenses { get; set; }

        [Column("number_available")]
        public int NumberAvailable { get; set; }

        [Column("management_department_id")]
        public int? ManagementDepartmentId { get; set; }

        [Column("manager_user_id")]
        public int? ManagerUserId { get; set; }

        [Column("license_status")]
        public string LicenseStatus { get; set; } = "Active";

        [Column("disposal_date")]
        public DateTime? DisposalDate { get; set; }

        [Column("parent_license_id")]
        public int? ParentLicenseId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public string CreatedBy { get; set; } = "system";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_by")]
        public string UpdatedBy { get; set; } = "system";

        // Navigation Properties
        [ForeignKey("ManagementDepartmentId")]
        public Department? ManagementDepartment { get; set; }

        [ForeignKey("ManagerUserId")]
        public User? Manager { get; set; }

        [ForeignKey("ParentLicenseId")]
        public License? ParentLicense { get; set; }

        public ICollection<License>? ChildLicenses { get; set; }

        public ICollection<LicenseInventoryHistory>? InventoryHistories { get; set; }
    }
}
