using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITAssetManagement.Models
{
    [Table("USER_PERMISSION")]
    public class UserPermission
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Được xem danh sách / chi tiết tài sản</summary>
        [Column("can_view")]
        public bool CanView { get; set; } = true;

        /// <summary>Được tạo request (Add / Edit / Delete asset)</summary>
        [Column("can_request")]
        public bool CanRequest { get; set; } = false;

        /// <summary>Được Approve / Reject request</summary>
        [Column("can_approve")]
        public bool CanApprove { get; set; } = false;

        /// <summary>Quản trị hệ thống (User, Department, Master)</summary>
        [Column("can_admin")]
        public bool CanAdmin { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}