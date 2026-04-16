using ITAssetManagement.Data;
using ITAssetManagement.DTOs;
using ITAssetManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ITAssetManagement.Controllers
{
    [ApiController]
    [Route("api/departments")]
    //[Authorize(Roles = "ADMIN")]
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
        // GET: api/departments
        [HttpGet]
        public async Task<IActionResult> GetDepartments([FromQuery] DepartmentQuery query)
        {
            // Validate paging
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var departments = _context.Departments
                .Include(d => d.ParentDepartment)
                .AsNoTracking()
                .AsQueryable();

            // Filter obsolete departments
            if (!query.ViewObsolete)
            {
                departments = departments.Where(d => d.IsActive);
            }

            // Search keyword
            if (!string.IsNullOrEmpty(query.Keyword))
            {
                departments = departments.Where(d =>
                    (d.DepartmentCode != null && d.DepartmentCode.Contains(query.Keyword)) ||
                    (d.DepartmentName != null && d.DepartmentName.Contains(query.Keyword)));
            }

            var total = await departments.CountAsync();

            var data = await departments
                .OrderBy(d => d.DepartmentCode)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentCode = d.DepartmentCode,
                    DepartmentName = d.DepartmentName,

                    ParentDepartmentCode = d.ParentDepartment != null
                        ? d.ParentDepartment.DepartmentCode
                        : null,

                    TopDepartmentName = d.ParentDepartment != null
                        ? d.ParentDepartment.DepartmentName
                        : d.DepartmentName,

                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page = query.Page,
                pageSize = query.PageSize,
                data
            });
        }

        // =========================
        // View Department Detail
        // =========================

        // GET: api/departments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentDetails(int id)
        {
            var department = await _context.Departments
                .Include(d => d.ParentDepartment)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
                return NotFound();

            var representatives = await _context.DepartmentRepresentatives
                .Where(r => r.DepartmentId == id)
                .Select(r => new
                {
                    Permission = r.Role.RoleName,
                    PrimaryAdminFlag = r.IsPrimaryAdmin,
                    UserCode = r.User.UserCode,
                    Username = r.User.Username,
                    PrimaryDepartmentCode = r.User.Department.DepartmentCode,
                    MainDepartmentName = r.User.Department.DepartmentName
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

            department.DepartmentName = dto.DepartmentName;
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
                message = dto.IsActive ? "Department activated" : "Department marked as obsolete",
                departmentId = department.DepartmentId,
                isActive = department.IsActive
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
                DepartmentCode = department.DepartmentCode + "_COPY",
                DepartmentName = department.DepartmentName + " Copy",
                ParentDepartmentId = department.ParentDepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(newDepartment);
            await _context.SaveChangesAsync();

            return Ok(newDepartment);
        }

        // =========================
        // Create Department
        // =========================
        // POST: api/departments
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DepartmentCode))
                return BadRequest("DepartmentCode is required");

            // check duplicate department code
            var exists = await _context.Departments
                .AnyAsync(d => d.DepartmentCode == dto.DepartmentCode);

            if (exists)
                return BadRequest("Department code already exists");

            // check parent department
            if (dto.ParentDepartmentId != null)
            {
                var parentExists = await _context.Departments
                    .AnyAsync(d => d.DepartmentId == dto.ParentDepartmentId);

                if (!parentExists)
                    return BadRequest("Parent department not found");
            }

            var department = new Department
            {
                DepartmentCode = dto.DepartmentCode,
                DepartmentName = dto.DepartmentName,
                ParentDepartmentId = dto.ParentDepartmentId,
                IsKittingDepartment = dto.IsKittingDepartment,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Department created successfully",
                departmentId = department.DepartmentId
            });
        }
    }
}