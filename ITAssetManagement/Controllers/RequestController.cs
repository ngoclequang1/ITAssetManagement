using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.DTOs.Request;

namespace ITAssetManagement.Controllers
{
    [Route("api/request")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // 1. CREATE CHANGE REQUEST 
        // ===============================
        [HttpPost("change")]
        public async Task<IActionResult> CreateChangeRequest([FromBody] ChangeRequestDto dto)
        {
            var asset = await _context.ITAssets.FindAsync(dto.AssetId);
            if (asset == null)
                return NotFound("Asset not found");

            var request = new Request
            {
                RequestTypeId = 1,
                UserCreatedId = dto.UserCreatedId,
                AssetId = dto.AssetId,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // save change
            foreach (var change in dto.Changes)
            {
                _context.RequestDetails.Add(new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = change.FieldName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue
                });
            }

            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId, 
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Change request created",
                requestId = request.RequestId
            });
        }

        // ===============================
        // 2. CREATE MOVE REQUEST 
        // ===============================
        [HttpPost("move")]
        public async Task<IActionResult> CreateMoveRequest([FromBody] MoveRequestDto dto)
        {
            var asset = await _context.ITAssets.FindAsync(dto.AssetId);
            if (asset == null)
                return NotFound("Asset not found");

            var request = new Request
            {
                RequestTypeId = 2,
                UserCreatedId = dto.UserCreatedId,
                AssetId = dto.AssetId,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            _context.RequestDetails.Add(new RequestDetail
            {
                RequestId = request.RequestId,
                FieldName = "department_id",
                OldValue = asset.DepartmentId.ToString(),
                NewValue = dto.MoveToDepartmentId.ToString()
            });

            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.FirstApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            // Kiểm tra: Nếu Frontend có gửi lên người duyệt cấp 2 (không bị null) thì mới tạo thêm
            if (dto.SecondApproverId.HasValue)
            {
                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId = request.RequestId,
                    ApproverId = dto.SecondApproverId.Value,
                    ApprovalLevel = 2,
                    StatusId = 1
                });
            }

            await _context.SaveChangesAsync();
            return Ok("Move request created");
        }

        // ===============================
        // 3. APPROVE REQUEST 
        // ===============================
        [HttpPut("{requestId}/approve")]
        public async Task<IActionResult> ApproveRequest(int requestId, [FromBody] ApproveRequestDto dto)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null)
                return NotFound();

            if (request.StatusId == 2)
                return BadRequest("Request already approved");

            var approval = await _context.RequestApprovals
                .FirstOrDefaultAsync(a => a.RequestId == requestId && a.ApproverId == dto.ApproverId);

            if (approval == null)
                return BadRequest("Not allowed");

        
            if (approval.StatusId == 2)
                return BadRequest("Already approved by this user");

            // check level trước
            var minPendingLevel = await _context.RequestApprovals
                .Where(a => a.RequestId == requestId && a.StatusId == 1)
                .MinAsync(a => a.ApprovalLevel);

            if (approval.ApprovalLevel != minPendingLevel)
                return BadRequest("You must approve in order");

            // update approval
            approval.StatusId = 2;
            approval.ApprovedAt = DateTime.Now;
            approval.Remarks = dto.Remarks;

            await _context.SaveChangesAsync();

            // check còn pending không
            var stillPending = await _context.RequestApprovals
                .AnyAsync(a => a.RequestId == requestId && a.StatusId == 1);

            if (!stillPending)
            {
                // ALL APPROVED → APPLY
                await ApplyRequest(requestId);
                return Ok("Approved and applied");
            }

            return Ok("Approved (waiting next level)");
        }

        // ===============================
        // BỔ SUNG: REJECT REQUEST 
        // ===============================
        [HttpPut("{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId, [FromBody] ApproveRequestDto dto)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            var approval = await _context.RequestApprovals
                .FirstOrDefaultAsync(a => a.RequestId == requestId && a.ApproverId == dto.ApproverId);

            if (approval == null) return BadRequest("Not allowed");
            if (approval.StatusId != 1) return BadRequest("Already processed");

            // Cập nhật trạng thái Approval = 3 (Rejected)
            approval.StatusId = 3;
            approval.ApprovedAt = DateTime.Now;
            approval.Remarks = dto.Remarks;

            // Đánh rớt toàn bộ Request (StatusId = 3)
            request.StatusId = 3;

            _context.RequestHistories.Add(new RequestHistory
            {
                RequestId = requestId,
                StatusId = 3,
                UserCreatedId = request.UserCreatedId,
                Remarks = "Rejected: " + dto.Remarks
            });

            await _context.SaveChangesAsync();
            return Ok("Request rejected");
        }

        // ===============================
        // BỔ SUNG: LẤY CÁC REQUEST CHỜ DUYỆT CỦA USER
        // ===============================
        [HttpGet("pending/{approverId}")]
        public async Task<IActionResult> GetPendingApprovals(int approverId)
        {
            var pendingList = await _context.RequestApprovals
                .Include(a => a.Request)
                    .ThenInclude(r => r.RequestType)
                .Where(a => a.ApproverId == approverId && a.StatusId == 1 && a.Request.StatusId == 1) // Chưa duyệt
                .Select(a => new
                {
                    RequestId = a.RequestId,
                    Type = a.Request.RequestType.TypeName,
                    Description = a.Request.RequestDescription,
                    CreatedAt = a.Request.CreatedAt,
                    ApprovalLevel = a.ApprovalLevel
                })
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();

            return Ok(pendingList);
        }

        // ===============================
        // 4. APPLY REQUEST
        // Router: phân loại IT Asset request vs License request
        // dựa vào RequestDetail có field "application_type"
        // ===============================
        private async Task ApplyRequest(int requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);

            var details = await _context.RequestDetails
                .Where(d => d.RequestId == requestId)
                .ToListAsync();

            // Kiểm tra đây có phải License request không
            var appType = details.FirstOrDefault(d => d.FieldName == "application_type")?.NewValue;

            if (appType != null && (appType.EndsWith("_LICENSE") || appType == "NEW_LICENSE"))
            {
                // 🔹 LICENSE REQUEST → xử lý riêng
                await ApplyLicenseRequest(request, details, appType);
            }
            else
            {
                // 🔹 IT ASSET REQUEST — giữ nguyên logic cũ
                if (request.AssetId != null)
                {
                    var asset = await _context.ITAssets.FindAsync(request.AssetId);
                    ApplyToAsset(asset, details);
                }
                else
                {
                    var requestAssets = await _context.Set<RequestAsset>()
                        .Where(x => x.RequestId == requestId)
                        .ToListAsync();

                    foreach (var ra in requestAssets)
                    {
                        var asset = await _context.ITAssets.FindAsync(ra.AssetId);
                        ApplyToAsset(asset, details);
                    }
                }
            }

            request.StatusId = 2; // Approved

            _context.RequestHistories.Add(new RequestHistory
            {
                RequestId     = requestId,
                StatusId      = 2,
                UserCreatedId = request.UserCreatedId,
                Action        = "Approved",
                ActionBy      = request.UpdatedBy ?? "system",
                ActionAt      = DateTime.UtcNow,
                Remarks       = "Applied"
            });

            await _context.SaveChangesAsync();
        }

        // ===============================
        // 4a. APPLY LICENSE REQUEST
        // Dispatch theo application_type
        // ===============================
        private async Task ApplyLicenseRequest(Request request, List<RequestDetail> details, string appType)
        {
            switch (appType)
            {
                case "NEW_LICENSE":
                    await ApplyNewLicense(request, details);
                    break;
                case "CHANGE_LICENSE":
                    await ApplyChangeLicense(details);
                    break;
                case "MOVE_LICENSE":
                    await ApplyMoveLicense(details);
                    break;
                case "SPLIT_LICENSE":
                    await ApplySplitLicense(request, details);
                    break;
                case "DISPOSAL_LICENSE":
                    await ApplyDisposalLicense(details);
                    break;
            }
        }

        // ===============================
        // APPLY NEW LICENSE
        // ===============================
        private async Task ApplyNewLicense(Request request, List<RequestDetail> details)
        {
            // Lấy license_id đã lưu khi tạo request
            var licenseIdStr = details.FirstOrDefault(d => d.FieldName == "license_id")?.NewValue;
            if (!int.TryParse(licenseIdStr, out int licenseId))
                throw new InvalidOperationException("license_id missing in NEW_LICENSE request. Cannot activate license.");
 
            var license = await _context.Licenses.FindAsync(licenseId)
                ?? throw new InvalidOperationException($"License {licenseId} not found.");
 
            if (license.LicenseStatus != "Pending")
                throw new InvalidOperationException(
                    $"License {license.LicenseManagementNumber} is in status '{license.LicenseStatus}', expected 'Pending'.");
 
            // Activate: chuyển Pending → Active, mở NumberAvailable
            license.LicenseStatus   = "Active";
            license.NumberAvailable = license.NumberOfLicenses;
            license.UpdatedAt       = DateTime.UtcNow;
            license.UpdatedBy       = request.UpdatedBy ?? "system";
 
            // Ghi lại license_id vào RequestDetail để truy vết
            _context.RequestDetails.Add(new RequestDetail
            {
                RequestId = request.RequestId,
                FieldName = "activated_license_id",
                NewValue  = license.LicenseId.ToString()
            });
        }

        // Áp dụng thay đổi vào License hiện có (CHANGE_LICENSE)
        private async Task ApplyChangeLicense(List<RequestDetail> details)
        {
            var licenseIdStr = details.FirstOrDefault(d => d.FieldName == "license_id")?.NewValue;
            if (!int.TryParse(licenseIdStr, out int licenseId))
                throw new InvalidOperationException("license_id missing in CHANGE_LICENSE request.");

            var license = await _context.Licenses.FindAsync(licenseId)
                ?? throw new InvalidOperationException($"License {licenseId} not found.");

            foreach (var d in details)
            {
                switch (d.FieldName)
                {
                    case "publisher_name":      license.PublisherName  = d.NewValue; break;
                    case "software_type":       license.SoftwareType   = d.NewValue; break;
                    case "license_type":        license.LicenseType    = d.NewValue; break;
                    case "license_format":      license.LicenseFormat  = d.NewValue; break;
                    case "counting_method":     license.CountingMethod = d.NewValue; break;
                    case "academic_flag":       license.AcademicFlag   = bool.Parse(d.NewValue ?? "false"); break;
                    case "description":         license.Description    = d.NewValue; break;
                    case "expiry_date":
                        license.ExpiryDate = d.NewValue != null ? DateTime.Parse(d.NewValue) : null;
                        break;
                    case "number_of_licenses":
                        if (int.TryParse(d.NewValue, out int newCount))
                        {
                            int diff = newCount - license.NumberOfLicenses;
                            license.NumberOfLicenses = newCount;
                            license.NumberAvailable  = Math.Max(0, license.NumberAvailable + diff);
                        }
                        break;
                    case "management_department_id":
                        license.ManagementDepartmentId = int.TryParse(d.NewValue, out int deptId) ? deptId : null;
                        break;
                }
            }

            license.UpdatedAt = DateTime.UtcNow;
        }

        // Di chuyển License sang bộ phận khác và đổi người quản lý (MOVE_LICENSE)
        // Theo SKILL_LICENSE_MANAGEMENT §3.3: cập nhật cả Department lẫn Person in charge
        private async Task ApplyMoveLicense(List<RequestDetail> details)
        {
            var licenseIdStr = details.FirstOrDefault(d => d.FieldName == "license_id")?.NewValue;
            if (!int.TryParse(licenseIdStr, out int licenseId))
                throw new InvalidOperationException("license_id missing in MOVE_LICENSE request.");

            var license = await _context.Licenses.FindAsync(licenseId)
                ?? throw new InvalidOperationException($"License {licenseId} not found.");

            // Cập nhật phòng ban đích (Deployment destination)
            var destDeptDetail = details.FirstOrDefault(d => d.FieldName == "destination_department_id");
            if (destDeptDetail != null && int.TryParse(destDeptDetail.NewValue, out int destDeptId))
                license.ManagementDepartmentId = destDeptId;

            // Cập nhật người quản lý mới (Person in charge of the location)
            var newManagerDetail = details.FirstOrDefault(d => d.FieldName == "new_manager_user_id");
            if (newManagerDetail != null && int.TryParse(newManagerDetail.NewValue, out int newManagerId))
                license.ManagerUserId = newManagerId;

            license.UpdatedAt = DateTime.UtcNow;
        }

        // Tách license con từ license cha (SPLIT_LICENSE)
        private async Task ApplySplitLicense(Request request, List<RequestDetail> details)
        {
            var licenseIdStr = details.FirstOrDefault(d => d.FieldName == "license_id")?.NewValue;
            if (!int.TryParse(licenseIdStr, out int licenseId))
                throw new InvalidOperationException("license_id missing in SPLIT_LICENSE request.");

            var license = await _context.Licenses.FindAsync(licenseId)
                ?? throw new InvalidOperationException($"License {licenseId} not found.");

            var splitCountStr = details.FirstOrDefault(d => d.FieldName == "split_count")?.NewValue;
            if (!int.TryParse(splitCountStr, out int splitCount) || splitCount <= 0)
                throw new InvalidOperationException("Invalid split_count.");

            if (splitCount > license.NumberAvailable)
                throw new InvalidOperationException($"Not enough available licenses. Available: {license.NumberAvailable}, Requested: {splitCount}");

            var destDeptIdStr = details.FirstOrDefault(d => d.FieldName == "destination_department_id")?.NewValue;
            int.TryParse(destDeptIdStr, out int destDeptId);

            var lastNumber = await _context.Licenses
                .Where(l => l.LicenseManagementNumber != null)
                .OrderByDescending(l => l.LicenseId)
                .Select(l => l.LicenseManagementNumber)
                .FirstOrDefaultAsync();

            var childNumber = GenerateLicenseManagementNumber(lastNumber);
            var currentUser = request.UpdatedBy ?? "system";

            license.NumberAvailable -= splitCount;
            license.UpdatedAt        = DateTime.UtcNow;

            var childLicense = new License
            {
                LicenseManagementNumber = childNumber,
                ParentLicenseId         = license.LicenseId,
                InstallationName        = license.InstallationName,
                PublisherName           = license.PublisherName,
                SoftwareType            = license.SoftwareType,
                LicenseType             = license.LicenseType,
                LicenseFormat           = license.LicenseFormat,
                CountingMethod          = license.CountingMethod,
                AcademicFlag            = license.AcademicFlag,
                NumberOfLicenses        = splitCount,
                NumberAvailable         = splitCount,
                ManagementDepartmentId  = destDeptId > 0 ? destDeptId : license.ManagementDepartmentId,
                LicenseStatus           = "Active",
                ExpiryDate              = license.ExpiryDate,
                IsDeleted               = false,
                CreatedAt               = DateTime.UtcNow,
                CreatedBy               = currentUser,
                UpdatedAt               = DateTime.UtcNow,
                UpdatedBy               = currentUser
            };
            _context.Licenses.Add(childLicense);
            await _context.SaveChangesAsync();

            _context.RequestDetails.Add(new RequestDetail
            {
                RequestId = request.RequestId,
                FieldName = "child_license_id",
                NewValue  = childLicense.LicenseId.ToString()
            });
        }

        // Đánh dấu License đã hủy (DISPOSAL_LICENSE)
        private async Task ApplyDisposalLicense(List<RequestDetail> details)
        {
            var licenseIdStr = details.FirstOrDefault(d => d.FieldName == "license_id")?.NewValue;
            if (!int.TryParse(licenseIdStr, out int licenseId))
                throw new InvalidOperationException("license_id missing in DISPOSAL_LICENSE request.");

            var license = await _context.Licenses.FindAsync(licenseId)
                ?? throw new InvalidOperationException($"License {licenseId} not found.");

            bool isLinked = await _context.Softwares.AnyAsync(s => s.LicenseId == licenseId);
            if (isLinked)
                throw new InvalidOperationException("Cannot dispose: license is still linked to software or devices.");

            var disposalDateStr = details.FirstOrDefault(d => d.FieldName == "disposal_date")?.NewValue;
            license.LicenseStatus = "Disposed";
            license.DisposalDate  = disposalDateStr != null ? DateTime.Parse(disposalDateStr) : DateTime.UtcNow;
            license.UpdatedAt     = DateTime.UtcNow;
        }

        // ===============================
        // Helper: sinh License Management Number
        // ===============================
        private static string GenerateLicenseManagementNumber(string? lastNumber)
        {
            if (string.IsNullOrEmpty(lastNumber))
                return "LIC-00001";

            var numPart = lastNumber.Replace("LIC-", "").TrimStart('0');
            if (int.TryParse(numPart, out int current))
                return $"LIC-{(current + 1):D5}";

            return "LIC-00001";
        }

        // ===============================
        // Helper: JSON deserialize helpers (dùng cho request_data)
        // ===============================
        private static string? GetString(Dictionary<string, System.Text.Json.JsonElement>? d, string key)
            => d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString() : null;

        private static int GetInt(Dictionary<string, System.Text.Json.JsonElement>? d, string key)
            => d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
                ? v.GetInt32() : 0;

        private static bool GetBool(Dictionary<string, System.Text.Json.JsonElement>? d, string key)
            => d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.True;

        private static int? GetNullableInt(Dictionary<string, System.Text.Json.JsonElement>? d, string key)
            => d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
                ? v.GetInt32() : null;

        private static DateTime? GetNullableDate(Dictionary<string, System.Text.Json.JsonElement>? d, string key)
        {
            if (d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String)
                if (DateTime.TryParse(v.GetString(), out var date))
                    return date;
            return null;
        }
        private void ApplyToAsset(ITAsset asset, List<RequestDetail> details)
        {
            if (asset == null) return;

            foreach (var d in details)
            {
                var field = d.FieldName?.ToLower();
                var value = d.NewValue?.Trim();

                if (string.IsNullOrEmpty(field)) continue;

                switch (field)
                {
                    case "asset_name":
                        asset.AssetName = value;
                        break;

                    case "manufacturer":
                        asset.Manufacturer = value;
                        break;

                    case "model":
                        asset.Model = value;
                        break;

                    case "status_id":
                        if (!string.IsNullOrEmpty(value))
                            asset.StatusId = int.Parse(value);
                        break;

                    case "department_id":
                        if (!string.IsNullOrEmpty(value))
                            asset.DepartmentId = int.Parse(value);
                        break;

                    case "location_id":
                        if (!string.IsNullOrEmpty(value))
                            asset.LocationId = int.Parse(value);
                        break;

                    case "user_used_id":
                        asset.UserUsedId = string.IsNullOrEmpty(value)
                            ? null
                            : int.Parse(value);
                        break;
                }
            }
        }

        // ===============================
        // 5. GET ALL REQUEST
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var list = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.Status)
                .Select(r => new
                {
                    r.RequestId,
                    Type = r.RequestType.TypeName,
                    Status = r.Status.StatusName,
                    r.RequestDescription,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // ===============================
        // 6. GET REQUEST DETAIL
        // ===============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestDetail(int id)
        {
            var request = await _context.Requests
                .Include(r => r.RequestType)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
                return NotFound();

            var details = await _context.RequestDetails
                .Where(d => d.RequestId == id)
                .ToListAsync();

            return Ok(new
            {
                request.RequestId,
                Type = request.RequestType.TypeName,
                Status = request.Status.StatusName,
                request.RequestDescription,
                Details = details
            });
        }

        // =====================================
        // 7. CREATE DISPOSAL / RETURN REQUEST
        // =====================================
        [HttpPost("disposal")]
        public async Task<IActionResult> CreateDisposalRequest([FromBody] DisposalRequestDto dto)
        {
            var asset = await _context.ITAssets.FindAsync(dto.AssetId);
            if (asset == null)
                return NotFound("Asset not found");

            // phân biệt type
            int requestTypeId = dto.Type.ToLower() == "return" ? 4 : 3;
            // 3 = disposal, 4 = return (bạn define trong DB)

            var request = new Request
            {
                RequestTypeId = requestTypeId,
                UserCreatedId = dto.UserCreatedId,
                AssetId = dto.AssetId,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // lưu trạng thái asset cũ
            _context.RequestDetails.Add(new RequestDetail
            {
                RequestId = request.RequestId,
                FieldName = "asset_status",
                OldValue = asset.StatusId.ToString(),
                NewValue = dto.Type.ToLower() == "return" ? "available" : "disposed"
            });

            // approval
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Disposal/Return request created",
                requestId = request.RequestId
            });
        }

        // ===============================
        // FAILURE REQUEST
        // ===============================

        [HttpPost("failure")]
        public async Task<IActionResult> CreateFailureRequest([FromBody] FailureRequestDto dto)
        {
            var asset = await _context.ITAssets.FindAsync(dto.AssetId);
            if (asset == null)
                return NotFound("Asset not found");

            var request = new Request
            {
                RequestTypeId = 6, // FAILURE
                UserCreatedId = dto.UserCreatedId,
                AssetId = dto.AssetId,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // 🔹 save details
            _context.RequestDetails.AddRange(
                new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "application_classification",
                    NewValue = dto.ApplicationClassification
                },
                new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "pickup_date",
                    NewValue = dto.PickupDate.ToString("yyyy-MM-dd")
                },
                new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "receipt_date",
                    NewValue = dto.ReceiptDate?.ToString("yyyy-MM-dd")
                },
                new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "breakdown_reason",
                    NewValue = dto.BreakdownReason
                },
                new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "status_id",
                    OldValue = asset.StatusId.ToString(),
                    NewValue = "3" 
                }
            );

            // approval
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Failure request created",
                requestId = request.RequestId
            });
        }

        // ===============================
        // APPLICATION HISTORY
        // ===============================

        [HttpGet("history/{assetId}")]
        public async Task<IActionResult> GetApplicationHistory(int assetId)
        {
            // 🔹 lấy asset
            var asset = await _context.ITAssets
                .Include(a => a.Department)
                .Include(a => a.UserUsed)
                .FirstOrDefaultAsync(a => a.AssetId == assetId);

            if (asset == null)
                return NotFound("Asset not found");

            // 🔹 CASE 1: request trực tiếp
            var directRequests = await _context.Requests
                .Where(r => r.AssetId == assetId)
                .Include(r => r.RequestType)
                .Include(r => r.UserCreated)
                .ToListAsync();

            // 🔹 CASE 2: request bulk
            var bulkRequestIds = await _context.Set<RequestAsset>()
                .Where(ra => ra.AssetId == assetId)
                .Select(ra => ra.RequestId)
                .ToListAsync();

            var bulkRequests = await _context.Requests
                .Where(r => bulkRequestIds.Contains(r.RequestId))
                .Include(r => r.RequestType)
                .Include(r => r.UserCreated)
                .ToListAsync();

            // 🔹 merge
            var allRequests = directRequests
                .Concat(bulkRequests)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var result = allRequests.Select(r => new
            {
                ApplicationId = r.RequestId,
                Applicant = r.UserCreated?.Username,
                ApplicationDate = r.CreatedAt,
                ControlNumber = asset.AssetControlNumber,
                ApplicationType = r.RequestType?.TypeName,
                Department = asset.Department?.DepartmentName,
                User = asset.UserUsed?.Username
            });

            return Ok(result);
        }

        // ===============================
        // INVENTORY HISTORY
        // ===============================

        [HttpGet("inventory-history/{assetId}")]
        public async Task<IActionResult> GetInventoryHistory(int assetId)
        {
            var asset = await _context.ITAssets
                .FirstOrDefaultAsync(a => a.AssetId == assetId);

            if (asset == null)
                return NotFound("Asset not found");

            // 🔹 lấy tất cả inventory của department asset
            var histories = await _context.InventoryHistories
                .Where(i => i.InventoryDepartmentId == asset.DepartmentId)
                .Include(i => i.InventoryDepartment)
                .Include(i => i.InventoryImplementerNavigation)
                .OrderByDescending(i => i.InventoryDate)
                .ToListAsync();

            var result = histories.Select(i => new
            {
                ControlNumber = asset.AssetControlNumber,
                Manufacturer = asset.Manufacturer,
                InventoryDate = i.InventoryDate,
                Department = i.InventoryDepartment?.DepartmentName,
                Implementer = i.InventoryImplementerNavigation?.Username,
                Result = i.InventoryStatus
            });

            return Ok(result);
        }
        // ===============================
        // BULK ACTION MENU
        // ===============================

        [HttpPost("bulk/change")]
        public async Task<IActionResult> BulkChange([FromBody] BulkChangeRequestDto dto)
        {
            var assets = await _context.ITAssets
                .Where(a => dto.AssetIds.Contains(a.AssetId))
                .ToListAsync();

            if (!assets.Any())
                return BadRequest("No valid assets found");

            var request = new Request
            {
                RequestTypeId = 1, 
                UserCreatedId = 1,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // map assets
            foreach (var assetId in dto.AssetIds)
            {
                _context.Add(new RequestAsset
                {
                    RequestId = request.RequestId,
                    AssetId = assetId
                });
            }

            // details
            foreach (var asset in assets)
            {
                _context.RequestDetails.Add(new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = dto.FieldName,
                    OldValue = "N/A",
                    NewValue = dto.NewValue
                });
            }
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok("Bulk change request created");
        }

        [HttpPost("bulk/move")]
        public async Task<IActionResult> BulkMove([FromBody] BulkMoveRequestDto dto)
        {
            var request = new Request
            {
                RequestTypeId = 2,
                UserCreatedId = 1,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            foreach (var assetId in dto.AssetIds)
            {
                _context.Add(new RequestAsset
                {
                    RequestId = request.RequestId,
                    AssetId = assetId
                });

                _context.RequestDetails.AddRange(
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "department_id",
                        OldValue = null,
                        NewValue = dto.NewDepartmentId.ToString()
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "location_id",
                        OldValue = null,
                        NewValue = dto.NewLocationId.ToString()
                    }
                );
            }

            // CHỈ 1 APPROVAL
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok("Bulk move request created");
        }

        [HttpPost("bulk/disposal")]
        public async Task<IActionResult> BulkDisposal([FromBody] BulkDisposalRequestDto dto)
        {
            int requestType = dto.IsDisposal ? 3 : 4;

            var request = new Request
            {
                RequestTypeId = requestType,
                UserCreatedId = 1,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            foreach (var assetId in dto.AssetIds)
            {
                _context.Add(new RequestAsset
                {
                    RequestId = request.RequestId,
                    AssetId = assetId
                });

                _context.RequestDetails.Add(new RequestDetail
                {
                    RequestId = request.RequestId,
                    FieldName = "status_id",
                    OldValue = null,
                    NewValue = dto.IsDisposal ? "3" : "1" 
                });
            }

            // CHỈ 1 APPROVAL
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok("Bulk disposal/return request created");
        }


        [HttpPost("bulk/failure")]
        public async Task<IActionResult> BulkFailure([FromBody] BulkFailureRequestDto dto)
        {
            var assets = await _context.ITAssets
                .Where(a => dto.AssetIds.Contains(a.AssetId))
                .ToListAsync();

            if (!assets.Any())
                return BadRequest("No assets found");

            var request = new Request
            {
                RequestTypeId = 6,
                UserCreatedId = dto.UserCreatedId,
                TargetDiv = 1,
                StatusId = 1,
                RequestDescription = dto.Description
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            foreach (var asset in assets)
            {
                // map asset
                _context.Add(new RequestAsset
                {
                    RequestId = request.RequestId,
                    AssetId = asset.AssetId
                });

                // details
                _context.RequestDetails.AddRange(
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "application_classification",
                        NewValue = dto.ApplicationClassification
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "pickup_date",
                        NewValue = dto.PickupDate.ToString("yyyy-MM-dd")
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "receipt_date",
                        NewValue = dto.ReceiptDate?.ToString("yyyy-MM-dd")
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "breakdown_reason",
                        NewValue = dto.BreakdownReason
                    },
                    new RequestDetail
                    {
                        RequestId = request.RequestId,
                        FieldName = "status_id",
                        OldValue = asset.StatusId.ToString(),
                        NewValue = "3"
                    }
                );
            }

            // approval
            _context.RequestApprovals.Add(new RequestApproval
            {
                RequestId = request.RequestId,
                ApproverId = dto.ApproverId,
                ApprovalLevel = 1,
                StatusId = 1
            });

            await _context.SaveChangesAsync();

            return Ok("Bulk failure request created");
        }
    }
}