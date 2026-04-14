namespace ITAssetManagement.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_code")]
        public string? UserCode { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [Column("username_kana")]
        public string? UsernameKana { get; set; }

        [Column("username_alphabet")]
        public string? UsernameAlphabet { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("console_login_id")]
        public string? ConsoleLoginId { get; set; }

        [Column("system_login_id")]
        public string? SystemLoginId { get; set; }

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("primary_department_id")]
        public int? PrimaryDepartmentId { get; set; }

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("auditor_flag")]
        public bool AuditorFlag { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("reset_otp")]
        public string? ResetOtp { get; set; }

        [Column("reset_otp_expiry")]
        public DateTime? ResetOtpExpiry { get; set; }

        // =========================
        // Navigation Properties
        // =========================

        [ForeignKey("PrimaryDepartmentId")]
        public Department? Department { get; set; }

        [ForeignKey("RoleId")]
        public PermissionRole? Role { get; set; }

        public ICollection<UserItem>? UserItems { get; set; }

        public ICollection<DepartmentRepresentative>? DepartmentRepresentatives { get; set; }
    }
}