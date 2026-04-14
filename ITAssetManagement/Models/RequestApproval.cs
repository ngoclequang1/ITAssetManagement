using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("request_approval")] 
public class RequestApproval
{
    [Key]
    [Column("approval_id")]
    public int ApprovalId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("approver_id")]
    public int ApproverId { get; set; }

    [Column("approval_level")]
    public int ApprovalLevel { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    public string? Remarks { get; set; }
}