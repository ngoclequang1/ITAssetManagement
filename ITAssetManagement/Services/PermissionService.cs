using ITAssetManagement.Data;
using ITAssetManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Services
{
    /// <summary>
    /// Tra cứu và kiểm tra quyền của user hiện tại.
    /// Mapping role_id → permission:
    ///   2 = General Staff  → can_view only
    ///   4 = Administrator  → can_view + can_approve
    ///   5 = Manager        → can_view + can_request
    ///   1 = ADMIN          → all
    /// </summary>
    public class PermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────
        // Lấy permission record của user (nullable)
        // ─────────────────────────────────────────────────
        public async Task<UserPermission?> GetPermissionAsync(int userId)
        {
            return await _context.UserPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        // ─────────────────────────────────────────────────
        // Helpers – kiểm tra từng quyền
        // ─────────────────────────────────────────────────
        public async Task<bool> CanViewAsync(int userId)
        {
            var p = await GetPermissionAsync(userId);
            if (p == null) return await FallbackFromRoleAsync(userId, "view");
            return p.CanView;
        }

        public async Task<bool> CanRequestAsync(int userId)
        {
            var p = await GetPermissionAsync(userId);
            if (p == null) return await FallbackFromRoleAsync(userId, "request");
            return p.CanRequest;
        }

        public async Task<bool> CanApproveAsync(int userId)
        {
            var p = await GetPermissionAsync(userId);
            if (p == null) return await FallbackFromRoleAsync(userId, "approve");
            return p.CanApprove;
        }

        public async Task<bool> CanAdminAsync(int userId)
        {
            var p = await GetPermissionAsync(userId);
            if (p == null) return await FallbackFromRoleAsync(userId, "admin");
            return p.CanAdmin;
        }

        // ─────────────────────────────────────────────────
        // Fallback: nếu chưa có bản ghi USER_PERMISSION,
        // suy từ role_id của user
        // ─────────────────────────────────────────────────
        private async Task<bool> FallbackFromRoleAsync(int userId, string permType)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            return permType switch
            {
                "view"    => true, // tất cả role đều có thể xem
                "request" => user.RoleId is 1 or 3 or 4 or 5, // ADMIN, Dept Rep, Manager
                "approve" => user.RoleId is 1 or 4,       // ADMIN, Administrator
                "admin"   => user.RoleId == 1,
                _         => false
            };
        }

        // ─────────────────────────────────────────────────
        // Tạo mới permission record từ role_id (gọi khi add user)
        // ─────────────────────────────────────────────────
        public async Task EnsurePermissionCreatedAsync(int userId)
        {
            var exists = await _context.UserPermissions.AnyAsync(p => p.UserId == userId);
            if (exists) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var perm = BuildDefaultPermission(userId, user.RoleId);
            _context.UserPermissions.Add(perm);
            await _context.SaveChangesAsync();
        }

        private static UserPermission BuildDefaultPermission(int userId, int? roleId) =>
            roleId switch
            {
                1 => new UserPermission { UserId = userId, CanView = true, CanRequest = true, CanApprove = true, CanAdmin = true },
                4 => new UserPermission { UserId = userId, CanView = true, CanRequest = true, CanApprove = true, CanAdmin = false },
                5 => new UserPermission { UserId = userId, CanView = true, CanRequest = true, CanApprove = false, CanAdmin = false },
                3 => new UserPermission { UserId = userId, CanView = true, CanRequest = true, CanApprove = false, CanAdmin = false },
                _ => new UserPermission { UserId = userId, CanView = true, CanRequest = false, CanApprove = false, CanAdmin = false }
            };
    }
}