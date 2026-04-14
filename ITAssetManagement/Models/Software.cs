using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("SOFTWARE")]
    public class Software
    {
        [Key]
        [Column("software_id")]
        public int SoftwareId { get; set; }

        [Column("software_name")]
        public string SoftwareName { get; set; }

        [Column("software_version")]
        public string SoftwareVersion { get; set; }

        [Column("vendor_id")]
        public int? VendorId { get; set; }

        [Column("license_id")]
        public int? LicenseId { get; set; }

        [Column("license_type")]
        public string? LicenseType { get; set; } 

        [Column("description")]
        public string? Description { get; set; } 

        [Column("group_id")]
        public int? GroupId { get; set; }

        [Column("asset_id")]
        public int? AssetId { get; set; }

        [Column("asset_control_number")]
        public string? AssetControlNumber { get; set; } 

        [Column("installed_by")]
        public int? InstalledBy { get; set; } 

        [Column("installed_date")]
        public DateTime InstalledDate { get; set; }

        [Column("software_type")]
        public string? SoftwareType { get; set; }

        // ======================
        // RELATIONSHIPS
        // ======================

        [ForeignKey("AssetId")]
        public ITAsset Asset { get; set; }

        [ForeignKey("InstalledBy")]
        public User InstalledByUser { get; set; }
    }
}