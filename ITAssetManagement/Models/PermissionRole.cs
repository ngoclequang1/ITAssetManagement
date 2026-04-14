using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("PERMISSION_ROLE")]
    public class PermissionRole
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("role_name")]
        public string? RoleName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        public ICollection<User>? Users { get; set; }
    }
}