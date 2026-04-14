namespace ITAssetManagement.DTOs.Users
{
    public class CreateUserDto
    {
        public string UserCode { get; set; }

        public string Username { get; set; }

        public string UsernameAlphabet { get; set; }

        public string Email { get; set; }

        public string ConsoleLoginId { get; set; }

        public string SystemLoginId { get; set; }

        public string PasswordHash { get; set; }

        public int PrimaryDepartmentId { get; set; }

        public int RoleId { get; set; }
    }
}
