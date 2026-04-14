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
    public int StatusId { get; set; }

    [Column("user_created_id")]
    public int UserCreatedId { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

}