namespace ITAssetManagement.DTOs.Software
{
    public class SoftwareGroupingDto
    {
        public List<int> SoftwareIds { get; set; }
        public int? GroupId { get; set; } 
    }

    // =============================================
    // CHANGE APPLICATION DTO
    // =============================================
    public class SoftwareChangeApplicationDto
    {
        public string? SoftwareName { get; set; }
        public string? SoftwareVersion { get; set; }
        public string? LicenseType { get; set; }
        public string? SoftwareType { get; set; }
        public string? Description { get; set; }
        public int? VendorId { get; set; }
        public int? LicenseId { get; set; }

        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // COPY REQUEST DTO
    // =============================================
    public class SoftwareCopyRequestDto
    {
        public string TargetAssetControlNumber { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // =============================================
    // UNINSTALL REQUEST DTO
    // =============================================
    public class SoftwareUninstallRequestDto
    {
        public string? Reason { get; set; }

        public int FirstApproverId { get; set; }
        public int? SecondApproverId { get; set; }
    }
}