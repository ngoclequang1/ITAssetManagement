namespace ITAssetManagement.DTOs.License
{
    // =============================================
    // NEW APPLICATION
    // =============================================
    public class LicenseNewApplicationDto
    {
        // Required: Software info
        public string InstallationName { get; set; } = string.Empty;
        public string PublisherName { get; set; } = string.Empty;
        public string SoftwareType { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public string LicenseFormat { get; set; } = string.Empty;
        public string CountingMethod { get; set; } = string.Empty;
        public bool AcademicFlag { get; set; }

        // License quantity & key
        public string? LicenseKey { get; set; }
        public int NumberOfLicenses { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Description { get; set; }

        // Management (optional)
        public int? ManagementDepartmentId { get; set; }

        // Approval workflow (required)
        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // CHANGE APPLICATION
    // =============================================
    public class LicenseChangeApplicationDto
    {
        // Editable fields only (License Management Number is read-only)
        public string? PublisherName { get; set; }
        public string? SoftwareType { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseFormat { get; set; }
        public string? CountingMethod { get; set; }
        public bool? AcademicFlag { get; set; }
        public int? NumberOfLicenses { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Description { get; set; }
        public int? ManagementDepartmentId { get; set; }

        // Approval workflow (required)
        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // MOVE APPLICATION
    // =============================================
    public class LicenseMoveApplicationDto
    {
        // Destination department (required) – per SKILL_LICENSE_MANAGEMENT §3.3
        public int DestinationDepartmentId { get; set; }

        // Person in charge of the location (required) – người quản lý mới tại bộ phận đích
        public int NewManagerUserId { get; set; }

        // Approval workflow (required)
        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // SPLIT APPLICATION
    // =============================================
    public class LicenseSplitApplicationDto
    {
        // How many licenses to split out
        public int SplitCount { get; set; }

        // Destination department for the new child license
        public int DestinationDepartmentId { get; set; }

        // Approval workflow (required)
        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // DISPOSAL APPLICATION
    // =============================================
    public class LicenseDisposalApplicationDto
    {
        public DateTime DisposalDate { get; set; }
        public string? Remarks { get; set; }

        // Approval workflow (required)
        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // REJECT (used by RequestController)
    // =============================================
    public class RejectRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}