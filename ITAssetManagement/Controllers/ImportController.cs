using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Data;
using ITAssetManagement.Models;
using ITAssetManagement.DTOs.Import;
using ClosedXML.Excel;

namespace ITAssetManagement.Controllers
{
    [Route("api/import")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Allowed file extensions and max size (5 MB)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".xlsx", ".xls", ".csv" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        // Valid enum values per field
        private static readonly HashSet<string> ValidSoftwareTypes = new(StringComparer.OrdinalIgnoreCase)
            { "Free", "Paid", "Open Source", "Trial" }; 
        private static readonly HashSet<string> ValidLicenseTypes = new(StringComparer.OrdinalIgnoreCase)
            { "Subscription", "Perpetual", "Academic", "Trial", "Freeware" };

        private static readonly HashSet<string> ValidLicenseFormats = new(StringComparer.OrdinalIgnoreCase)
            { "Digital", "DVD", "USB Key" };

        private static readonly HashSet<string> ValidCountingMethods = new(StringComparer.OrdinalIgnoreCase)
            { "User", "Device", "Core", "Processor" };

        private static readonly HashSet<string> ValidSwLicenseTypes = new(StringComparer.OrdinalIgnoreCase)
            { "PAID", "FREEWARE", "TRIAL", "OPEN_SOURCE" };

        public ImportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // DOWNLOAD TEMPLATES
        // ====================================================================

        /// <summary>Download hardware import template</summary>
        [HttpGet("template/hardware")]
        public IActionResult DownloadHardwareTemplate()
        {
            var bytes = BuildHardwareTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Hardware_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>Download software import template</summary>
        [HttpGet("template/software")]
        public IActionResult DownloadSoftwareTemplate()
        {
            var bytes = BuildSoftwareTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Software_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>Download license import template</summary>
        [HttpGet("template/license")]
        public IActionResult DownloadLicenseTemplate()
        {
            var bytes = BuildLicenseTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"License_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ====================================================================
        // IMPORT HARDWARE
        // ====================================================================

        /// <summary>
        /// Import hardware assets from Excel/CSV.
        /// Validates ALL rows first; if any row has errors, returns an error file.
        /// If all rows pass, inserts everything in one transaction.
        /// </summary>
        [HttpPost("hardware")]
        public async Task<IActionResult> ImportHardware(IFormFile file)
        {
            var fileValidation = ValidateFile(file);
            if (fileValidation != null) return fileValidation;

            // Parse rows
            List<HardwareImportRowDto> rows;
            try
            {
                rows = ParseHardwareFile(file);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to parse file: " + ex.Message });
            }

            if (!rows.Any())
                return BadRequest(new { message = "The file contains no data rows." });

            // Business validation (DB checks, duplicates)
            await ValidateHardwareRowsAsync(rows);

            // If any errors → return error file (rollback — nothing was inserted)
            if (rows.Any(r => r.HasErrors))
            {
                var errorBytes = BuildHardwareErrorFile(rows);
                return File(errorBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Hardware_Import_Errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            // All valid → batch insert in one transaction
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int successCount = 0;
                foreach (var row in rows)
                {
                    _context.ITAssets.Add(new ITAsset
                    {
                        AssetControlNumber = row.AssetControlNumber!.Trim(),
                        AssetName          = row.AssetName!.Trim(),
                        SerialNumber       = row.SerialNumber!.Trim(),
                        Manufacturer       = row.Manufacturer?.Trim(),
                        Model              = row.Model?.Trim(),
                        CategoryId         = row.CategoryId!.Value,
                        StatusId           = row.StatusId!.Value,
                        LocationId         = row.LocationId!.Value,
                        DepartmentId       = row.DepartmentId!.Value,
                        PurchaseDate       = row.PurchaseDate,
                        WarrantyExpiry     = row.WarrantyExpiry,
                        CreatedAt          = DateTime.UtcNow,
                        UserCreatedId      = 1  // replace with current user from auth token
                    });
                    successCount++;
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success      = true,
                    totalRows    = rows.Count,
                    successCount,
                    errorCount   = 0,
                    message      = $"Import completed successfully. {successCount} hardware assets imported."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Import failed during save: " + ex.Message });
            }
        }

        // ====================================================================
        // IMPORT SOFTWARE
        // ====================================================================

        [HttpPost("software")]
        public async Task<IActionResult> ImportSoftware(IFormFile file)
        {
            var fileValidation = ValidateFile(file);
            if (fileValidation != null) return fileValidation;

            List<SoftwareImportRowDto> rows;
            try
            {
                rows = ParseSoftwareFile(file);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to parse file: " + ex.Message });
            }

            if (!rows.Any())
                return BadRequest(new { message = "The file contains no data rows." });

            await ValidateSoftwareRowsAsync(rows);

            if (rows.Any(r => r.HasErrors))
            {
                var errorBytes = BuildSoftwareErrorFile(rows);
                return File(errorBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Software_Import_Errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int successCount = 0;
                foreach (var row in rows)
                {
                    int? assetId = null;
                    if (!string.IsNullOrWhiteSpace(row.AssetControlNumber))
                    {
                        var asset = await _context.ITAssets
                            .FirstOrDefaultAsync(a => a.AssetControlNumber == row.AssetControlNumber.Trim());
                        assetId = asset?.AssetId;
                    }

                    _context.Softwares.Add(new Software
                    {
                        SoftwareName    = row.SoftwareName!.Trim(),
                        SoftwareVersion = row.SoftwareVersion!.Trim(),
                        SoftwareType    = row.SoftwareType?.Trim(), // Đã map dữ liệu trở lại trường này
                        LicenseType     = row.LicenseType?.Trim(),
                        VendorId        = row.VendorId,
                        LicenseId       = row.LicenseId,
                        AssetId         = assetId,
                        AssetControlNumber = row.AssetControlNumber?.Trim(),
                        InstalledBy     = row.InstalledBy,
                        InstalledDate   = row.InstallDate ?? DateTime.UtcNow,
                        Description     = row.Description?.Trim()
                    });
                    successCount++;
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success      = true,
                    totalRows    = rows.Count,
                    successCount,
                    errorCount   = 0,
                    message      = $"Import completed successfully. {successCount} software records imported."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "Import failed during save: " + innerMsg });
            }
        }

        // ====================================================================
        // IMPORT LICENSE
        // ====================================================================

        [HttpPost("license")]
        public async Task<IActionResult> ImportLicense(IFormFile file)
        {
            var fileValidation = ValidateFile(file);
            if (fileValidation != null) return fileValidation;

            List<LicenseImportRowDto> rows;
            try
            {
                rows = ParseLicenseFile(file);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to parse file: " + ex.Message });
            }

            if (!rows.Any())
                return BadRequest(new { message = "The file contains no data rows." });

            await ValidateLicenseRowsAsync(rows);

            if (rows.Any(r => r.HasErrors))
            {
                var errorBytes = BuildLicenseErrorFile(rows);
                return File(errorBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"License_Import_Errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            // Determine starting license number once for the whole batch
            var lastNumber = await _context.Licenses
                .Where(l => l.LicenseManagementNumber != null)
                .OrderByDescending(l => l.LicenseId)
                .Select(l => l.LicenseManagementNumber)
                .FirstOrDefaultAsync();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int successCount = 0;
                foreach (var row in rows)
                {
                    lastNumber = GenerateLicenseManagementNumber(lastNumber);

                    _context.Licenses.Add(new License
                    {
                        LicenseManagementNumber = lastNumber,
                        InstallationName        = row.InstallationName!.Trim(),
                        PublisherName           = row.PublisherName?.Trim(),
                        SoftwareType            = row.SoftwareType!.Trim(),
                        LicenseType             = row.LicenseType!.Trim(),
                        LicenseFormat           = row.LicenseFormat!.Trim(),
                        CountingMethod          = row.CountingMethod!.Trim(),
                        NumberOfLicenses        = row.NumberOfLicenses!.Value,
                        NumberAvailable         = row.NumberOfLicenses!.Value,
                        LicenseKey              = row.LicenseKey?.Trim(),
                        ExpiryDate              = row.ExpiryDate,
                        AcademicFlag            = row.AcademicFlag,
                        ManagementDepartmentId  = row.ManagementDepartmentId,
                        Description             = row.Description?.Trim(),
                        LicenseStatus           = "Active",
                        IsDeleted               = false,
                        CreatedAt               = DateTime.UtcNow,
                        CreatedBy               = "import",
                        UpdatedAt               = DateTime.UtcNow,
                        UpdatedBy               = "import"
                    });
                    successCount++;

                    // Flush periodically so GenerateLicenseManagementNumber sees the latest record
                    await _context.SaveChangesAsync();
                }
                await tx.CommitAsync();

                return Ok(new
                {
                    success      = true,
                    totalRows    = rows.Count,
                    successCount,
                    errorCount   = 0,
                    message      = $"Import completed successfully. {successCount} licenses imported."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Import failed during save: " + ex.Message });
            }
        }

        // ====================================================================
        // FILE VALIDATION
        // ====================================================================

        private IActionResult? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = "Invalid file format. Only .xlsx, .xls, and .csv files are accepted." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { message = "File size exceeds the 5 MB limit." });

            return null;
        }

        // ====================================================================
        // PARSERS
        // ====================================================================

        private List<HardwareImportRowDto> ParseHardwareFile(IFormFile file)
        {
            var rows = new List<HardwareImportRowDto>();
            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                // Skip completely empty rows
                if (IsRowEmpty(row, 12)) continue;

                rows.Add(new HardwareImportRowDto
                {
                    RowNumber          = r,
                    AssetControlNumber = GetCell(row, 1),
                    AssetName          = GetCell(row, 2),
                    SerialNumber       = GetCell(row, 3),
                    Manufacturer       = GetCell(row, 4),
                    Model              = GetCell(row, 5),
                    CategoryId         = GetIntCell(row, 6),
                    StatusId           = GetIntCell(row, 7),
                    LocationId         = GetIntCell(row, 8),
                    DepartmentId       = GetIntCell(row, 9),
                    PurchaseDate       = GetDateCell(row, 10),
                    WarrantyExpiry     = GetDateCell(row, 11),
                    Notes              = GetCell(row, 12)
                });
            }
            return rows;
        }

        private List<SoftwareImportRowDto> ParseSoftwareFile(IFormFile file)
        {
            var rows = new List<SoftwareImportRowDto>();
            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (IsRowEmpty(row, 10)) continue; // Đọc tổng cộng 10 cột dữ liệu

                rows.Add(new SoftwareImportRowDto
                {
                    RowNumber          = r,
                    AssetControlNumber = GetCell(row, 1),
                    SoftwareName       = GetCell(row, 2),
                    SoftwareVersion    = GetCell(row, 3),
                    SoftwareType       = GetCell(row, 4), // Cột số 4 là Software Type
                    VendorId           = GetIntCell(row, 5),
                    LicenseId          = GetIntCell(row, 6),
                    LicenseType        = GetCell(row, 7),
                    Description        = GetCell(row, 8),
                    InstalledBy        = GetIntCell(row, 9),
                    InstallDate        = GetDateCell(row, 10)
                });
            }
            return rows;
        }

        private List<LicenseImportRowDto> ParseLicenseFile(IFormFile file)
        {
            var rows = new List<LicenseImportRowDto>();
            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (IsRowEmpty(row, 12)) continue;

                var academicRaw = GetCell(row, 10)?.Trim().ToLower();
                bool academicFlag = academicRaw == "true" || academicRaw == "yes" || academicRaw == "1";

                rows.Add(new LicenseImportRowDto
                {
                    RowNumber              = r,
                    InstallationName       = GetCell(row, 1),
                    PublisherName          = GetCell(row, 2),
                    SoftwareType           = GetCell(row, 3),
                    LicenseType            = GetCell(row, 4),
                    LicenseFormat          = GetCell(row, 5),
                    CountingMethod         = GetCell(row, 6),
                    NumberOfLicenses       = GetIntCell(row, 7),
                    LicenseKey             = GetCell(row, 8),
                    ExpiryDate             = GetDateCell(row, 9),
                    AcademicFlag           = academicFlag,
                    ManagementDepartmentId = GetIntCell(row, 11),
                    Description            = GetCell(row, 12)
                });
            }
            return rows;
        }

        // ====================================================================
        // VALIDATORS
        // ====================================================================

        private async Task ValidateHardwareRowsAsync(List<HardwareImportRowDto> rows)
        {
            // Pre-load existing control numbers and serial numbers for duplicate check
            var existingControlNos = (await _context.ITAssets
                .Select(a => a.AssetControlNumber)
                .ToListAsync())
                .Where(x => x != null).Select(x => x!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingSerials = (await _context.ITAssets
                .Where(a => a.SerialNumber != null)
                .Select(a => a.SerialNumber!)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var validDeptIds = (await _context.Departments
                .Where(d => !d.IsDeleted)
                .Select(d => d.DepartmentId)
                .ToListAsync())
                .ToHashSet();

            // Track duplicates within the file itself
            var seenControlNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenSerials    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                // Required field checks
                if (string.IsNullOrWhiteSpace(row.AssetControlNumber))
                    row.Errors.Add("Asset Control Number is required.");
                else if (existingControlNos.Contains(row.AssetControlNumber.Trim()))
                    row.Errors.Add($"Asset Control Number '{row.AssetControlNumber}' already exists in the system.");
                else if (!seenControlNos.Add(row.AssetControlNumber.Trim()))
                    row.Errors.Add($"Asset Control Number '{row.AssetControlNumber}' is duplicated within the file.");

                if (string.IsNullOrWhiteSpace(row.AssetName))
                    row.Errors.Add("Asset Name is required.");
                else if (row.AssetName.Length > 200)
                    row.Errors.Add("Asset Name must not exceed 200 characters.");

                if (string.IsNullOrWhiteSpace(row.SerialNumber))
                    row.Errors.Add("Serial Number is required.");
                else if (existingSerials.Contains(row.SerialNumber.Trim()))
                    row.Errors.Add($"Serial Number '{row.SerialNumber}' already exists in the system.");
                else if (!seenSerials.Add(row.SerialNumber.Trim()))
                    row.Errors.Add($"Serial Number '{row.SerialNumber}' is duplicated within the file.");

                if (row.CategoryId == null || row.CategoryId <= 0)
                    row.Errors.Add("Category ID is required and must be a positive integer.");

                if (row.StatusId == null || row.StatusId <= 0)
                    row.Errors.Add("Status ID is required and must be a positive integer.");

                if (row.LocationId == null || row.LocationId <= 0)
                    row.Errors.Add("Location ID is required and must be a positive integer.");

                if (row.DepartmentId == null || row.DepartmentId <= 0)
                    row.Errors.Add("Department ID is required and must be a positive integer.");
                else if (!validDeptIds.Contains(row.DepartmentId.Value))
                    row.Errors.Add($"Department ID {row.DepartmentId} does not exist in the system.");

                // Optional field length checks
                if (!string.IsNullOrWhiteSpace(row.Manufacturer) && row.Manufacturer.Length > 100)
                    row.Errors.Add("Manufacturer must not exceed 100 characters.");

                if (!string.IsNullOrWhiteSpace(row.Model) && row.Model.Length > 100)
                    row.Errors.Add("Model must not exceed 100 characters.");
            }
        }

        private async Task ValidateSoftwareRowsAsync(List<SoftwareImportRowDto> rows)
        {
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validAssetControlNos = (await _context.ITAssets
                .Select(a => a.AssetControlNumber)
                .ToListAsync())
                .Where(x => x != null).Select(x => x!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                
            var validVendorIds = (await _context.Softwares
                .Where(s => s.VendorId != null)
                .Select(s => s.VendorId!.Value)
                .Distinct()
                .ToListAsync())
                .ToHashSet();
                
            var validLicenseIds = (await _context.Licenses
                .Where(l => !l.IsDeleted)
                .Select(l => l.LicenseId)
                .ToListAsync())
                .ToHashSet();

            foreach (var row in rows)
            {
                // 1. Kiểm tra Asset Control Number (Bắt buộc)
                if (string.IsNullOrWhiteSpace(row.AssetControlNumber))
                    row.Errors.Add("Asset Control Number is required.");
                else if (!validAssetControlNos.Contains(row.AssetControlNumber.Trim()))
                    row.Errors.Add($"Asset Control Number '{row.AssetControlNumber}' was not found in the system.");

                // 2. Kiểm tra Software Name & Version (Bắt buộc)
                if (string.IsNullOrWhiteSpace(row.SoftwareName))
                    row.Errors.Add("Software Name is required.");
                else if (row.SoftwareName.Length > 200)
                    row.Errors.Add("Software Name must not exceed 200 characters.");

                if (string.IsNullOrWhiteSpace(row.SoftwareVersion))
                    row.Errors.Add("Software Version is required.");
                else if (row.SoftwareVersion.Length > 50)
                    row.Errors.Add("Software Version must not exceed 50 characters.");

                // 3. Kiểm tra danh sách hợp lệ của Software Type
                if (!string.IsNullOrWhiteSpace(row.SoftwareType) && !ValidSoftwareTypes.Contains(row.SoftwareType.Trim()))
                    row.Errors.Add($"Software Type '{row.SoftwareType}' is invalid. Valid values: {string.Join(", ", ValidSoftwareTypes)}.");

                // 4. Kiểm tra Enum License Type & Khóa ngoại ID
                if (!string.IsNullOrWhiteSpace(row.LicenseType) && !ValidSwLicenseTypes.Contains(row.LicenseType.Trim()))
                    row.Errors.Add($"License Type '{row.LicenseType}' is invalid. Valid values: PAID, FREEWARE, TRIAL, OPEN_SOURCE.");

                if (row.VendorId.HasValue && !validVendorIds.Contains(row.VendorId.Value))
                    row.Errors.Add($"Vendor ID {row.VendorId} does not exist in the system.");

                if (row.LicenseId.HasValue && !validLicenseIds.Contains(row.LicenseId.Value))
                    row.Errors.Add($"License ID {row.LicenseId} does not exist in the system.");

                // 5. Kiểm tra trùng lặp bản ghi
                if (!string.IsNullOrWhiteSpace(row.AssetControlNumber) &&
                    !string.IsNullOrWhiteSpace(row.SoftwareName) && 
                    !string.IsNullOrWhiteSpace(row.SoftwareVersion))
                {
                    var key = $"{row.SoftwareName.Trim()}|{row.SoftwareVersion.Trim()}|{row.AssetControlNumber.Trim()}";
                    if (!seenKeys.Add(key))
                        row.Errors.Add($"Duplicate entry: '{row.SoftwareName} {row.SoftwareVersion}' on asset '{row.AssetControlNumber}' appears more than once in the file.");

                    var asset = await _context.ITAssets.FirstOrDefaultAsync(a => a.AssetControlNumber == row.AssetControlNumber.Trim());
                    if (asset != null)
                    {
                        bool exists = await _context.Softwares.AnyAsync(s =>
                            s.SoftwareName == row.SoftwareName.Trim() &&
                            s.SoftwareVersion == row.SoftwareVersion.Trim() &&
                            s.AssetId == asset.AssetId);
                        if (exists)
                            row.Errors.Add($"'{row.SoftwareName} {row.SoftwareVersion}' is already installed on asset '{row.AssetControlNumber}'.");
                    }
                }
            }
        }
        private async Task ValidateLicenseRowsAsync(List<LicenseImportRowDto> rows)
        {
            var validDeptIds = (await _context.Departments
                .Where(d => !d.IsDeleted)
                .Select(d => d.DepartmentId)
                .ToListAsync())
                .ToHashSet();
            var seenInstallNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.InstallationName))
                    row.Errors.Add("Installation Name is required.");
                else if (row.InstallationName.Length > 200)
                    row.Errors.Add("Installation Name must not exceed 200 characters.");

                // 1. Kiểm tra Software Type (Bắt buộc & Giới hạn trong danh sách chuẩn để chống lỗi Truncated)
                if (string.IsNullOrWhiteSpace(row.SoftwareType))
                    row.Errors.Add("Software Type is required.");
                else if (!ValidSoftwareTypes.Contains(row.SoftwareType.Trim()))
                    row.Errors.Add($"Software Type '{row.SoftwareType}' is invalid. Valid values: {string.Join(", ", ValidSoftwareTypes)}.");

                if (string.IsNullOrWhiteSpace(row.LicenseType))
                    row.Errors.Add("License Type is required.");
                else if (!ValidLicenseTypes.Contains(row.LicenseType.Trim()))
                    row.Errors.Add($"License Type '{row.LicenseType}' is invalid. Valid values: Subscription, Perpetual, Academic, Trial, Freeware.");

                if (string.IsNullOrWhiteSpace(row.LicenseFormat))
                    row.Errors.Add("License Format is required.");
                else if (!ValidLicenseFormats.Contains(row.LicenseFormat.Trim()))
                    row.Errors.Add($"License Format '{row.LicenseFormat}' is invalid. Valid values: Digital, DVD, USB Key.");

                if (string.IsNullOrWhiteSpace(row.CountingMethod))
                    row.Errors.Add("Counting Method is required.");
                else if (!ValidCountingMethods.Contains(row.CountingMethod.Trim()))
                    row.Errors.Add($"Counting Method '{row.CountingMethod}' is invalid. Valid values: User, Device, Core, Processor.");

                if (row.NumberOfLicenses == null || row.NumberOfLicenses <= 0)
                    row.Errors.Add("Number of Licenses is required and must be greater than 0.");

                if (row.ManagementDepartmentId.HasValue && !validDeptIds.Contains(row.ManagementDepartmentId.Value))
                    row.Errors.Add($"Management Department ID {row.ManagementDepartmentId} does not exist in the system.");

                if (row.ExpiryDate.HasValue && row.ExpiryDate.Value < DateTime.UtcNow.Date)
                    row.Errors.Add($"Expiry Date {row.ExpiryDate.Value:yyyy-MM-dd} is in the past. Please verify.");
            }
        }

        // ====================================================================
        // ERROR FILE BUILDERS
        // ====================================================================

        private byte[] BuildHardwareErrorFile(List<HardwareImportRowDto> rows)
        {
            var headers = new[]
            {
                "Row#", "Asset Control Number*", "Asset Name*", "Serial Number*",
                "Manufacturer", "Model", "Category ID*", "Status ID*", "Location ID*",
                "Department ID*", "Purchase Date", "Warranty Expiry", "Notes", "Import Error"
            };

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Hardware Import Errors");
            ApplyErrorSheetHeader(ws, headers);

            int excelRow = 2;
            foreach (var row in rows)
            {
                ws.Cell(excelRow, 1).Value  = row.RowNumber;
                ws.Cell(excelRow, 2).Value  = row.AssetControlNumber ?? "";
                ws.Cell(excelRow, 3).Value  = row.AssetName ?? "";
                ws.Cell(excelRow, 4).Value  = row.SerialNumber ?? "";
                ws.Cell(excelRow, 5).Value  = row.Manufacturer ?? "";
                ws.Cell(excelRow, 6).Value  = row.Model ?? "";
                ws.Cell(excelRow, 7).Value  = row.CategoryId?.ToString() ?? "";
                ws.Cell(excelRow, 8).Value  = row.StatusId?.ToString() ?? "";
                ws.Cell(excelRow, 9).Value  = row.LocationId?.ToString() ?? "";
                ws.Cell(excelRow, 10).Value = row.DepartmentId?.ToString() ?? "";
                ws.Cell(excelRow, 11).Value = row.PurchaseDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(excelRow, 12).Value = row.WarrantyExpiry?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(excelRow, 13).Value = row.Notes ?? "";
                ws.Cell(excelRow, 14).Value = string.Join(" | ", row.Errors);

                if (row.HasErrors)
                    HighlightErrorRow(ws, excelRow, headers.Length);

                excelRow++;
            }

            AutoFitColumns(ws, headers.Length);
            ws.Column(headers.Length).Width = 60;
            ws.Columns().Style.Alignment.WrapText = false;
            ws.Column(headers.Length).Style.Alignment.WrapText = true;

            return ToBytes(wb);
        }

        private byte[] BuildSoftwareErrorFile(List<SoftwareImportRowDto> rows)
        {
            var headers = new[]
            {
                "Row#", "Asset Control Number*", "Software Name*", "Software Version*",
                "Software Type", "Vendor ID", "License ID", "License Type",
                "Description", "Installed By (User ID)", "Install Date", "Import Error"
            };

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Software Import Errors");
            ApplyErrorSheetHeader(ws, headers);

            int excelRow = 2;
            foreach (var row in rows)
            {
                ws.Cell(excelRow, 1).Value  = row.RowNumber;
                ws.Cell(excelRow, 2).Value  = row.AssetControlNumber ?? "";
                ws.Cell(excelRow, 3).Value  = row.SoftwareName ?? "";
                ws.Cell(excelRow, 4).Value  = row.SoftwareVersion ?? "";
                ws.Cell(excelRow, 5).Value  = row.SoftwareType ?? "";
                ws.Cell(excelRow, 6).Value  = row.VendorId?.ToString() ?? "";
                ws.Cell(excelRow, 7).Value  = row.LicenseId?.ToString() ?? "";
                ws.Cell(excelRow, 8).Value  = row.LicenseType ?? "";
                ws.Cell(excelRow, 9).Value  = row.Description ?? "";
                ws.Cell(excelRow, 10).Value = row.InstalledBy?.ToString() ?? "";
                ws.Cell(excelRow, 11).Value = row.InstallDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(excelRow, 12).Value = string.Join(" | ", row.Errors);

                if (row.HasErrors)
                    HighlightErrorRow(ws, excelRow, headers.Length);

                excelRow++;
            }

            AutoFitColumns(ws, headers.Length);
            ws.Column(headers.Length).Width = 60;
            ws.Column(headers.Length).Style.Alignment.WrapText = true;

            return ToBytes(wb);
        }

        private byte[] BuildLicenseErrorFile(List<LicenseImportRowDto> rows)
        {
            var headers = new[]
            {
                "Row#", "Installation Name*", "Publisher Name", "Software Type*",
                "License Type*", "License Format*", "Counting Method*", "Number of Licenses*",
                "License Key", "Expiry Date", "Academic Flag", "Management Dept ID", "Description", "Import Error"
            };

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("License Import Errors");
            ApplyErrorSheetHeader(ws, headers);

            int excelRow = 2;
            foreach (var row in rows)
            {
                ws.Cell(excelRow, 1).Value  = row.RowNumber;
                ws.Cell(excelRow, 2).Value  = row.InstallationName ?? "";
                ws.Cell(excelRow, 3).Value  = row.PublisherName ?? "";
                ws.Cell(excelRow, 4).Value  = row.SoftwareType ?? "";
                ws.Cell(excelRow, 5).Value  = row.LicenseType ?? "";
                ws.Cell(excelRow, 6).Value  = row.LicenseFormat ?? "";
                ws.Cell(excelRow, 7).Value  = row.CountingMethod ?? "";
                ws.Cell(excelRow, 8).Value  = row.NumberOfLicenses?.ToString() ?? "";
                ws.Cell(excelRow, 9).Value  = row.LicenseKey ?? "";
                ws.Cell(excelRow, 10).Value = row.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(excelRow, 11).Value = row.AcademicFlag.ToString().ToLower();
                ws.Cell(excelRow, 12).Value = row.ManagementDepartmentId?.ToString() ?? "";
                ws.Cell(excelRow, 13).Value = row.Description ?? "";
                ws.Cell(excelRow, 14).Value = string.Join(" | ", row.Errors);

                if (row.HasErrors)
                    HighlightErrorRow(ws, excelRow, headers.Length);

                excelRow++;
            }

            AutoFitColumns(ws, headers.Length);
            ws.Column(headers.Length).Width = 60;
            ws.Column(headers.Length).Style.Alignment.WrapText = true;

            return ToBytes(wb);
        }

        // ====================================================================
        // TEMPLATE BUILDERS
        // ====================================================================

        private static byte[] BuildHardwareTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Hardware Import");
            var headers = new[]
            {
                "Asset Control Number*", "Asset Name*", "Serial Number*",
                "Manufacturer", "Model", "Category ID*", "Status ID*", "Location ID*",
                "Department ID*", "Purchase Date", "Warranty Expiry", "Notes"
            };
            var requiredIdx = new[] { 1, 2, 3, 6, 7, 8, 9 };
            ApplyTemplateHeader(ws, headers, requiredIdx);

            // Sample rows
            var samples = new object[][]
            {
                new object[] {"ACN001","Dell Laptop XPS 13","SN001ABC","Dell","XPS 13",1,1,1,1,"2024-01-15","2027-01-15","Sample row"},
                new object[] {"ACN002","HP EliteBook 840","SN002DEF","HP","EliteBook 840",1,2,2,1,"2024-02-10","",""},
            };
            AddSampleRows(ws, samples);

            var ws2 = wb.AddWorksheet("Instructions");
            var inst = new[]
            {
                new[]{"Column","Required","Description","Valid Values / Example"},
                new[]{"Asset Control Number*","YES","Unique control number","ACN001"},
                new[]{"Asset Name*","YES","Name of the asset","Dell Laptop XPS 13"},
                new[]{"Serial Number*","YES","Unique serial number","SN123456"},
                new[]{"Manufacturer","NO","Hardware manufacturer","Dell, HP, Lenovo"},
                new[]{"Model","NO","Model name","XPS 13 9310"},
                new[]{"Category ID*","YES","1=Laptop, 2=Monitor, 3=Printer","1"},
                new[]{"Status ID*","YES","1=In Use, 2=Available, 3=Broken","2"},
                new[]{"Location ID*","YES","Location ID from system","1"},
                new[]{"Department ID*","YES","Department ID from system","1"},
                new[]{"Purchase Date","NO","YYYY-MM-DD format","2024-01-15"},
                new[]{"Warranty Expiry","NO","YYYY-MM-DD format","2027-01-15"},
                new[]{"Notes","NO","Additional notes","Free text"},
            };
            ApplyInstructionSheet(ws2, inst);
            SetColumnWidths(ws, new double[] {22,25,18,18,18,13,13,13,15,15,16,25});
            ws.SheetView.FreezeRows(1);
            return ToBytes(wb);
        }

        private static byte[] BuildSoftwareTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Software Import");
            var headers = new[]
            {
                "Asset Control Number*", "Software Name*", "Software Version*",
                "Software Type", "Vendor ID", "License ID", "License Type",
                "Description", "Installed By (User ID)", "Install Date"
            };
            ApplyTemplateHeader(ws, headers, new[] { 1, 2, 3 });

            var samples = new object[][]
            {
                new object[]{"ACN001", "Microsoft Office 365", "2024", "Trial", 1, 1, "PAID", "Enterprise license", 1, "2024-01-20"},
                new object[]{"ACN002", "VLC Media Player", "3.0.20", "Free", "", "", "FREEWARE", "Free multimedia player", "", "2024-03-15"}
            };
            AddSampleRows(ws, samples);

            var ws2 = wb.AddWorksheet("Instructions");
            var inst = new[]
            {
                new[]{"Column","Required","Description","Valid Values"},
                new[]{"Asset Control Number*","YES","Device code to install software","ACN001"},
                new[]{"Software Name*","YES","Name of the software","Microsoft Office 365"},
                new[]{"Software Version*","YES","Version number","2024, 3.0.20"},
                new[]{"Software Type","NO","Category of software","Free, Paid, Open Source, Trial"},
                new[]{"Vendor ID","NO","Vendor ID from system","1, 2, 3"},
                new[]{"License ID","NO","License ID to link to","1, 2, 3"},
                new[]{"License Type","NO","PAID / FREEWARE / TRIAL / OPEN_SOURCE","PAID"},
                new[]{"Description","NO","Additional notes","Free text"},
                new[]{"Installed By (User ID)","NO","User ID who installed","1"},
                new[]{"Install Date","NO","YYYY-MM-DD format","2024-01-20"}
            };
            ApplyInstructionSheet(ws2, inst);
            SetColumnWidths(ws, new double[] {22, 25, 18, 18, 12, 12, 15, 30, 22, 14});
            ws.SheetView.FreezeRows(1);
            return ToBytes(wb);
        }

        private static byte[] BuildLicenseTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("License Import");
            var headers = new[]
            {
                "Installation Name*", "Publisher Name", "Software Type*", "License Type*",
                "License Format*", "Counting Method*", "Number of Licenses*",
                "License Key", "Expiry Date", "Academic Flag (true/false)",
                "Management Department ID", "Description"
            };
            ApplyTemplateHeader(ws, headers, new[] { 1, 3, 4, 5, 6, 7 });
            var samples = new object[][]
            {
                new object[]{"Microsoft 365 E5","Microsoft","Paid","Subscription","Digital","User",500,"MS365-KEY","2027-12-31","false",1,"Enterprise"},
                new object[]{"MATLAB Academic","MathWorks","Scientific Computing","Academic","Digital","User",200,"MATLAB-KEY","2028-08-31","true",2,"Academic bundle"},
            };
            AddSampleRows(ws, samples);
            var ws2 = wb.AddWorksheet("Instructions");
            var inst = new[]
            {
                new[]{"Column","Required","Description","Valid Values"},
                new[]{"Installation Name*","YES","Software name","Microsoft 365 E5"},
                new[]{"Publisher Name","NO","Publisher/vendor name","Microsoft, Adobe"},
                new[]{"Software Type*","YES","Category","Free / Paid / Open Source / Trial"},
                new[]{"License Type*","YES","License model","Subscription / Perpetual / Academic / Trial / Freeware"},
                new[]{"License Format*","YES","Media format","Digital / DVD / USB Key"},
                new[]{"Counting Method*","YES","How licenses counted","User / Device / Core / Processor"},
                new[]{"Number of Licenses*","YES","Integer > 0","500"},
                new[]{"License Key","NO","Activation key","XXXXX-XXXXX"},
                new[]{"Expiry Date","NO","YYYY-MM-DD","2027-12-31"},
                new[]{"Academic Flag (true/false)","NO","true or false","false"},
                new[]{"Management Department ID","NO","Dept ID from system","1"},
                new[]{"Description","NO","Notes","Free text"},
            };
            ApplyInstructionSheet(ws2, inst);
            SetColumnWidths(ws, new double[] {25,20,20,16,15,17,22,25,15,25,26,30});
            ws.SheetView.FreezeRows(1);
            return ToBytes(wb);
        }

