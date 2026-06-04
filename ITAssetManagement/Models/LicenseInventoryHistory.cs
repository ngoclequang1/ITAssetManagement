using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("LICENSE_INVENTORY_HISTORY")]
    public class LicenseInventoryHistory
    {
        [Key]
        [Column("inventory_id")]
        public int InventoryId { get; set; }

        [Column("license_id")]
        public int LicenseId { get; set; }

        [Column("inventory_date")]
        public DateTime InventoryDate { get; set; }

        [Column("inventory_taker_id")]
        public int? InventoryTakerId { get; set; }

        // "Completed" / "Not Yet"
        [Column("inventory_status")]
        public string InventoryStatus { get; set; } = "Not Yet";

        [Column("remarks")]
        public string? Remarks { get; set; }

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

        // Navigation
        [ForeignKey("LicenseId")]
        public License License { get; set; }

        [ForeignKey("InventoryTakerId")]
        public User? InventoryTaker { get; set; }
    }
}
