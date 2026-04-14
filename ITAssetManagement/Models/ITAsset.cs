using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("IT_ASSET")]
    public class ITAsset
    {
        [Key]
        [Column("asset_id")]
        public int AssetId { get; set; }

        [Column("asset_control_number")]
        public string? AssetControlNumber { get; set; }

        [Column("asset_name")]
        public string? AssetName { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("manufacturer")]
        public string? Manufacturer { get; set; }

        [Column("model")]
        public string? Model { get; set; }

        [Column("serial_number")]
        public string? SerialNumber { get; set; }

        [Column("purchase_date")]
        public DateTime? PurchaseDate { get; set; }

        [Column("warranty_expiry")]
        public DateTime? WarrantyExpiry { get; set; }

        [Column("department_id")]
        public int? DepartmentId { get; set; }

        [Column("asset_manager_id")]
        public int? AssetManagerId { get; set; }

        [Column("user_created_id")]
        public int? UserCreatedId { get; set; }

        [Column("user_used_id")]
        public int? UserUsedId { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [Column("inventory_id")]
        public int? InventoryId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // =========================
        // Navigation Properties
        // =========================

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [ForeignKey("AssetManagerId")]
        public User? AssetManager { get; set; }

        [ForeignKey("UserCreatedId")]
        public User? UserCreated { get; set; }

        [ForeignKey("UserUsedId")]
        public User? UserUsed { get; set; }

        [ForeignKey("CategoryId")]
        public AssetCategory? Category { get; set; }

        [ForeignKey("StatusId")]
        public AssetStatus? Status { get; set; }

        [ForeignKey("LocationId")]
        public AssetLocation? Location { get; set; }

        [ForeignKey("InventoryId")]
        public InventoryHistory? Inventory { get; set; }
    }
}