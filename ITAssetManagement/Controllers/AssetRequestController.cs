using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.DTOs.Request;
using ITAssetManagement.Services;
using System.Text.Json;

namespace ITAssetManagement.Controllers
{
    /// <summary>
    /// Quản lý request thêm/sửa/xóa tài sản có approval workflow.
    ///
    /// Luồng:
    ///   Manager  → POST /api/asset-request/add     → request type 12 (ADD_ASSET)
    ///   Manager  → POST /api/asset-request/edit    → request type  1 (CHANGE)
    ///   Manager  → POST /api/asset-request/delete  → request type 13 (DELETE_ASSET)
    ///
    ///   Administrator thấy pending requests tại GET /api/request/pending/{approverId}
    ///   Administrator approve qua PUT /api/request/{requestId}/approve (dùng RequestController)
    ///
    ///   Khi approve:
    ///     ADD_ASSET    → tạo ITAsset mới từ request_data
    ///     CHANGE       → cập nhật các field trong RequestDetail (đã có sẵn)
    ///     DELETE_ASSET → soft-delete ITAsset (is_deleted = 1)
    /// </summary>
    [Route("api/asset-request")]
    [ApiController]
    public class AssetRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PermissionService    _permSvc;

        public AssetRequestController(ApplicationDbContext context, PermissionService permSvc)
        {
            _context = context;
            _permSvc = permSvc;
        }

        // ─────────────────────────────────────────────────────────
        // GET /api/asset-request/permission/{userId}
        // Trả về quyền của user – frontend dùng để ẩn/hiện nút
        // ─────────────────────────────────────────────────────────
        [HttpGet("permission/{userId}")]
        public async Task<IActionResult> GetPermission(int userId)
        {
            var perm = await _permSvc.GetPermissionAsync(userId);
            if (perm != null)
            {
                return Ok(new UserPermissionDto
                {
                    UserId     = userId,
                    CanView    = perm.CanView,
                    CanRequest = perm.CanRequest,
                    CanApprove = perm.CanApprove,
                    CanAdmin   = perm.CanAdmin
                });
            }

            // Fallback: suy từ role_id
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
            if (user == null) return NotFound("User not found");

            return Ok(new UserPermissionDto
            {
                UserId     = userId,
                CanView    = true,
                CanRequest = user.RoleId is 1 or 3 or 4 or 5,
                CanApprove = user.RoleId is 1 or 4,
                CanAdmin   = user.RoleId == 1
            });
        }

        // ─────────────────────────────────────────────────────────
        // POST /api/asset-request/add
        // Manager tạo request thêm mới tài sản
        // ─────────────────────────────────────────────────────────
        [HttpPost("add")]
        public async Task<IActionResult> RequestAddAsset([FromBody] AddAssetRequestDto dto)
        {
            // 1. Kiểm tra quyền
            if (!await _permSvc.CanRequestAsync(dto.UserCreatedId))
                return Forbid();

            // 2. Validate approver
            var firstApprover = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.FirstApproverId && !u.IsDeleted);
            if (firstApprover == null)
                return BadRequest(new { errors = new { FirstApproverId = "First approver not found." } });

            // Approver phải có quyền approve
            if (!await _permSvc.CanApproveAsync(dto.FirstApproverId))
                return BadRequest(new { errors = new { FirstApproverId = "Selected user does not have approval permission." } });