        // ====================================================================
        // XLSX STYLING HELPERS
        // ====================================================================

        private static void ApplyTemplateHeader(IXLWorksheet ws, string[] headers, int[] requiredColIndices)
        {
            var requiredSet = new HashSet<int>(requiredColIndices);
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.FontName = "Arial";
                cell.Style.Font.FontSize = 10;
                cell.Style.Fill.BackgroundColor = requiredSet.Contains(i + 1)
                    ? XLColor.FromHtml("#003399")
                    : XLColor.FromHtml("#336699");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText   = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#AAAAAA");
            }
            ws.Row(1).Height = 30;
        }

        private static void ApplyErrorSheetHeader(IXLWorksheet ws, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.FontName  = "Arial";
                cell.Style.Font.FontSize  = 10;
                // Last column (Import Error) is gray, others are dark blue
                cell.Style.Fill.BackgroundColor = (i == headers.Length - 1)
                    ? XLColor.FromHtml("#808080")
                    : XLColor.FromHtml("#003399");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#AAAAAA");
            }
            ws.Row(1).Height = 25;
        }

        private static void HighlightErrorRow(IXLWorksheet ws, int rowNum, int colCount)
        {
            for (int c = 1; c <= colCount - 1; c++) // don't highlight error column itself
            {
                ws.Cell(rowNum, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                ws.Cell(rowNum, c).Style.Font.FontColor = XLColor.FromHtml("#856404");
            }
            ws.Cell(rowNum, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA");
            ws.Cell(rowNum, colCount).Style.Font.FontColor = XLColor.FromHtml("#721C24");
        }

        private static void ApplyInstructionSheet(IXLWorksheet ws, string[][] rows)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    var cell = ws.Cell(r + 1, c + 1);
                    cell.Value = rows[r][c];
                    cell.Style.Font.FontName = "Arial";
                    cell.Style.Font.FontSize = 10;
                    if (r == 0)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003399");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (rows[r].Length > 1 && rows[r][1] == "YES")
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F4FD");
                    }
                }
            }
            ws.Column(1).Width = 28;
            ws.Column(2).Width = 10;
            ws.Column(3).Width = 55;
            ws.Column(4).Width = 30;
        }

        private static void AddSampleRows(IXLWorksheet ws, object[][] samples)
        {
            for (int r = 0; r < samples.Length; r++)
            {
                for (int c = 0; c < samples[r].Length; c++)
                {
                    var cell = ws.Cell(r + 2, c + 1);
                    cell.Value = XLCellValue.FromObject(samples[r][c]);
                    cell.Style.Font.FontName  = "Arial";
                    cell.Style.Font.FontSize  = 10;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F8FF");
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#DDDDDD");
                }
            }
        }

        private static void AutoFitColumns(IXLWorksheet ws, int count)
        {
            for (int c = 1; c <= count; c++)
                ws.Column(c).AdjustToContents(1, 1000, 12, 40);
        }

        private static void SetColumnWidths(IXLWorksheet ws, double[] widths)
        {
            for (int i = 0; i < widths.Length; i++)
                ws.Column(i + 1).Width = widths[i];
        }

        private static byte[] ToBytes(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ====================================================================
        // CELL READING HELPERS
        // ====================================================================

        private static string? GetCell(IXLRow row, int col)
        {
            var val = row.Cell(col).GetString()?.Trim();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }

        private static int? GetIntCell(IXLRow row, int col)
        {
            var raw = row.Cell(col).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return int.TryParse(raw, out int v) ? v : null;
        }

        private static DateTime? GetDateCell(IXLRow row, int col)
        {
            var cell = row.Cell(col);
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();
            var raw = cell.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, out var d) ? d : null;
        }

        private static bool IsRowEmpty(IXLRow row, int colCount)
        {
            for (int c = 1; c <= colCount; c++)
                if (!string.IsNullOrWhiteSpace(row.Cell(c).GetString())) return false;
            return true;
        }

        // ====================================================================
        // MISC HELPERS
        // ====================================================================

        private static string GenerateLicenseManagementNumber(string? lastNumber)
        {
            if (string.IsNullOrEmpty(lastNumber)) return "LIC-0001";
            var numPart = lastNumber.Replace("LIC-", "").TrimStart('0');
            if (int.TryParse(numPart, out int current))
                return $"LIC-{(current + 1):D4}";
            return "LIC-0001";
        }
    }
}