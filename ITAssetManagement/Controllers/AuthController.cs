using Microsoft.AspNetCore.Mvc;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.Services;
using ITAssetManagement.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;

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
        [AllowAnonymous, EnableRateLimiting("auth")]
        public IActionResult Register(RegisterDTO dto)
        {
            var normalizedUsername = dto.Username.Trim();
            var normalizedEmail = dto.Email.Trim();
            var exist = _context.Users
                .FirstOrDefault(x => x.Username == normalizedUsername || x.Email == normalizedEmail);

            if (exist != null)
                return Conflict("Username or email already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = normalizedUsername,
                Email = normalizedEmail,
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
        [AllowAnonymous, EnableRateLimiting("auth")]
        public IActionResult Login(LoginDTO dto)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Username == dto.Username.Trim() && !x.IsDeleted);

            if (user == null)
                return Unauthorized("Invalid username or password");

            if (user.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password");
            }

            user.LastLogin = DateTime.UtcNow;
            _context.SaveChanges();

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
        [AllowAnonymous, EnableRateLimiting("auth")]
        public IActionResult ForgotPassword(ForgotPasswordDTO dto)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.SystemLoginId == dto.LoginId
            );

            if (user == null || string.IsNullOrEmpty(user.Email))
                return Ok("If the account exists, an OTP has been sent.");

            // Generate OTP 6 digits
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            user.ResetOtp = otp;
            user.ResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);

            _context.SaveChanges();

            // Send email
            _emailService.SendEmail(
                user.Email,
                "Reset Password OTP",
                $"Your OTP is: {otp}\nThis code will expire in 5 minutes."
            );

            return Ok("If the account exists, an OTP has been sent.");
        }

        // =========================
        // RESET PASSWORD (FROM EMAIL)
        // =========================
        [HttpPost("reset-password")]
        [AllowAnonymous, EnableRateLimiting("auth")]
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
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordDTO dto)
        {
            var userIdValue = User.FindFirstValue("userId");
            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var user = _context.Users.FirstOrDefault(x => x.UserId == userId);

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