            if (dto.SecondApproverId.HasValue)
            {
                var secondApprover = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == dto.SecondApproverId.Value && !u.IsDeleted);
                if (secondApprover == null)
                    return BadRequest(new { errors = new { SecondApproverId = "Second approver not found." } });
                if (!await _permSvc.CanApproveAsync(dto.SecondApproverId.Value))
                    return BadRequest(new { errors = new { SecondApproverId = "Selected user does not have approval permission." } });
            }

            // 3. Validate required fields
            if (string.IsNullOrWhiteSpace(dto.AssetControlNumber))
                return BadRequest(new { errors = new { AssetControlNumber = "Asset control number is required." } });
            if (string.IsNullOrWhiteSpace(dto.AssetName))
                return BadRequest(new { errors = new { AssetName = "Asset name is required." } });

            // 4. Kiểm tra trùng control number
            var duplicate = await _context.ITAssets
                .AnyAsync(a => a.AssetControlNumber == dto.AssetControlNumber);
            if (duplicate)
                return BadRequest(new { errors = new { AssetControlNumber = "Asset control number already exists." } });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUser = GetCurrentUser();

                // Serialize toàn bộ thông tin vào request_data để apply khi approve
                var requestData = JsonSerializer.Serialize(new
                {
                    asset_control_number = dto.AssetControlNumber,
                    asset_name           = dto.AssetName,
                    manufacturer         = dto.Manufacturer,
                    model                = dto.Model,
                    serial_number        = dto.SerialNumber,
                    category_id          = dto.CategoryId,
                    status_id            = dto.StatusId,
                    location_id          = dto.LocationId,
                    department_id        = dto.DepartmentId
                });

                var request = new Request
                {
                    RequestTypeId      = 12, // ADD_ASSET
                    UserCreatedId      = dto.UserCreatedId,
                    TargetDiv          = null,
                    StatusId           = 1,  // Pending
                    RequestDescription = $"Add Asset Request: {dto.AssetName} ({dto.AssetControlNumber})",
                    RequestData        = requestData,
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                // RequestDetail lưu application_type để ApplyRequest dispatcher xử lý
                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = request.RequestId, FieldName = "application_type",      NewValue = "ADD_ASSET" },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "asset_control_number",  NewValue = dto.AssetControlNumber },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "asset_name",            NewValue = dto.AssetName },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "manufacturer",          NewValue = dto.Manufacturer },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "model",                 NewValue = dto.Model },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "serial_number",         NewValue = dto.SerialNumber },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "category_id",           NewValue = dto.CategoryId.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "status_id",             NewValue = dto.StatusId.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "location_id",           NewValue = dto.LocationId.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "department_id",         NewValue = dto.DepartmentId.ToString() }
                });

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = request.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = request.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Add asset request submitted: {dto.AssetName} ({dto.AssetControlNumber})"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Add asset request submitted. Pending approval.",
                    requestId       = request.RequestId,
                    approveEndpoint = $"PUT api/request/{request.RequestId}/approve",
                    status          = "Pending"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────
        // POST /api/asset-request/edit/{assetId}
        // Manager tạo request chỉnh sửa tài sản
        // (Reuse ChangeRequestDto + RequestController.CreateChangeRequest
        //  nhưng thêm permission check ở đây)
        // ─────────────────────────────────────────────────────────
        [HttpPost("edit/{assetId}")]
        public async Task<IActionResult> RequestEditAsset(int assetId, [FromBody] ChangeRequestDto dto)
        {
            // Permission check
            if (!await _permSvc.CanRequestAsync(dto.UserCreatedId))
                return Forbid();

            var asset = await _context.ITAssets.FindAsync(assetId);
            if (asset == null)
                return NotFound("Asset not found");

            // Approver phải có quyền approve
            if (!await _permSvc.CanApproveAsync(dto.ApproverId))
                return BadRequest(new { errors = new { ApproverId = "Selected approver does not have approval permission." } });

            if (dto.Changes == null || !dto.Changes.Any())
                return BadRequest(new { message = "No changes provided." });

            var currentUser = GetCurrentUser();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = new Request
                {
                    RequestTypeId      = 1, // CHANGE
                    UserCreatedId      = dto.UserCreatedId,
                    AssetId            = assetId,
                    TargetDiv          = null,
                    StatusId           = 1,
                    RequestDescription = dto.Description ?? $"Edit Asset Request: {asset.AssetName}",
                    FirstApproverId    = dto.ApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                foreach (var change in dto.Changes)
                {
                    _context.RequestDetails.Add(new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = change.FieldName,
                        OldValue  = change.OldValue,
                        NewValue  = change.NewValue
                    });
                }

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = request.RequestId,
                    ApproverId    = dto.ApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Edit request for asset {asset.AssetControlNumber}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Edit asset request submitted. Pending approval.",
                    requestId       = request.RequestId,
                    approveEndpoint = $"PUT api/request/{request.RequestId}/approve",
                    status          = "Pending"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────
        // POST /api/asset-request/delete
        // Manager tạo request xóa tài sản
        // ─────────────────────────────────────────────────────────
        [HttpPost("delete")]
        public async Task<IActionResult> RequestDeleteAsset([FromBody] DeleteAssetRequestDto dto)
        {
            // Permission check
            if (!await _permSvc.CanRequestAsync(dto.UserCreatedId))
                return Forbid();

            var asset = await _context.ITAssets.FindAsync(dto.AssetId);
            if (asset == null)
                return NotFound("Asset not found");

            if (!await _permSvc.CanApproveAsync(dto.FirstApproverId))
                return BadRequest(new { errors = new { FirstApproverId = "Selected approver does not have approval permission." } });

            if (dto.SecondApproverId.HasValue && !await _permSvc.CanApproveAsync(dto.SecondApproverId.Value))
                return BadRequest(new { errors = new { SecondApproverId = "Selected approver does not have approval permission." } });

            var currentUser = GetCurrentUser();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = new Request
                {
                    RequestTypeId      = 13, // DELETE_ASSET
                    UserCreatedId      = dto.UserCreatedId,
                    AssetId            = dto.AssetId,
                    TargetDiv          = null,
                    StatusId           = 1,
                    RequestDescription = dto.Reason ?? $"Delete Asset Request: {asset.AssetName} ({asset.AssetControlNumber})",
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = request.RequestId, FieldName = "application_type",     NewValue = "DELETE_ASSET" },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "asset_id",             NewValue = dto.AssetId.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "asset_control_number", NewValue = asset.AssetControlNumber },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "delete_reason",        NewValue = dto.Reason }
                });

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = request.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = request.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Delete request for asset {asset.AssetControlNumber}. Reason: {dto.Reason}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Delete asset request submitted. Pending approval.",
                    requestId       = request.RequestId,
                    approveEndpoint = $"PUT api/request/{request.RequestId}/approve",
                    assetId         = dto.AssetId,
                    assetName       = asset.AssetName,
                    status          = "Pending"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private string GetCurrentUser() => HttpContext?.User?.Identity?.Name ?? "system";
    }
}