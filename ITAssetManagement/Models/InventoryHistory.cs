using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("INVENTORY_HISTORY")]
    public class InventoryHistory
    {
        [Key]
        [Column("inventory_id")]
        public int InventoryId { get; set; }

        [Column("inventory_department_id")]
        public int InventoryDepartmentId { get; set; }

        [Column("inventory_implementer")]
        public int InventoryImplementer { get; set; }

        [Column("inventory_date")]
        public DateTime InventoryDate { get; set; }

        [Column("inventory_status")]
        public string InventoryStatus { get; set; }

        [Column("remarks")]
        public string Remarks { get; set; }

        // Navigation
        [ForeignKey("InventoryDepartmentId")]
        public Department InventoryDepartment { get; set; }

        [ForeignKey("InventoryImplementer")]
        public User InventoryImplementerNavigation { get; set; }
    }
}