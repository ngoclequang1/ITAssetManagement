using ITAssetManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("REQUEST_ASSET")]
public class RequestAsset
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    public Request Request { get; set; }

    public ITAsset Asset { get; set; }
}