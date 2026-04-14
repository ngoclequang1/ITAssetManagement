using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("REQUEST_DETAIL")]
    public class RequestDetail
    {
        [Key] 
        [Column("detail_id")]
        public int RequestDetailId { get; set; }

        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("field_name")]
        public string FieldName { get; set; }

        [Column("old_value")]
        public string ? OldValue { get; set; }

        [Column("new_value")]
        public string ? NewValue { get; set; }

        // Navigation
        public Request Request { get; set; }
    }
}