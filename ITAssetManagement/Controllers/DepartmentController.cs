using ITAssetManagement.Data;
using ITAssetManagement.DTOs;
using ITAssetManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Controllers
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Department List & Search
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetDepartments([FromQuery] DepartmentQuery query)
        {
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var departments = _context.Departments
                .Include(d => d.ParentDepartment)
                .AsNoTracking()
                .AsQueryable();

            if (!query.ViewObsolete)
                departments = departments.Where(d => d.IsActive);

            if (!string.IsNullOrEmpty(query.Keyword))
                departments = departments.Where(d =>
                    (d.DepartmentCode != null && d.DepartmentCode.Contains(query.Keyword)) ||
                    (d.DepartmentName != null && d.DepartmentName.Contains(query.Keyword)));

            var total = await departments.CountAsync();

            var data = await departments
                .OrderBy(d => d.DepartmentCode)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(d => new DepartmentDto
                {
                    DepartmentId         = d.DepartmentId,
                    DepartmentCode       = d.DepartmentCode,
                    DepartmentName       = d.DepartmentName,
                    ParentDepartmentCode = d.ParentDepartment != null ? d.ParentDepartment.DepartmentCode : null,
                    TopDepartmentName    = d.ParentDepartment != null ? d.ParentDepartment.DepartmentName : d.DepartmentName,
                    IsActive             = d.IsActive,
                    CreatedAt            = d.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page = query.Page, pageSize = query.PageSize, data });
        }

        // ============================================
        // GET /api/departments/{id}
        // View Department Detail
        // FIX: trả về USERS có PrimaryDepartmentId = id
        //      thay vì chỉ query DEPARTMENT_REPRESENTATIVE
        // ============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentDetails(int id)
        {
            var department = await _context.Departments
                .Include(d => d.ParentDepartment)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
                return NotFound();

            // ── Lấy tất cả users thuộc phòng ban này (qua PrimaryDepartmentId) ──
            var members = await _context.Users
                .Where(u => u.PrimaryDepartmentId == id && !u.IsDeleted)
                .Include(u => u.Role)
                .AsNoTracking()
                .Select(u => new
                {
                    UserId      = u.UserId,
                    UserCode    = u.UserCode,
                    Username    = u.Username,
                    Email       = u.Email,
                    RoleName    = u.Role != null ? u.Role.RoleName : "General Staff",
                    AuditorFlag = u.AuditorFlag,
                    // Kiểm tra xem có trong DepartmentRepresentative không
                    IsPrimaryAdmin = _context.DepartmentRepresentatives
                        .Any(r => r.DepartmentId == id && r.UserId == u.UserId && r.IsPrimaryAdmin)
                })
                .ToListAsync();

            // ── Lấy từ DepartmentRepresentative (đại diện được chỉ định) ──
            var representatives = await _context.DepartmentRepresentatives
                .Where(r => r.DepartmentId == id)
                .Include(r => r.User)
                    .ThenInclude(u => u.Role)
                .AsNoTracking()
                .Select(r => new
                {
                    RepId        = r.RepId,
                    UserId       = r.UserId,
                    UserCode     = r.User.UserCode,
                    Username     = r.User.Username,
                    Email        = r.User.Email,
                    RoleName     = r.Role != null ? r.Role.RoleName : (r.User.Role != null ? r.User.Role.RoleName : "General Staff"),
                    IsPrimaryAdmin = r.IsPrimaryAdmin
                })
                .ToListAsync();

            return Ok(new
            {
                Department = new
                {
                    department.DepartmentId,
                    department.DepartmentCode,
                    department.DepartmentName,
                    ParentDepartmentCode = department.ParentDepartment != null
                        ? department.ParentDepartment.DepartmentCode
                        : null,
                    department.DeploymentName,
                    department.TopDeploymentName,
                    department.OverallDeployment,
                    department.IsKittingDepartment
                },
                // Danh sách tất cả thành viên (từ PrimaryDepartmentId)
                Members = members,
                // Danh sách đại diện được chỉ định (từ DEPARTMENT_REPRESENTATIVE)
                Representatives = representatives
            });
        }

        // =========================
        // Update Department Info
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, UpdateDepartmentDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            department.DepartmentName  = dto.DepartmentName;
            department.ParentDepartmentId = dto.ParentDepartmentId;

            await _context.SaveChangesAsync();
            return Ok(department);
        }

        // =========================
        // Update Department Status
        // =========================
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateDepartmentStatus(int id, UpdateDepartmentStatusDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            department.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message      = dto.IsActive ? "Department activated" : "Department marked as obsolete",
                departmentId = department.DepartmentId,
                isActive     = department.IsActive
            });
        }

        // =========================
        // Create Department Copy
        // =========================
        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            var newDepartment = new Department
            {
                DepartmentCode     = department.DepartmentCode + "_COPY",
                DepartmentName     = department.DepartmentName + " Copy",
                ParentDepartmentId = department.ParentDepartmentId,
                IsActive           = true,
                CreatedAt          = DateTime.UtcNow
            };

            _context.Departments.Add(newDepartment);
            await _context.SaveChangesAsync();
            return Ok(newDepartment);
        }

        // =========================
        // Create Department
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DepartmentCode))
                return BadRequest("DepartmentCode is required");

            var exists = await _context.Departments
                .AnyAsync(d => d.DepartmentCode == dto.DepartmentCode);
            if (exists)
                return BadRequest("Department code already exists");

            if (dto.ParentDepartmentId != null)
            {
                var parentExists = await _context.Departments
                    .AnyAsync(d => d.DepartmentId == dto.ParentDepartmentId);
                if (!parentExists)
                    return BadRequest("Parent department not found");
            }

            var department = new Department
            {
                DepartmentCode      = dto.DepartmentCode,
                DepartmentName      = dto.DepartmentName,
                ParentDepartmentId  = dto.ParentDepartmentId,
                IsKittingDepartment = dto.IsKittingDepartment,
                IsActive            = true,
                CreatedAt           = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department created successfully", departmentId = department.DepartmentId });
        }

        // ============================================
        // POST /api/departments/{id}/assign-user
        // FIX: cập nhật PrimaryDepartmentId VÀ thêm
        //      vào DEPARTMENT_REPRESENTATIVE nếu chưa có
        // ============================================
        [HttpPost("{id}/assign-user")]
        public async Task<IActionResult> AssignUserToDepartment(int id, [FromBody] AssignUserDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound("Department not found");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId && !u.IsDeleted);
            if (user == null)
                return NotFound("User not found");

            // 1. Cập nhật PrimaryDepartmentId
            user.PrimaryDepartmentId = id;

            // 2. Thêm vào DEPARTMENT_REPRESENTATIVE nếu chưa có
            var alreadyRep = await _context.DepartmentRepresentatives
                .AnyAsync(r => r.DepartmentId == id && r.UserId == dto.UserId);

            if (!alreadyRep)
            {
                var roleId = user.RoleId ?? 2; // fallback General Staff

                _context.DepartmentRepresentatives.Add(new DepartmentRepresentative
                {
                    DepartmentId   = id,
                    UserId         = dto.UserId,
                    RoleId         = roleId,
                    IsPrimaryAdmin = dto.IsPrimaryAdmin
                });
            }
            else if (dto.IsPrimaryAdmin)
            {
                // Nếu đã là rep rồi, chỉ cập nhật flag nếu được chỉ định làm Primary Admin
                var existingRep = await _context.DepartmentRepresentatives
                    .FirstAsync(r => r.DepartmentId == id && r.UserId == dto.UserId);
                existingRep.IsPrimaryAdmin = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message      = $"User '{user.Username}' assigned to department '{department.DepartmentName}' successfully",
                userId       = dto.UserId,
                departmentId = id,
                isPrimaryAdmin = dto.IsPrimaryAdmin
            });
        }

        // ============================================
        // DELETE /api/departments/{id}/remove-user/{userId}
        // Xóa user khỏi phòng ban (xóa khỏi DEPARTMENT_REPRESENTATIVE
        // và clear PrimaryDepartmentId)
        // ============================================
        [HttpDelete("{id}/remove-user/{userId}")]
        public async Task<IActionResult> RemoveUserFromDepartment(int id, int userId)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound("Department not found");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Xóa khỏi DEPARTMENT_REPRESENTATIVE
            var rep = await _context.DepartmentRepresentatives
                .FirstOrDefaultAsync(r => r.DepartmentId == id && r.UserId == userId);
            if (rep != null)
                _context.DepartmentRepresentatives.Remove(rep);

            // Clear PrimaryDepartmentId nếu đang trỏ vào department này
            if (user.PrimaryDepartmentId == id)
                user.PrimaryDepartmentId = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{user.Username}' removed from department successfully" });
        }

        // ============================================
        // GET /api/departments/{id}/available-users
        // Lấy danh sách users chưa thuộc department này
        // (dùng trong dropdown khi assign user)
        // ============================================
        [HttpGet("{id}/available-users")]
        public async Task<IActionResult> GetAvailableUsers(int id)
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted && u.PrimaryDepartmentId != id)
                .Include(u => u.Role)
                .Include(u => u.Department)
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserId,
                    u.UserCode,
                    u.Username,
                    u.Email,
                    RoleName          = u.Role != null ? u.Role.RoleName : "General Staff",
                    CurrentDepartment = u.Department != null ? u.Department.DepartmentName : "None"
                })
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(users);
        }
    }
}