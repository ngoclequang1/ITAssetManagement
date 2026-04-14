using Microsoft.AspNetCore.Mvc;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.Services;
using ITAssetManagement.DTOs.Auth;

namespace ITAssetManagement.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;
        private readonly IEmailService _emailService;

        public AuthController(
            ApplicationDbContext context,
            JwtService jwt,
            IEmailService emailService)
        {
            _context = context;
            _jwt = jwt;
            _emailService = emailService;
        }

        // =========================
        // REGISTER
        // =========================
        [HttpPost("register")]
        public IActionResult Register(RegisterDTO dto)
        {
            var exist = _context.Users
                .FirstOrDefault(x => x.Username == dto.Username);

            if (exist != null)
                return BadRequest("Username already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User created");
        }

        // =========================
        // LOGIN
        // =========================
        [HttpPost("login")]
        public IActionResult Login(LoginDTO dto)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Username == dto.Username);

            if (user == null)
                return Unauthorized("Invalid username");

            if (user.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid password");
            }

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                user.Username,
                user.UserId,
                user.RoleId
            });
        }

        // =========================
        // FORGOT PASSWORD (LOGIN ID)
        // =========================
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordDTO dto)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.SystemLoginId == dto.LoginId
            );

            if (user == null)
                return BadRequest("Login ID not found");

            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("User has no email");

            // Generate OTP 6 digits
            var otp = new Random().Next(100000, 999999).ToString();

            user.ResetOtp = otp;
            user.ResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);

            _context.SaveChanges();

            // Send email
            _emailService.SendEmail(
                user.Email,
                "Reset Password OTP",
                $"Your OTP is: {otp}\nThis code will expire in 5 minutes."
            );

            return Ok("OTP sent to email");
        }

        // =========================
        // RESET PASSWORD (FROM EMAIL)
        // =========================
        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordDTO dto)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.SystemLoginId == dto.LoginId &&
                x.ResetOtp == dto.Otp &&
                x.ResetOtpExpiry > DateTime.UtcNow
            );

            if (user == null)
                return BadRequest("Invalid OTP or expired");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // clear OTP
            user.ResetOtp = null;
            user.ResetOtpExpiry = null;

            _context.SaveChanges();

            return Ok("Password reset successful");
        }

        // =========================
        // CHANGE PASSWORD (AFTER LOGIN)
        // =========================
        [HttpPost("change-password")]
        public IActionResult ChangePassword(ChangePasswordDTO dto)
        {
            var user = _context.Users.FirstOrDefault(x => x.UserId == dto.UserId);

            if (user == null)
                return NotFound("User not found");

            if (user.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            {
                return BadRequest("Old password is incorrect");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            _context.SaveChanges();

            return Ok("Password changed successfully");
        }
    }
}