namespace ITAssetManagement.DTOs.Software
{
    public class SoftwareSearchDto
    {
        public string? AssetControlNumber { get; set; }
        public string? SoftwareName { get; set; }
        public string? SoftwareVersion { get; set; }

        public int? VendorId { get; set; }
        public int? LicenseId { get; set; }
        public string? LicenseType { get; set; }

        public int? GroupId { get; set; }
    }
}