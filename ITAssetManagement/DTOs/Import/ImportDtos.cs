namespace ITAssetManagement.DTOs.Import
{
    // =============================================
    // HARDWARE IMPORT
    // =============================================
    public class HardwareImportRowDto
    {
        public int RowNumber { get; set; }

        public string? AssetControlNumber { get; set; }
        public string? AssetName { get; set; }
        public string? SerialNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int? CategoryId { get; set; }
        public int? StatusId { get; set; }
        public int? LocationId { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string? Notes { get; set; }

        // Populated during validation
        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }

    // =============================================
    // SOFTWARE IMPORT
    // =============================================
    public class SoftwareImportRowDto
    {
        public int RowNumber { get; set; }

        public string? AssetControlNumber { get; set; }
        public string? SoftwareName { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? SoftwareType { get; set; } // Đã giữ lại trường này
        public int? VendorId { get; set; }
        public int? LicenseId { get; set; }
        public string? LicenseType { get; set; }
        public string? Description { get; set; }
        public int? InstalledBy { get; set; }
        public DateTime? InstallDate { get; set; }

        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }
    // =============================================
    // LICENSE IMPORT
    // =============================================
    public class LicenseImportRowDto
    {
        public int RowNumber { get; set; }

        public string? InstallationName { get; set; }
        public string? PublisherName { get; set; }
        public string? SoftwareType { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseFormat { get; set; }
        public string? CountingMethod { get; set; }
        public int? NumberOfLicenses { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool AcademicFlag { get; set; }
        public int? ManagementDepartmentId { get; set; }
        public string? Description { get; set; }

        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Any();
    }

    // =============================================
    // IMPORT RESULT
    // =============================================
    public class ImportResultDto
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string? Message { get; set; }
        // Only populated when errors exist — client downloads this file
        public byte[]? ErrorFileBytes { get; set; }
        public string? ErrorFileName { get; set; }
    }
}
