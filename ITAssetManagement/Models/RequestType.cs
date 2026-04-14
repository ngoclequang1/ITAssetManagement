using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("REQUEST_TYPE")] 
    public class RequestType
    {
        [Key]
        [Column("type_id")]
        public int TypeId { get; set; }

        [Column("type_name")]
        public string TypeName { get; set; }
    }
}