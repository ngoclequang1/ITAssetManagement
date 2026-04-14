using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("DEPARTMENT_REPRESENTATIVE")]
    public class DepartmentRepresentative
    {
        [Key]
        [Column("rep_id")]
        public int RepId { get; set; }

        [Column("department_id")]
        public int DepartmentId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("is_primary_admin")]
        public bool IsPrimaryAdmin { get; set; }

        public Department Department { get; set; }

        public User User { get; set; }

        public PermissionRole Role { get; set; }
    }
}
