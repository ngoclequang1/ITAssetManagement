using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("ASSET_STATUS")]
    public class AssetStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("status_name")]
        public string? StatusName { get; set; }

        public ICollection<ITAsset>? Assets { get; set; }
    }
}
