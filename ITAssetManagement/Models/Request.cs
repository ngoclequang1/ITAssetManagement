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
        public int? TargetDiv { get; set; }

        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("request_description")]
        public string? RequestDescription { get; set; }

        // JSON data chứa nội dung thay đổi (theo SKILL_APPROVAL_WORKFLOW)
        [Column("request_data")]
        public string? RequestData { get; set; }

        [Column("first_approver_id")]
        public int? FirstApproverId { get; set; }

        [Column("second_approver_id")]
        public int? SecondApproverId { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("rejected_at")]
        public DateTime? RejectedAt { get; set; }

        [Column("reject_reason")]
        public string? RejectReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public string CreatedBy { get; set; } = "system";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_by")]
        public string UpdatedBy { get; set; } = "system";

        // ======================
        // Navigation
        // ======================

        [ForeignKey("UserCreatedId")]
        public User? UserCreated { get; set; }

        [ForeignKey("AssetId")]
        public ITAsset? Asset { get; set; }

        [ForeignKey("RequestTypeId")]
        public RequestType? RequestType { get; set; }

        [ForeignKey("StatusId")]
        public RequestStatus? Status { get; set; }
    }
}