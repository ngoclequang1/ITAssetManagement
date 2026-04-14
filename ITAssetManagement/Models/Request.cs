using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("REQUEST")]
    public class Request
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("request_type_id")]
        public int RequestTypeId { get; set; }

        [Column("user_created_id")]
        public int UserCreatedId { get; set; }

        [Column("asset_id")]
        public int? AssetId { get; set; }

        [Column("target_div")]
        public int TargetDiv { get; set; }

        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("request_description")]
        public string? RequestDescription { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ======================
        // Navigation
        // ======================

        [ForeignKey("UserCreatedId")]
        public User UserCreated { get; set; }

        [ForeignKey("AssetId")]
        public ITAsset Asset { get; set; }

        [ForeignKey("RequestTypeId")]
        public RequestType RequestType { get; set; }

        [ForeignKey("StatusId")]
        public RequestStatus Status { get; set; }
    }
}