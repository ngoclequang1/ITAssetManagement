using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("ASSET_LOCATION")]
    public class AssetLocation
    {
        [Key]
        [Column("location_id")]
        public int LocationId { get; set; }

        [Column("location_name")]
        public string? LocationName { get; set; }

        public ICollection<ITAsset>? Assets { get; set; }
    }
}
