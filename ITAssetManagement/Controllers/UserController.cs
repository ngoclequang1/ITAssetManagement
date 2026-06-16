using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.DTOs.Users;

namespace ITAssetManagement.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        //  Search Users
        // =========================

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] UserSearchRequest request)
        {
            var query =
                from u in _context.Users
                join d in _context.Departments
                on u.PrimaryDepartmentId equals d.DepartmentId into dept
                from d in dept.DefaultIfEmpty()

                join r in _context.PermissionRoles
                on u.RoleId equals r.RoleId into role
                from r in role.DefaultIfEmpty()

                select new { u, d, r };

            // Lọc dữ liệu
            if (!string.IsNullOrWhiteSpace(request.UserCode))
                query = query.Where(x => x.u.UserCode != null && x.u.UserCode.Contains(request.UserCode));

            if (!string.IsNullOrWhiteSpace(request.Username))
                query = query.Where(x => x.u.Username != null && x.u.Username.Contains(request.Username));

            if (!string.IsNullOrWhiteSpace(request.ConsoleLoginId))
                query = query.Where(x => x.u.ConsoleLoginId != null && x.u.ConsoleLoginId.Contains(request.ConsoleLoginId));

            if (!string.IsNullOrWhiteSpace(request.Email))
                query = query.Where(x => x.u.Email != null && x.u.Email.Contains(request.Email));

            if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
                query = query.Where(x => x.d != null && x.d.DepartmentCode == request.DepartmentCode);

            // Cập nhật: Lọc theo mảng RoleIds (nếu có chọn từ UI)
            if (request.RoleIds != null && request.RoleIds.Any())
                query = query.Where(x => x.u.RoleId.HasValue && request.RoleIds.Contains(x.u.RoleId.Value));

            if (request.AuditorFlag.HasValue)
                query = query.Where(x => x.u.AuditorFlag == request.AuditorFlag);

            // Tính tổng số lượng bản ghi sau khi lọc
            var total = await query.CountAsync();

            // Phân trang và lấy dữ liệu
            int page = request.Page > 0 ? request.Page : 1;
            int pageSize = request.PageSize > 0 ? request.PageSize : 10;

            var users = await query
                .OrderBy(x => x.u.UserId) // Cần sắp xếp trước khi Skip/Take
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserListResponse
                {
                    UserId = x.u.UserId,
                    UserCode = x.u.UserCode,
                    Username = x.u.Username,
                    ConsoleLoginId = x.u.ConsoleLoginId,
                    SystemLoginId = x.u.SystemLoginId,
                    Email = x.u.Email,
                    DepartmentCode = x.d != null ? x.d.DepartmentCode : null,
                    DepartmentName = x.d != null ? x.d.DepartmentName : null,
                    RoleName = x.r != null ? x.r.RoleName : null,
                    AuditorFlag = x.u.AuditorFlag
                })
                .ToListAsync();

            // Trả về cấu trúc hỗ trợ phân trang cho Frontend
            return Ok(new
            {
                total = total,
                data = users
            });
        }

        // =========================
        //  Get User List
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query =
               from u in _context.Users
               join d in _context.Departments
               on u.PrimaryDepartmentId equals d.DepartmentId into dept
               from d in dept.DefaultIfEmpty()

               join r in _context.PermissionRoles
               on u.RoleId equals r.RoleId into role
               from r in role.DefaultIfEmpty()

               select new UserListResponse
               {
                   UserId = u.UserId,
                   UserCode = u.UserCode,
                   Username = u.Username,
                   ConsoleLoginId = u.ConsoleLoginId,
                   SystemLoginId = u.SystemLoginId,
                   Email = u.Email,
                   DepartmentCode = d != null ? d.DepartmentCode : null,
                   DepartmentName = d != null ? d.DepartmentName : null,
                   RoleName = r != null ? r.RoleName : null,
                   AuditorFlag = u.AuditorFlag
               };

            var total = await query.CountAsync();

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var users = await query
                        .OrderBy(u => u.UserId)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

            return Ok(new
            {
                total = total,
                data = users
            });
        }

        // =========================
        //  Get User Detail
        // =========================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.UserCode,
                    u.Username,
                    u.UsernameAlphabet,
                    u.Email,
                    u.ConsoleLoginId,
                    u.SystemLoginId,
                    DepartmentCode = u.Department.DepartmentCode,
                    DepartmentName = u.Department.DepartmentName,
                    RoleName = u.Role.RoleName,
                    u.AuditorFlag
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // =========================
        //  List Hardware Used
        // =========================

        [HttpGet("{id}/hardware")]
        public async Task<IActionResult> GetUserHardware(int id)
        {
            var assets = await _context.ITAssets
                .Where(a => a.UserUsedId == id)
                .Select(a => new
                {
                    a.AssetControlNumber,
                    a.AssetName,
                    a.Model,
                    a.SerialNumber
                })
                .ToListAsync();

            return Ok(assets);
        }

        // =========================
        // Delete User
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // =========================
        // Update User
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(request.Username))
                user.Username = request.Username;

            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;

            if (!string.IsNullOrEmpty(request.ConsoleLoginId))
                user.ConsoleLoginId = request.ConsoleLoginId;

            if (request.RoleId.HasValue)
                user.RoleId = request.RoleId.Value;

            if (request.AuditorFlag.HasValue)
                user.AuditorFlag = request.AuditorFlag.Value;

            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // =========================
        // Create User
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            // Check email duplicate
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
                return BadRequest("Email already exists");

            var user = new User
            {
                UserCode = dto.UserCode,
                Username = dto.Username,
                UsernameAlphabet = dto.UsernameAlphabet,
                Email = dto.Email,
                ConsoleLoginId = dto.ConsoleLoginId,
                SystemLoginId = dto.SystemLoginId,
                PasswordHash = dto.PasswordHash,
                PrimaryDepartmentId = dto.PrimaryDepartmentId,
                RoleId = dto.RoleId
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserDetail), new { id = user.UserId }, user);
        }

        // =========================
        // Copy User Registration
        // =========================
        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            // Tạo bản sao với một số trường thêm hậu tố để tránh lỗi Unique Key (Email, UserCode...)
            var newUser = new User
            {
                UserCode = user.UserCode + "_COPY_" + DateTime.Now.Ticks.ToString().Substring(10),
                Username = user.Username + " (Copy)",
                UsernameAlphabet = user.UsernameAlphabet,
                Email = "copy_" + DateTime.Now.Ticks + "_" + user.Email,
                ConsoleLoginId = user.ConsoleLoginId + "_copy",
                SystemLoginId = user.SystemLoginId,
                PasswordHash = user.PasswordHash, 
                PrimaryDepartmentId = user.PrimaryDepartmentId,
                RoleId = user.RoleId,
                AuditorFlag = user.AuditorFlag,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User copied successfully",
                userId = newUser.UserId
            });
        }

    }
}