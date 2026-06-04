using ITAssetManagement.Data;
using ITAssetManagement.DTOs.License;
using ITAssetManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Controllers
{
    [Route("api/license")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LicenseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // 1. SEARCH LICENSE (POST)
        // ===============================
        [HttpPost("search")]
        public async Task<IActionResult> SearchLicense([FromBody] LicenseSearchDto dto)
        {
            if (dto.Page <= 0) dto.Page = 1;
            if (dto.PageSize <= 0) dto.PageSize = 10;

            var query = _context.Licenses
                .Include(l => l.ManagementDepartment)
                .Include(l => l.Manager)
                .Include(l => l.ParentLicense)
                .Where(l => !l.IsDeleted)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.LicenseManagementNumber))
                query = query.Where(l => l.LicenseManagementNumber != null &&
                    l.LicenseManagementNumber.Contains(dto.LicenseManagementNumber));

            if (!string.IsNullOrWhiteSpace(dto.InstallationName))
                query = query.Where(l => l.InstallationName != null &&
                    l.InstallationName.Contains(dto.InstallationName));

            if (!string.IsNullOrWhiteSpace(dto.PublisherName))
                query = query.Where(l => l.PublisherName != null &&
                    l.PublisherName.Contains(dto.PublisherName));

            if (!string.IsNullOrWhiteSpace(dto.SoftwareType))
                query = query.Where(l => l.LicenseType == dto.SoftwareType);

            if (!string.IsNullOrWhiteSpace(dto.LicenseType))
                query = query.Where(l => l.LicenseType == dto.LicenseType);

            if (!string.IsNullOrWhiteSpace(dto.CountingMethod))
                query = query.Where(l => l.CountingMethod == dto.CountingMethod);

            if (!string.IsNullOrWhiteSpace(dto.LicenseStatus))
                query = query.Where(l => l.LicenseStatus == dto.LicenseStatus);

            if (dto.ManagementDepartmentId.HasValue)
                query = query.Where(l => l.ManagementDepartmentId == dto.ManagementDepartmentId);

            if (dto.AcademicFlag.HasValue)
                query = query.Where(l => l.AcademicFlag == dto.AcademicFlag);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(l => new LicenseListResponse
                {
                    LicenseId                     = l.LicenseId,
                    LicenseManagementNumber       = l.LicenseManagementNumber,
                    InstallationName              = l.InstallationName,
                    PublisherName                 = l.PublisherName,
                    LicenseStatus                 = l.LicenseStatus,
                    HasInventory                  = l.InventoryHistories != null &&
                        l.InventoryHistories.Any(h => h.InventoryStatus == "Completed" && !h.IsDeleted),
                    IsLinked                      = _context.Softwares.Any(s => s.LicenseId == l.LicenseId),
                    IsUnstocked                   = l.NumberAvailable == l.NumberOfLicenses &&
                        !_context.Softwares.Any(s => s.LicenseId == l.LicenseId),
                    NumberOfLicenses              = l.NumberOfLicenses,
                    NumberAvailable               = l.NumberAvailable,
                    CountingMethod                = l.CountingMethod,
                    SoftwareType                  = l.SoftwareType,
                    LicenseType                   = l.LicenseType,
                    LicenseFormat                 = l.LicenseFormat,
                    AcademicFlag                  = l.AcademicFlag,
                    ManagementDepartmentCode      = l.ManagementDepartment != null
                        ? l.ManagementDepartment.DepartmentCode : null,
                    ManagementDepartmentName      = l.ManagementDepartment != null
                        ? l.ManagementDepartment.DepartmentName : null,
                    ManagerUsername               = l.Manager != null ? l.Manager.Username : null,
                    ParentLicenseId               = l.ParentLicenseId,
                    ParentLicenseManagementNumber = l.ParentLicense != null
                        ? l.ParentLicense.LicenseManagementNumber : null,
                    ExpiryDate                    = l.ExpiryDate,
                    DisposalDate                  = l.DisposalDate,
                    CreatedAt                     = l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page = dto.Page, pageSize = dto.PageSize, data });
        }

        // ===============================
        // 2. GET ALL LICENSE
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Licenses
                .Include(l => l.ManagementDepartment)
                .Include(l => l.Manager)
                .Where(l => !l.IsDeleted)
                .AsNoTracking();

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LicenseListResponse
                {
                    LicenseId                     = l.LicenseId,
                    LicenseManagementNumber       = l.LicenseManagementNumber,
                    InstallationName              = l.InstallationName,
                    PublisherName                 = l.PublisherName,
                    LicenseStatus                 = l.LicenseStatus,
                    HasInventory                  = l.InventoryHistories != null &&
                        l.InventoryHistories.Any(h => h.InventoryStatus == "Completed" && !h.IsDeleted),
                    IsLinked                      = _context.Softwares.Any(s => s.LicenseId == l.LicenseId),
                    IsUnstocked                   = l.NumberAvailable == l.NumberOfLicenses &&
                        !_context.Softwares.Any(s => s.LicenseId == l.LicenseId),
                    NumberOfLicenses              = l.NumberOfLicenses,
                    NumberAvailable               = l.NumberAvailable,
                    CountingMethod                = l.CountingMethod,
                    SoftwareType                  = l.SoftwareType,
                    LicenseType                   = l.LicenseType,
                    LicenseFormat                 = l.LicenseFormat,
                    AcademicFlag                  = l.AcademicFlag,
                    ManagementDepartmentCode      = l.ManagementDepartment != null
                        ? l.ManagementDepartment.DepartmentCode : null,
                    ManagementDepartmentName      = l.ManagementDepartment != null
                        ? l.ManagementDepartment.DepartmentName : null,
                    ManagerUsername               = l.Manager != null ? l.Manager.Username : null,
                    ParentLicenseId               = l.ParentLicenseId,
                    ExpiryDate                    = l.ExpiryDate,
                    DisposalDate                  = l.DisposalDate,
                    CreatedAt                     = l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, data });
        }

        // ===============================
        // 3. GET LICENSE DETAIL
        // ===============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var license = await _context.Licenses
                .Include(l => l.ManagementDepartment)
                .Include(l => l.Manager)
                .Include(l => l.ParentLicense)
                .Include(l => l.ChildLicenses)
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found");

            return Ok(new
            {
                license.LicenseId,
                license.LicenseManagementNumber,
                license.LicenseKey,
                license.InstallationName,
                license.PublisherName,
                license.SoftwareType,
                license.LicenseType,
                license.LicenseFormat,
                license.CountingMethod,
                license.AcademicFlag,
                license.NumberOfLicenses,
                license.NumberAvailable,
                license.LicenseStatus,
                license.ExpiryDate,
                license.DisposalDate,
                license.Description,
                license.CreatedAt,
                license.UpdatedAt,

                ManagementDepartmentId   = license.ManagementDepartmentId,
                ManagementDepartmentCode = license.ManagementDepartment?.DepartmentCode,
                ManagementDepartmentName = license.ManagementDepartment?.DepartmentName,

                ManagerUserId   = license.ManagerUserId,
                ManagerUsername = license.Manager?.Username,

                ParentLicenseId               = license.ParentLicenseId,
                ParentLicenseManagementNumber = license.ParentLicense?.LicenseManagementNumber,

                ChildLicenses = license.ChildLicenses?
                    .Where(c => !c.IsDeleted)
                    .Select(c => new { c.LicenseId, c.LicenseManagementNumber, c.NumberOfLicenses })
                    .ToList()
            });
        }

        // ===============================
        // 4. GET APPLICATION HISTORY
        // ===============================
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetApplicationHistory(int id)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found");

            var relatedRequestIds = await _context.RequestDetails
                .Where(d => d.FieldName == "license_id" && d.NewValue == id.ToString())
                .Select(d => d.RequestId)
                .Distinct()
                .ToListAsync();

            var history = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.UserCreated)
                .Where(r => relatedRequestIds.Contains(r.RequestId))
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

            return Ok(history);
        }

        // ===============================
        // 5. GET INVENTORY HISTORY
        // ===============================
        [HttpGet("{id}/inventory-history")]
        public async Task<IActionResult> GetInventoryHistory(int id)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found");

            var histories = await _context.LicenseInventoryHistories
                .Include(h => h.InventoryTaker)
                .Where(h => h.LicenseId == id && !h.IsDeleted)
                .OrderByDescending(h => h.InventoryDate)
                .Select(h => new
                {
                    h.InventoryId,
                    h.InventoryDate,
                    InventoryTaker  = h.InventoryTaker != null ? h.InventoryTaker.Username : null,
                    h.InventoryStatus,
                    h.Remarks
                })
                .ToListAsync();

            return Ok(histories);
        }

        // ===============================
        // 6. NEW APPLICATION (新規申請)
        //
        // FIX: LicenseManagementNumber được sinh và License record được tạo
        // ngay tại đây với status = "Pending", thay vì chờ đến khi Approve.
        // Khi Approve xong, ApplyNewLicense() chỉ cần update status → "Active".
        // ===============================
        [HttpPost("new-application")]
        public async Task<IActionResult> NewApplication([FromBody] LicenseNewApplicationDto dto)
        {
            // --- Validate input ---
            if (string.IsNullOrWhiteSpace(dto.InstallationName))
                return BadRequest(new { errors = new { InstallationName = "Installation name is required." } });

            if (string.IsNullOrWhiteSpace(dto.SoftwareType))
                return BadRequest(new { errors = new { SoftwareType = "Software type is required." } });

            if (string.IsNullOrWhiteSpace(dto.LicenseType))
                return BadRequest(new { errors = new { LicenseType = "License type is required." } });

            if (string.IsNullOrWhiteSpace(dto.CountingMethod))
                return BadRequest(new { errors = new { CountingMethod = "Counting method is required." } });

            if (dto.NumberOfLicenses <= 0)
                return BadRequest(new { errors = new { NumberOfLicenses = "Number of licenses must be greater than 0." } });

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
                var currentUserId = GetCurrentUserId();
                var currentUser   = GetCurrentUser();

                // --- Sinh LicenseManagementNumber ngay tại đây ---
                // Bao gồm cả các license đang Pending để tránh trùng số
                var lastNumber = await _context.Licenses
                    .Where(l => l.LicenseManagementNumber != null)
                    .OrderByDescending(l => l.LicenseId)
                    .Select(l => l.LicenseManagementNumber)
                    .FirstOrDefaultAsync();

                var newLicenseNumber = GenerateLicenseManagementNumber(lastNumber);

                // --- Tạo bản ghi License với status = "Pending" ---
                // Bản ghi này hiển thị ngay trong danh sách với số đã có.
                // Khi approve xong, status sẽ được chuyển sang "Active".
                var license = new License
                {
                    LicenseManagementNumber = newLicenseNumber,
                    InstallationName        = dto.InstallationName,
                    PublisherName           = dto.PublisherName,
                    SoftwareType            = dto.SoftwareType,
                    LicenseType             = dto.LicenseType,
                    LicenseFormat           = dto.LicenseFormat,
                    CountingMethod          = dto.CountingMethod,
                    AcademicFlag            = dto.AcademicFlag,
                    LicenseKey              = dto.LicenseKey,
                    NumberOfLicenses        = dto.NumberOfLicenses,
                    // NumberAvailable = 0 khi Pending vì chưa được approve đưa vào sử dụng
                    NumberAvailable         = 0,
                    ManagementDepartmentId  = dto.ManagementDepartmentId,
                    ExpiryDate              = dto.ExpiryDate,
                    Description             = dto.Description,
                    LicenseStatus           = "Pending",
                    IsDeleted               = false,
                    CreatedAt               = DateTime.UtcNow,
                    CreatedBy               = currentUser,
                    UpdatedAt               = DateTime.UtcNow,
                    UpdatedBy               = currentUser
                };
                _context.Licenses.Add(license);
                await _context.SaveChangesAsync(); // SaveChanges để lấy license.LicenseId

                // --- Serialize request_data (giữ đầy đủ để audit log) ---
                var requestData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    license_management_number  = newLicenseNumber,
                    license_id                 = license.LicenseId,
                    installation_name          = dto.InstallationName,
                    publisher_name             = dto.PublisherName,
                    software_type              = dto.SoftwareType,
                    license_type               = dto.LicenseType,
                    license_format             = dto.LicenseFormat,
                    counting_method            = dto.CountingMethod,
                    academic_flag              = dto.AcademicFlag,
                    license_key                = dto.LicenseKey,
                    number_of_licenses         = dto.NumberOfLicenses,
                    expiry_date                = dto.ExpiryDate,
                    description                = dto.Description,
                    management_department_id   = dto.ManagementDepartmentId
                });

                // --- Tạo Request (Pending) ---
                var request = new Request
                {
                    RequestTypeId      = 10, // NEW_LICENSE
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    TargetDiv          = null,
                    StatusId           = 1,  // Pending
                    RequestDescription = $"New License Application: {dto.InstallationName}",
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

                // --- RequestDetails ---
                // Lưu application_type và license_id để ApplyRequest định tuyến đúng
                // và ApplyNewLicense biết bản ghi License nào cần activate
                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "application_type",
                        NewValue  = "NEW_LICENSE"
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "license_id",
                        NewValue  = license.LicenseId.ToString()
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "license_management_number",
                        NewValue  = newLicenseNumber
                    }
                });

                // --- Approvals ---
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

                // --- History ---
                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = request.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"New license application submitted. License No: {newLicenseNumber}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message                 = "New license application submitted. Pending approval.",
                    requestId               = request.RequestId,
                    licenseId               = license.LicenseId,
                    licenseManagementNumber = newLicenseNumber,
                    licenseStatus           = "Pending",
                    approveEndpoint         = $"PUT api/request/{request.RequestId}/approve"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred.", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ===============================
        // 7. CHANGE APPLICATION (変更申請)
        // ===============================
        [HttpPost("{id}/change-application")]
        public async Task<IActionResult> ChangeApplication(int id, [FromBody] LicenseChangeApplicationDto dto)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found.");

            if (license.LicenseStatus == "Disposed")
                return BadRequest(new { message = "Cannot modify a disposed license." });

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
                var changeFields = new List<RequestDetail>();

                if (dto.PublisherName != null && dto.PublisherName != license.PublisherName)
                    changeFields.Add(new RequestDetail { FieldName = "publisher_name",   OldValue = license.PublisherName,  NewValue = dto.PublisherName });

                if (dto.SoftwareType != null && dto.SoftwareType != license.SoftwareType)
                    changeFields.Add(new RequestDetail { FieldName = "software_type",    OldValue = license.SoftwareType,   NewValue = dto.SoftwareType });

                if (dto.LicenseType != null && dto.LicenseType != license.LicenseType)
                    changeFields.Add(new RequestDetail { FieldName = "license_type",     OldValue = license.LicenseType,    NewValue = dto.LicenseType });

                if (dto.LicenseFormat != null && dto.LicenseFormat != license.LicenseFormat)
                    changeFields.Add(new RequestDetail { FieldName = "license_format",   OldValue = license.LicenseFormat,  NewValue = dto.LicenseFormat });

                if (dto.CountingMethod != null && dto.CountingMethod != license.CountingMethod)
                    changeFields.Add(new RequestDetail { FieldName = "counting_method",  OldValue = license.CountingMethod, NewValue = dto.CountingMethod });

                if (dto.AcademicFlag.HasValue && dto.AcademicFlag.Value != license.AcademicFlag)
                    changeFields.Add(new RequestDetail { FieldName = "academic_flag",    OldValue = license.AcademicFlag.ToString(), NewValue = dto.AcademicFlag.Value.ToString() });

                if (dto.NumberOfLicenses.HasValue && dto.NumberOfLicenses.Value != license.NumberOfLicenses)
                    changeFields.Add(new RequestDetail { FieldName = "number_of_licenses", OldValue = license.NumberOfLicenses.ToString(), NewValue = dto.NumberOfLicenses.Value.ToString() });

                if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value != license.ExpiryDate)
                    changeFields.Add(new RequestDetail { FieldName = "expiry_date",      OldValue = license.ExpiryDate?.ToString("o"), NewValue = dto.ExpiryDate.Value.ToString("o") });

                if (dto.Description != null && dto.Description != license.Description)
                    changeFields.Add(new RequestDetail { FieldName = "description",      OldValue = license.Description,   NewValue = dto.Description });

                if (dto.ManagementDepartmentId.HasValue && dto.ManagementDepartmentId.Value != license.ManagementDepartmentId)
                    changeFields.Add(new RequestDetail { FieldName = "management_department_id", OldValue = license.ManagementDepartmentId?.ToString(), NewValue = dto.ManagementDepartmentId.Value.ToString() });

                if (!changeFields.Any())
                    return BadRequest(new { message = "No changes detected." });

                var currentUserId = GetCurrentUserId();
                var currentUser   = GetCurrentUser();

                var req = new Request
                {
                    RequestTypeId      = 1, // CHANGE
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    TargetDiv          = null,
                    StatusId           = 1, // Pending
                    RequestDescription = $"Change Application for License: {license.LicenseManagementNumber}",
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(req);
                await _context.SaveChangesAsync();

                changeFields.Add(new RequestDetail { FieldName = "application_type", NewValue = "CHANGE_LICENSE" });
                changeFields.Add(new RequestDetail { FieldName = "license_id",       NewValue = id.ToString() });

                foreach (var detail in changeFields)
                    detail.RequestId = req.RequestId;

                _context.RequestDetails.AddRange(changeFields);

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = req.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = req.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = req.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Change application for license {license.LicenseManagementNumber}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Change application submitted. Pending approval.",
                    requestId       = req.RequestId,
                    approveEndpoint = $"PUT api/request/{req.RequestId}/approve",
                    changedFields   = changeFields
                        .Where(f => f.FieldName != "application_type" && f.FieldName != "license_id")
                        .Select(f => f.FieldName),
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
        // 8. MOVE APPLICATION (移動申請)
        // ===============================
        [HttpPost("{id}/move-application")]
        public async Task<IActionResult> MoveApplication(int id, [FromBody] LicenseMoveApplicationDto dto)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found.");

            if (license.LicenseStatus == "Disposed")
                return BadRequest(new { message = "Cannot move a disposed license." });

            var destDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == dto.DestinationDepartmentId && !d.IsDeleted);
            if (destDept == null)
                return BadRequest(new { errors = new { DestinationDepartmentId = "Destination department not found." } });

            if (dto.NewManagerUserId <= 0)
                return BadRequest(new { errors = new { NewManagerUserId = "Person in charge of the location is required." } });

            var newManager = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.NewManagerUserId && !u.IsDeleted);
            if (newManager == null)
                return BadRequest(new { errors = new { NewManagerUserId = "Person in charge not found." } });

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
                var currentUserId = GetCurrentUserId();
                var currentUser   = GetCurrentUser();

                var req = new Request
                {
                    RequestTypeId      = 2, // MOVE
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    TargetDiv          = null,
                    StatusId           = 1,
                    RequestDescription = $"Move Application for License: {license.LicenseManagementNumber}",
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(req);
                await _context.SaveChangesAsync();

                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = req.RequestId, FieldName = "application_type",          NewValue = "MOVE_LICENSE" },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "license_id",                NewValue = id.ToString() },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "destination_department_id", OldValue = license.ManagementDepartmentId?.ToString(), NewValue = dto.DestinationDepartmentId.ToString() },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "new_manager_user_id",       OldValue = license.ManagerUserId?.ToString(),           NewValue = dto.NewManagerUserId.ToString() }
                });

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = req.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = req.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = req.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Move license {license.LicenseManagementNumber} to department {destDept.DepartmentName}, new manager: {newManager.Username}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message               = "Move application submitted. Pending approval.",
                    requestId             = req.RequestId,
                    approveEndpoint       = $"PUT api/request/{req.RequestId}/approve",
                    destinationDepartment = destDept.DepartmentName,
                    newManager            = newManager.Username,
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
        // 9. SPLIT APPLICATION (分割申請)
        // ===============================
        [HttpPost("{id}/split-application")]
        public async Task<IActionResult> SplitApplication(int id, [FromBody] LicenseSplitApplicationDto dto)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found.");

            if (license.LicenseStatus == "Disposed")
                return BadRequest(new { message = "Cannot split a disposed license." });

            if (dto.SplitCount <= 0)
                return BadRequest(new { errors = new { SplitCount = "Split count must be greater than 0." } });

            if (dto.SplitCount > license.NumberAvailable)
                return BadRequest(new { errors = new { SplitCount = $"Split count ({dto.SplitCount}) cannot exceed available licenses ({license.NumberAvailable})." } });

            var destDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == dto.DestinationDepartmentId && !d.IsDeleted);
            if (destDept == null)
                return BadRequest(new { errors = new { DestinationDepartmentId = "Destination department not found." } });

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
                var currentUserId = GetCurrentUserId();
                var currentUser   = GetCurrentUser();

                var req = new Request
                {
                    RequestTypeId      = 11, // SPLIT
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    TargetDiv          = null,
                    StatusId           = 1,
                    RequestDescription = $"Split Application: {dto.SplitCount} licenses from {license.LicenseManagementNumber}",
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(req);
                await _context.SaveChangesAsync();

                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = req.RequestId, FieldName = "application_type",          NewValue = "SPLIT_LICENSE" },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "license_id",                NewValue = id.ToString() },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "split_count",               NewValue = dto.SplitCount.ToString() },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "destination_department_id", NewValue = dto.DestinationDepartmentId.ToString() }
                });

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = req.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = req.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = req.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Split {dto.SplitCount} licenses from {license.LicenseManagementNumber}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Split application submitted. Pending approval.",
                    requestId       = req.RequestId,
                    approveEndpoint = $"PUT api/request/{req.RequestId}/approve",
                    splitCount      = dto.SplitCount,
                    remaining       = license.NumberAvailable - dto.SplitCount,
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
        // 10. DISPOSAL APPLICATION (廃棄申請)
        // ===============================
        [HttpPost("{id}/disposal-application")]
        public async Task<IActionResult> DisposalApplication(int id, [FromBody] LicenseDisposalApplicationDto dto)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseId == id && !l.IsDeleted);

            if (license == null)
                return NotFound("License not found.");

            if (license.LicenseStatus == "Disposed")
                return BadRequest(new { message = "This license is already disposed." });

            bool isLinked = await _context.Softwares.AnyAsync(s => s.LicenseId == id);
            if (isLinked)
                return BadRequest(new
                {
                    message   = "Cannot dispose this license. It is still linked to software or devices. Please unlink all associations first.",
                    errorCode = "LICENSE_STILL_LINKED",
                    isLinked  = true
                });

            if (dto.DisposalDate == default)
                return BadRequest(new { errors = new { DisposalDate = "Disposal date is required." } });

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
                var currentUserId = GetCurrentUserId();
                var currentUser   = GetCurrentUser();

                var req = new Request
                {
                    RequestTypeId      = 4, // DISPOSAL
                    UserCreatedId      = currentUserId > 0 ? currentUserId : dto.FirstApproverId,
                    TargetDiv          = null,
                    StatusId           = 1,
                    RequestDescription = $"Disposal Application for License: {license.LicenseManagementNumber}",
                    FirstApproverId    = dto.FirstApproverId,
                    SecondApproverId   = dto.SecondApproverId,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = currentUser,
                    UpdatedAt          = DateTime.UtcNow,
                    UpdatedBy          = currentUser
                };
                _context.Requests.Add(req);
                await _context.SaveChangesAsync();

                _context.RequestDetails.AddRange(new[]
                {
                    new RequestDetail { RequestId = req.RequestId, FieldName = "application_type", NewValue = "DISPOSAL_LICENSE" },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "license_id",       NewValue = id.ToString() },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "disposal_date",    NewValue = dto.DisposalDate.ToString("o") },
                    new RequestDetail { RequestId = req.RequestId, FieldName = "remarks",          NewValue = dto.Remarks }
                });

                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId     = req.RequestId,
                    ApproverId    = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId      = 1
                });

                if (dto.SecondApproverId.HasValue)
                {
                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId     = req.RequestId,
                        ApproverId    = dto.SecondApproverId.Value,
                        ApprovalLevel = 2,
                        StatusId      = 1
                    });
                }

                _context.RequestHistories.Add(new RequestHistory
                {
                    RequestId = req.RequestId,
                    Action    = "Created",
                    ActionBy  = currentUser,
                    ActionAt  = DateTime.UtcNow,
                    Note      = $"Disposal application for license {license.LicenseManagementNumber}, disposal date: {dto.DisposalDate:yyyy-MM-dd}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message         = "Disposal application submitted. Pending approval.",
                    requestId       = req.RequestId,
                    approveEndpoint = $"PUT api/request/{req.RequestId}/approve",
                    expectedStatus  = "Disposed",
                    disposalDate    = dto.DisposalDate,
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
        // HELPER: Sinh License Management Number (LIC-0001, LIC-0002, ...)
        // Format 4 chữ số: LIC-XXXX
        // ===============================
        private static string GenerateLicenseManagementNumber(string? lastNumber)
        {
            if (string.IsNullOrEmpty(lastNumber))
                return "LIC-0001";

            // Hỗ trợ cả format cũ "LIC-00001" (5 chữ số) lẫn format mới "LIC-0001" (4 chữ số)
            var numPart = lastNumber.Replace("LIC-", "").TrimStart('0');
            if (int.TryParse(numPart, out int current))
                return $"LIC-{(current + 1):D4}";

            return "LIC-0001";
        }

        // ===============================
        // PRIVATE HELPER METHODS
        // ===============================
        private string GetCurrentUser() => HttpContext?.User?.Identity?.Name ?? "system";

        private int GetCurrentUserId()
        {
            if (int.TryParse(HttpContext?.User?.FindFirst("user_id")?.Value, out int uid))
                return uid;
            return 0;
        }
    }
}