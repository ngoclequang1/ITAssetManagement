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

            // 2 level approval
            _context.RequestApprovals.AddRange(
                new RequestApproval
                {
                    RequestId = request.RequestId,
                    ApproverId = dto.FirstApproverId,
                    ApprovalLevel = 1,
                    StatusId = 1
                },
                new RequestApproval
                {
                    RequestId = request.RequestId,
                    ApproverId = dto.SecondApproverId,
                    ApprovalLevel = 2,
                    StatusId = 1
                }
            );

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
        // 4. APPLY REQUEST
        // ===============================
        private async Task ApplyRequest(int requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);

            var details = await _context.RequestDetails
                .Where(d => d.RequestId == requestId)
                .ToListAsync();

            // 🔹 CASE 1: SINGLE
            if (request.AssetId != null)
            {
                var asset = await _context.ITAssets.FindAsync(request.AssetId);
                ApplyToAsset(asset, details);
            }
            else
            {
                // 🔹 CASE 2: BULK
                var requestAssets = await _context.Set<RequestAsset>()
                    .Where(x => x.RequestId == requestId)
                    .ToListAsync();

                foreach (var ra in requestAssets)
                {
                    var asset = await _context.ITAssets.FindAsync(ra.AssetId);
                    ApplyToAsset(asset, details);
                }
            }

            request.StatusId = 2;

            _context.RequestHistories.Add(new RequestHistory
            {
                RequestId = requestId,
                StatusId = 2,
                UserCreatedId = request.UserCreatedId,
                Remarks = "Applied"
            });

            await _context.SaveChangesAsync();
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