using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("USER_ITEMS")]
    public class UserItem
    {
        [Key]
        [Column("user_item_id")]
        public int UserItemId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("item_key")]
        public string? ItemKey { get; set; }

        [Column("item_value")]
        public string? ItemValue { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
