using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("request_history")]
public class RequestHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("status_id")]
    public int? StatusId { get; set; }

    [Column("user_created_id")]
    public int? UserCreatedId { get; set; }

    // Hành động: Created / First Approved / Approved / Rejected / Cancelled
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("action_by")]
    public string ActionBy { get; set; } = "system";

    [Column("action_at")]
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    [Column("note")]
    public string? Note { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }
}