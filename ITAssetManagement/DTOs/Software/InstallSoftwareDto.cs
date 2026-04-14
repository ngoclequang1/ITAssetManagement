namespace ITAssetManagement.DTOs.Software
{
    public class InstallSoftwareDto
    {
        public string SoftwareName { get; set; }
        public string SoftwareVersion { get; set; }
        public int? VendorId { get; set; }
        public int? LicenseId { get; set; }
        public string LicenseType { get; set; }
        public string Description { get; set; }

        public string AssetControlNumber { get; set; }

        public int InstalledBy { get; set; } // user thực hiện
    }
}