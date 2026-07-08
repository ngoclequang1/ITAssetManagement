namespace ITAssetManagement.DTOs
{
    public class AssignUserDto
    {
        public int UserId { get; set; }

        /// <summary>
        /// Đánh dấu user này là Primary Admin của phòng ban.
        /// Mặc định false (thành viên thường).
        /// </summary>
        public bool IsPrimaryAdmin { get; set; } = false;
    }
}