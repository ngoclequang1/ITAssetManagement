using ITAssetManagement.Data;
using ITAssetManagement.DTOs.Software;
using ITAssetManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Controllers
{
    [Route("api/software")]
    [ApiController]
    public class SoftwareController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SoftwareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // 1. SEARCH SOFTWARE
        // ===============================
        [HttpPost("search")]
        public async Task<IActionResult> SearchSoftware([FromBody] SoftwareSearchDto dto)
        {
            var query = _context.Softwares.AsQueryable();

            if (!string.IsNullOrEmpty(dto.AssetControlNumber))
                query = query.Where(s =>
                    s.Asset != null &&
                    s.Asset.AssetControlNumber.Contains(dto.AssetControlNumber));

            if (!string.IsNullOrEmpty(dto.SoftwareName))
                query = query.Where(s => s.SoftwareName.Contains(dto.SoftwareName));

            if (!string.IsNullOrEmpty(dto.SoftwareVersion))
                query = query.Where(s =>
                    s.SoftwareVersion != null &&
                    s.SoftwareVersion.Contains(dto.SoftwareVersion));

            if (dto.VendorId.HasValue)
                query = query.Where(s => s.VendorId == dto.VendorId);

            if (dto.LicenseId.HasValue)
                query = query.Where(s => s.LicenseId == dto.LicenseId);

            if (!string.IsNullOrEmpty(dto.LicenseType))
                query = query.Where(s => s.LicenseType == dto.LicenseType);

            if (dto.GroupId.HasValue)
                query = query.Where(s => s.GroupId == dto.GroupId);

            var result = await query
                .Include(s => s.Asset)
                .Include(s => s.InstalledByUser)
                .AsNoTracking()
                .Select(s => new
                {
                    softwareId          = s.SoftwareId,
                    softwareName        = s.SoftwareName,
                    installationName    = s.SoftwareName,
                    softwareVersion     = s.SoftwareVersion,
                    softwareType        = s.SoftwareType ?? s.LicenseType,
                    assetControlNumber  = s.Asset != null ? s.Asset.AssetControlNumber : null,
                    assetId             = s.Asset != null ? s.Asset.AssetId : (int?)null,
                    assetName           = s.Asset != null ? s.Asset.AssetName : null,
                    licenseId           = s.LicenseId,
                    groupId             = s.GroupId,
                    installedByName     = s.InstalledByUser != null ? s.InstalledByUser.Username : null,
                    installedDate       = s.InstalledDate,
                    licenseType         = s.LicenseType,
                    description         = s.Description
                })
                .ToListAsync();

            return Ok(result);
        }

        // ===============================
        // 2. GET ALL SOFTWARE
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Softwares
                .Include(s => s.Asset)
                .Include(s => s.InstalledByUser)
                .AsNoTracking()
                .Select(s => new
                {
                    s.SoftwareId,
                    s.SoftwareName,
                    s.SoftwareVersion,
                    s.VendorId,
                    s.LicenseId,
                    s.LicenseType,
                    s.Description,
                    s.AssetId,
                    s.AssetControlNumber,
                    AssetName          = s.Asset != null ? s.Asset.AssetName : null,
                    s.GroupId,
                    s.InstalledBy,
                    InstalledByName    = s.InstalledByUser != null ? s.InstalledByUser.Username : null,
                    s.InstalledDate,
                    s.SoftwareType
                })
                .ToListAsync();

            return Ok(list);
        }

        // ===============================
        // 3. GET SOFTWARE DETAIL (View Details)
        // ===============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var software = await _context.Softwares
                .Include(s => s.Asset)
                    .ThenInclude(a => a!.Department)
                .Include(s => s.InstalledByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SoftwareId == id);

            if (software == null)
                return NotFound(new { message = "Software not found." });

            return Ok(new
            {
                software.SoftwareId,
                software.SoftwareName,
                software.SoftwareVersion,
                software.SoftwareType,
                software.LicenseType,
                software.LicenseId,
                software.VendorId,
                software.Description,
                software.GroupId,
                software.AssetId,
                software.AssetControlNumber,
                AssetName          = software.Asset?.AssetName,
                DepartmentName     = software.Asset?.Department?.DepartmentName,
                software.InstalledBy,
                InstalledByName    = software.InstalledByUser?.Username,
                software.InstalledDate
            });
        }

        // ===============================
        // 4. INSTALL SOFTWARE
        // ===============================
        [HttpPost("install")]
        public async Task<IActionResult> InstallSoftware([FromBody] InstallSoftwareDto dto)
        {
            if (string.IsNullOrEmpty(dto.SoftwareName) ||
                string.IsNullOrEmpty(dto.SoftwareVersion) ||
                string.IsNullOrEmpty(dto.AssetControlNumber))
                return BadRequest(new { message = "Missing required fields: SoftwareName, SoftwareVersion, AssetControlNumber." });

            var asset = await _context.ITAssets
                .FirstOrDefaultAsync(a => a.AssetControlNumber == dto.AssetControlNumber);

            if (asset == null)
                return NotFound(new { message = "Asset not found." });

            var existed = await _context.Softwares
                .FirstOrDefaultAsync(s =>
                    s.SoftwareName == dto.SoftwareName &&
                    s.SoftwareVersion == dto.SoftwareVersion &&
                    s.AssetId == asset.AssetId);

            if (existed != null)
                return BadRequest(new { message = "Software already installed on this asset." });

            var software = new Software
            {
                SoftwareName       = dto.SoftwareName,
                SoftwareVersion    = dto.SoftwareVersion,
                VendorId           = dto.VendorId,
                LicenseId          = dto.LicenseId,
                LicenseType        = dto.LicenseType,
                Description        = dto.Description,
                AssetId            = asset.AssetId,
                AssetControlNumber = asset.AssetControlNumber,
                InstalledBy        = dto.InstalledBy,
                InstalledDate      = DateTime.UtcNow
            };

            _context.Softwares.Add(software);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message            = "Software installed successfully.",
                softwareId         = software.SoftwareId,
                assetId            = asset.AssetId,
                assetControlNumber = asset.AssetControlNumber
            });
        }

        // ===============================
        // 5. CREATE SOFTWARE (no asset link)
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InstallSoftwareDto dto)
        {
            var software = new Software
            {
                SoftwareName    = dto.SoftwareName,
                SoftwareVersion = dto.SoftwareVersion,
                VendorId        = dto.VendorId,
                LicenseId       = dto.LicenseId,
                LicenseType     = dto.LicenseType,
                Description     = dto.Description,
                InstalledBy     = dto.InstalledBy,
                InstalledDate   = DateTime.UtcNow
            };

            _context.Softwares.Add(software);
            await _context.SaveChangesAsync();

            return Ok(software);
        }

        // ===============================
        // 6. UPDATE SOFTWARE
        // ===============================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] InstallSoftwareDto dto)
        {
            var software = await _context.Softwares.FindAsync(id);
            if (software == null)
                return NotFound(new { message = "Software not found." });

            software.SoftwareName    = dto.SoftwareName;
            software.SoftwareVersion = dto.SoftwareVersion;
            software.VendorId        = dto.VendorId;
            software.LicenseId       = dto.LicenseId;
            software.LicenseType     = dto.LicenseType;
            software.Description     = dto.Description;
            software.InstalledBy     = dto.InstalledBy;

            await _context.SaveChangesAsync();
            return Ok(software);
        }

        // ===============================
        // 7. UNINSTALL SOFTWARE (廃棄 – xóa trực tiếp, không cần approve)
        // Bảng SOFTWARE không có cột is_deleted → hard delete sau khi validate.
        // Người dùng confirm trên frontend trước khi gọi endpoint này.
        // ===============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Uninstall(int id)
        {
            var software = await _context.Softwares
                .Include(s => s.Asset)
                .FirstOrDefaultAsync(s => s.SoftwareId == id);

            if (software == null)
                return NotFound(new { message = "Software not found." });

            var softwareName    = software.SoftwareName;
            var softwareVersion = software.SoftwareVersion;
            var assetControlNo  = software.AssetControlNumber;

            _context.Softwares.Remove(software);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message         = "Software uninstalled successfully.",
                softwareId      = id,
                softwareName    = softwareName,
                softwareVersion = softwareVersion,
                assetControlNo  = assetControlNo
            });
        }

        // ===============================
        // 8. CHANGE APPLICATION (変更申請)
        // Tạo REQUEST qua approval workflow – KHÔNG cập nhật trực tiếp DB
        // ===============================
        [HttpPost("{id}/change-application")]
        public async Task<IActionResult> ChangeApplication(int id, [FromBody] SoftwareChangeApplicationDto dto)
        {
            // --- Validate software tồn tại ---
            var software = await _context.Softwares
                .Include(s => s.Asset)
                .FirstOrDefaultAsync(s => s.SoftwareId == id);

            if (software == null)
                return NotFound(new { message = "Software not found." });

            // --- Validate approver ---
            if (dto.FirstApproverId <= 0)
                return BadRequest(new { errors = new { FirstApproverId = "First approver is required." } });

            var firstApprover = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.FirstApproverId && !u.IsDeleted);
            if (firstApprover == null)
                return BadRequest(new { errors = new { FirstApproverId = "First approver not found." } });

            if (dto.SecondApproverId.HasValue)
            {
                var secondApprover = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == dto.SecondApproverId.Value && !u.IsDeleted);
                if (secondApprover == null)
                    return BadRequest(new { errors = new { SecondApproverId = "Second approver not found." } });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUser   = GetCurrentUser();
                var currentUserId = GetCurrentUserId();

                // Ghi nhận các field thay đổi
                var changeFields = new List<RequestDetail>();

                if (dto.SoftwareName != null && dto.SoftwareName != software.SoftwareName)
                    changeFields.Add(new RequestDetail { FieldName = "software_name",    OldValue = software.SoftwareName,    NewValue = dto.SoftwareName });

                if (dto.SoftwareVersion != null && dto.SoftwareVersion != software.SoftwareVersion)
                    changeFields.Add(new RequestDetail { FieldName = "software_version", OldValue = software.SoftwareVersion, NewValue = dto.SoftwareVersion });

                if (dto.LicenseType != null && dto.LicenseType != software.LicenseType)
                    changeFields.Add(new RequestDetail { FieldName = "license_type",     OldValue = software.LicenseType,     NewValue = dto.LicenseType });

                if (dto.SoftwareType != null && dto.SoftwareType != software.SoftwareType)
                    changeFields.Add(new RequestDetail { FieldName = "software_type",    OldValue = software.SoftwareType,    NewValue = dto.SoftwareType });

                if (dto.Description != null && dto.Description != software.Description)
                    changeFields.Add(new RequestDetail { FieldName = "description",      OldValue = software.Description,     NewValue = dto.Description });

                if (dto.VendorId.HasValue && dto.VendorId != software.VendorId)
                    changeFields.Add(new RequestDetail { FieldName = "vendor_id",        OldValue = software.VendorId?.ToString(), NewValue = dto.VendorId.Value.ToString() });

                if (dto.LicenseId.HasValue && dto.LicenseId != software.LicenseId)
                    changeFields.Add(new RequestDetail { FieldName = "license_id",       OldValue = software.LicenseId?.ToString(), NewValue = dto.LicenseId.Value.ToString() });

                if (!changeFields.Any())
                    return BadRequest(new { message = "No changes detected." });

                // Tạo REQUEST
                var request = new Request
                {
                    RequestTypeId      = 1, // CHANGE
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    StatusId           = 1, // Pending
                    RequestDescription = $"Change Application for Software: {software.SoftwareName} v{software.SoftwareVersion}",
                    RequestData        = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        software_id      = id,
                        software_name    = software.SoftwareName,
                        software_version = software.SoftwareVersion,
                        asset_control_no = software.AssetControlNumber
                    }),
                    FirstApproverId  = dto.FirstApproverId,
                    SecondApproverId = dto.SecondApproverId,
                    CreatedAt        = DateTime.UtcNow,
                    CreatedBy        = currentUser,
                    UpdatedAt        = DateTime.UtcNow,
                    UpdatedBy        = currentUser
                };
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                // Gắn RequestId và metadata vào changeFields
                changeFields.Add(new RequestDetail { FieldName = "application_type", NewValue = "CHANGE_SOFTWARE" });
                changeFields.Add(new RequestDetail { FieldName = "software_id",      NewValue = id.ToString() });
                foreach (var d in changeFields) d.RequestId = request.RequestId;
                _context.RequestDetails.AddRange(changeFields);

                // Approval levels
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

                // History
                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Change application for software {software.SoftwareName} v{software.SoftwareVersion}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Change application submitted. Pending approval.",
                    requestId       = request.RequestId,
                    approveEndpoint = $"PUT api/request/{request.RequestId}/approve",
                    changedFields   = changeFields
                        .Where(f => f.FieldName != "application_type" && f.FieldName != "software_id")
                        .Select(f => new { f.FieldName, f.OldValue, f.NewValue }),
                    status          = "Pending"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ===============================
        // 9. COPY REQUEST (コピー申請)
        // Tạo một bản sao của software record (cài đặt lên asset khác hoặc cùng asset)
        // Qua approval workflow
        // ===============================
        [HttpPost("{id}/copy-request")]
        public async Task<IActionResult> CopyRequest(int id, [FromBody] SoftwareCopyRequestDto dto)
        {
            var sourceSoftware = await _context.Softwares
                .Include(s => s.Asset)
                .FirstOrDefaultAsync(s => s.SoftwareId == id);

            if (sourceSoftware == null)
                return NotFound(new { message = "Source software not found." });

            // Validate target asset
            if (string.IsNullOrWhiteSpace(dto.TargetAssetControlNumber))
                return BadRequest(new { errors = new { TargetAssetControlNumber = "Target asset control number is required." } });

            var targetAsset = await _context.ITAssets
                .FirstOrDefaultAsync(a => a.AssetControlNumber == dto.TargetAssetControlNumber);
            if (targetAsset == null)
                return BadRequest(new { errors = new { TargetAssetControlNumber = "Target asset not found." } });

            // Không cho copy lên chính asset đó nếu đã có cùng phiên bản
            var duplicate = await _context.Softwares
                .FirstOrDefaultAsync(s =>
                    s.SoftwareName    == sourceSoftware.SoftwareName &&
                    s.SoftwareVersion == sourceSoftware.SoftwareVersion &&
                    s.AssetId         == targetAsset.AssetId);
            if (duplicate != null)
                return BadRequest(new { message = $"Software '{sourceSoftware.SoftwareName} v{sourceSoftware.SoftwareVersion}' is already installed on asset '{dto.TargetAssetControlNumber}'." });

            // Validate approver
            if (dto.FirstApproverId <= 0)
                return BadRequest(new { errors = new { FirstApproverId = "First approver is required." } });

            var firstApprover = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.FirstApproverId && !u.IsDeleted);
            if (firstApprover == null)
                return BadRequest(new { errors = new { FirstApproverId = "First approver not found." } });

            if (dto.SecondApproverId.HasValue)
            {
                var secondApprover = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == dto.SecondApproverId.Value && !u.IsDeleted);
                if (secondApprover == null)
                    return BadRequest(new { errors = new { SecondApproverId = "Second approver not found." } });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUser   = GetCurrentUser();
                var currentUserId = GetCurrentUserId();

                var requestData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    source_software_id         = id,
                    source_software_name       = sourceSoftware.SoftwareName,
                    source_software_version    = sourceSoftware.SoftwareVersion,
                    source_asset_control_no    = sourceSoftware.AssetControlNumber,
                    target_asset_id            = targetAsset.AssetId,
                    target_asset_control_no    = dto.TargetAssetControlNumber,
                    license_id                 = sourceSoftware.LicenseId,
                    license_type               = sourceSoftware.LicenseType,
                    description                = dto.Description ?? sourceSoftware.Description
                });

                var request = new Request
                {
                    RequestTypeId      = 3, // COPY
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    StatusId           = 1, // Pending
                    RequestDescription = $"Copy Request: {sourceSoftware.SoftwareName} v{sourceSoftware.SoftwareVersion} → {dto.TargetAssetControlNumber}",
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

                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = request.RequestId, FieldName = "application_type",       NewValue = "COPY_SOFTWARE" },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "source_software_id",     NewValue = id.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "target_asset_id",        NewValue = targetAsset.AssetId.ToString() },
                    new RequestDetail { RequestId = request.RequestId, FieldName = "target_asset_control_no",NewValue = dto.TargetAssetControlNumber }
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
                    Note      = $"Copy request: '{sourceSoftware.SoftwareName} v{sourceSoftware.SoftwareVersion}' from asset '{sourceSoftware.AssetControlNumber}' → '{dto.TargetAssetControlNumber}'"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message               = "Copy request submitted. Pending approval.",
                    requestId             = request.RequestId,
                    approveEndpoint       = $"PUT api/request/{request.RequestId}/approve",
                    sourceSoftwareName    = sourceSoftware.SoftwareName,
                    sourceVersion         = sourceSoftware.SoftwareVersion,
                    targetAssetControlNo  = dto.TargetAssetControlNumber,
                    status                = "Pending"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ===============================
        // 11. GROUP SOFTWARE
        // ===============================
        [HttpPost("group")]
        public async Task<IActionResult> GroupSoftware([FromBody] SoftwareGroupingDto dto)
        {
            if (dto.SoftwareIds == null || !dto.SoftwareIds.Any())
                return BadRequest(new { message = "SoftwareIds is required." });

            var softwares = await _context.Softwares
                .Where(s => dto.SoftwareIds.Contains(s.SoftwareId))
                .ToListAsync();

            if (!softwares.Any())
                return BadRequest(new { message = "No software found for the provided IDs." });

            // Sinh groupId mới nếu chưa chỉ định; dùng max existing + 1 thay vì Random để ít trùng hơn
            int groupId;
            if (dto.GroupId.HasValue && dto.GroupId.Value > 0)
            {
                groupId = dto.GroupId.Value;
            }
            else
            {
                var maxGroup = await _context.Softwares
                    .Where(s => s.GroupId != null)
                    .MaxAsync(s => (int?)s.GroupId) ?? 0;
                groupId = maxGroup + 1;
            }

            foreach (var s in softwares)
                s.GroupId = groupId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message       = "Grouped successfully.",
                groupId       = groupId,
                groupedCount  = softwares.Count,
                softwareIds   = softwares.Select(s => s.SoftwareId)
            });
        }

        // ===============================
        // 12. UNGROUP SOFTWARE
        // ===============================
        [HttpPost("ungroup")]
        public async Task<IActionResult> UngroupSoftware([FromBody] SoftwareGroupingDto dto)
        {
            if (dto.SoftwareIds == null || !dto.SoftwareIds.Any())
                return BadRequest(new { message = "SoftwareIds is required." });

            var softwares = await _context.Softwares
                .Where(s => dto.SoftwareIds.Contains(s.SoftwareId))
                .ToListAsync();

            if (!softwares.Any())
                return BadRequest(new { message = "No software found for the provided IDs." });

            foreach (var s in softwares)
                s.GroupId = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message        = "Ungrouped successfully.",
                ungroupedCount = softwares.Count,
                softwareIds    = softwares.Select(s => s.SoftwareId)
            });
        }

        // ===============================
        // 13. GET GROUP DETAILS
        // ===============================
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            var members = await _context.Softwares
                .Include(s => s.Asset)
                .AsNoTracking()
                .Where(s => s.GroupId == groupId)
                .Select(s => new
                {
                    s.SoftwareId,
                    s.SoftwareName,
                    s.SoftwareVersion,
                    s.SoftwareType,
                    s.LicenseType,
                    s.AssetControlNumber,
                    AssetName    = s.Asset != null ? s.Asset.AssetName : null,
                    s.InstalledDate
                })
                .ToListAsync();

            if (!members.Any())
                return NotFound(new { message = $"No software found in group {groupId}." });

            return Ok(new
            {
                groupId = groupId,
                count   = members.Count,
                members = members
            });
        }

        // ===============================
        // 14. APPLICATION HISTORY (lịch sử yêu cầu của 1 software)
        // ===============================
        [HttpGet("{id}/application-history")]
        public async Task<IActionResult> GetApplicationHistory(int id)
        {
            var software = await _context.Softwares.FindAsync(id);
            if (software == null)
                return NotFound(new { message = "Software not found." });

            // Tìm tất cả request liên quan đến software_id này
            var relatedRequestIds = await _context.RequestDetails
                .Where(d => d.FieldName == "software_id" && d.NewValue == id.ToString())
                .Select(d => d.RequestId)
                .Distinct()
                .ToListAsync();

            var history = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.UserCreated)
                .Include(r => r.Status)
                .Where(r => relatedRequestIds.Contains(r.RequestId))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    ApplicationId   = r.RequestId,
                    Applicant       = r.UserCreated != null ? r.UserCreated.Username : null,
                    ApplicationDate = r.CreatedAt,
                    ApplicationType = r.RequestType != null ? r.RequestType.TypeName : null,
                    Status          = r.Status != null ? r.Status.StatusName : null,
                    Description     = r.RequestDescription
                })
                .ToListAsync();

            return Ok(history);
        }

        // ===============================
        // 15. INVENTORY HISTORY (lịch sử kiểm kê của asset chứa software)
        // ===============================
        [HttpGet("{id}/inventory-history")]
        public async Task<IActionResult> GetInventoryHistory(int id)
        {
            var software = await _context.Softwares
                .Include(s => s.Asset)
                .FirstOrDefaultAsync(s => s.SoftwareId == id);

            if (software == null)
                return NotFound(new { message = "Software not found." });

            if (software.AssetId == null)
                return Ok(new
                {
                    message = "This software is not linked to any asset. No inventory history available.",
                    data    = Array.Empty<object>()
                });

            // Lấy lịch sử inventory từ bảng INVENTORY_HISTORY thông qua IT_ASSET
            var histories = await _context.InventoryHistories
                .Where(h => h.InventoryDepartmentId != null) // join qua asset nếu cần
                .OrderByDescending(h => h.InventoryDate)
                .Take(50)
                .Select(h => new
                {
                    h.InventoryId,
                    InventoryDate   = h.InventoryDate,
                    InventoryStatus = h.InventoryStatus,
                    Remarks         = h.Remarks
                })
                .ToListAsync();

            // Ưu tiên trả lịch sử request liên quan đến asset của software
            var assetId = software.AssetId.Value;
            var invRequests = await _context.RequestDetails
                .Where(d => d.FieldName == "asset_id" && d.NewValue == assetId.ToString())
                .Select(d => d.RequestId)
                .Distinct()
                .ToListAsync();

            var requestHistory = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.UserCreated)
                .Where(r => invRequests.Contains(r.RequestId))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    ApplicationId   = r.RequestId,
                    Applicant       = r.UserCreated != null ? r.UserCreated.Username : null,
                    ApplicationDate = r.CreatedAt,
                    ApplicationType = r.RequestType != null ? r.RequestType.TypeName : null,
                    Description     = r.RequestDescription
                })
                .ToListAsync();

            return Ok(new
            {
                softwareId     = id,
                softwareName   = software.SoftwareName,
                assetId        = software.AssetId,
                assetControlNo = software.AssetControlNumber,
                inventoryHistory = requestHistory
            });
        }

        // ===============================
        // PRIVATE HELPERS
        // ===============================
        private string GetCurrentUser()
            => HttpContext?.User?.Identity?.Name ?? "system";

        private int GetCurrentUserId()
        {
            if (int.TryParse(HttpContext?.User?.FindFirst("user_id")?.Value, out int uid))
                return uid;
            return 0;
        }
    }
}