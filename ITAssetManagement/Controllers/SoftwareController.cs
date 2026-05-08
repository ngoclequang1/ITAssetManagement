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
    {
        query = query.Where(s =>
            s.Asset != null &&
            s.Asset.AssetControlNumber.Contains(dto.AssetControlNumber));
    }

    if (!string.IsNullOrEmpty(dto.SoftwareName))
    {
        query = query.Where(s =>
            s.SoftwareName.Contains(dto.SoftwareName));
    }

    if (!string.IsNullOrEmpty(dto.SoftwareVersion))
    {
        query = query.Where(s =>
            s.SoftwareVersion != null &&
            s.SoftwareVersion.Contains(dto.SoftwareVersion));
    }

    if (dto.VendorId.HasValue)
    {
        query = query.Where(s => s.VendorId == dto.VendorId);
    }

    if (dto.LicenseId.HasValue)
    {
        query = query.Where(s => s.LicenseId == dto.LicenseId);
    }

    if (!string.IsNullOrEmpty(dto.LicenseType))
    {
        query = query.Where(s => s.LicenseType == dto.LicenseType);
    }

    if (dto.GroupId.HasValue)
    {
        query = query.Where(s => s.GroupId == dto.GroupId);
    }

    var result = await query
        .Include(s => s.Asset)
        .Include(s => s.InstalledByUser)
        .AsNoTracking()
        .Select(s => new
        {
            softwareId = s.SoftwareId,
            softwareName = s.SoftwareName,
            installationName = s.SoftwareName,

            softwareVersion = s.SoftwareVersion,
            softwareType = s.SoftwareType ?? s.LicenseType,

            assetControlNumber = s.Asset != null ? s.Asset.AssetControlNumber : null,
            assetName = s.Asset != null ? s.Asset.AssetName : null,

            licenseId = s.LicenseId,
            groupId = s.GroupId,

            installedByName = s.InstalledByUser != null
                ? s.InstalledByUser.Username
                : null,

            installedDate = s.InstalledDate
        })
        .ToListAsync();

    return Ok(result);
}
        // ===============================
        // 2. GROUP SOFTWARE
        // ===============================
        [HttpPost("group")]
        public async Task<IActionResult> GroupSoftware([FromBody] SoftwareGroupingDto dto)
        {
            var softwares = await _context.Softwares
                .Where(s => dto.SoftwareIds.Contains(s.SoftwareId))
                .ToListAsync();

            if (!softwares.Any())
                return BadRequest("No software found");

            // tạo group id
            int groupId = new Random().Next(1000, 9999);

            foreach (var s in softwares)
            {
                s.GroupId = groupId;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Grouped successfully",
                groupId = groupId
            });
        }

        // ===============================
        // 3. UNGROUP SOFTWARE
        // ===============================
        [HttpPost("ungroup")]
        public async Task<IActionResult> UngroupSoftware([FromBody] SoftwareGroupingDto dto)
        {
            var softwares = await _context.Softwares
                .Where(s => dto.SoftwareIds.Contains(s.SoftwareId))
                .ToListAsync();

            if (!softwares.Any())
                return BadRequest("No software found");

            foreach (var s in softwares)
            {
                s.GroupId = null;
            }

            await _context.SaveChangesAsync();

            return Ok("Ungrouped successfully");
        }

        // ===============================
        // 4. GET ALL SOFTWARE
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Softwares
                .Include(s => s.Asset)
                .Include(s => s.InstalledByUser)
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
                    AssetName = s.Asset != null ? s.Asset.AssetName : null, 

                    s.GroupId,

                    s.InstalledBy,
                    InstalledByName = s.InstalledByUser != null
                        ? s.InstalledByUser.Username
                        : null, 

                    s.InstalledDate,
                    s.SoftwareType
                })
                .ToListAsync();

            return Ok(list);
        }

        // ===============================
        // 5. INSTALL SOFTWARE
        // ===============================
        [HttpPost("install")]
        public async Task<IActionResult> InstallSoftware([FromBody] InstallSoftwareDto dto)
        {
            // 1. Validate
            if (string.IsNullOrEmpty(dto.SoftwareName) ||
                string.IsNullOrEmpty(dto.SoftwareVersion) ||
                string.IsNullOrEmpty(dto.AssetControlNumber))
            {
                return BadRequest("Missing required fields");
            }

            // 2. Tìm asset
            var asset = await _context.ITAssets
                .FirstOrDefaultAsync(a => a.AssetControlNumber == dto.AssetControlNumber);

            if (asset == null)
            {
                return NotFound("Asset not found");
            }

            // 3. Check duplicate
            var existed = await _context.Softwares
                .FirstOrDefaultAsync(s =>
                    s.SoftwareName == dto.SoftwareName &&
                    s.SoftwareVersion == dto.SoftwareVersion &&
                    s.AssetId == asset.AssetId);

            if (existed != null)
            {
                return BadRequest("Software already installed on this asset");
            }

            // 4. Insert SOFTWARE 
            var software = new Software
            {
                SoftwareName = dto.SoftwareName,
                SoftwareVersion = dto.SoftwareVersion,
                VendorId = dto.VendorId,
                LicenseId = dto.LicenseId,
                LicenseType = dto.LicenseType,
                Description = dto.Description,

                AssetId = asset.AssetId,
                AssetControlNumber = asset.AssetControlNumber,

                InstalledBy = dto.InstalledBy,
                InstalledDate = DateTime.Now
            };

            _context.Softwares.Add(software);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Software installed successfully",
                softwareId = software.SoftwareId,
                assetId = asset.AssetId,
                assetControlNumber = asset.AssetControlNumber
            });
        }

        // ===============================
        // 6. CREATE SOFTWARE
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InstallSoftwareDto dto)
        {
            var software = new Software
            {
                SoftwareName = dto.SoftwareName,
                SoftwareVersion = dto.SoftwareVersion,
                VendorId = dto.VendorId,
                LicenseId = dto.LicenseId,
                LicenseType = dto.LicenseType,
                Description = dto.Description,
                InstalledBy = dto.InstalledBy,
                InstalledDate = DateTime.Now
            };

            _context.Softwares.Add(software);
            await _context.SaveChangesAsync();

            return Ok(software);
        }

        // ===============================
        // 7. UPDATE SOFTWARE
        // ===============================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] InstallSoftwareDto dto)
        {
            var software = await _context.Softwares.FindAsync(id);

            if (software == null)
                return NotFound();

            software.SoftwareName = dto.SoftwareName;
            software.SoftwareVersion = dto.SoftwareVersion;
            software.VendorId = dto.VendorId;
            software.LicenseId = dto.LicenseId;
            software.LicenseType = dto.LicenseType;
            software.Description = dto.Description;
            software.InstalledBy = dto.InstalledBy;

            await _context.SaveChangesAsync();

            return Ok(software);
        }

        // ===============================
        // 8. UNINSTALL SOFTWARE
        // ===============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var software = await _context.Softwares.FindAsync(id);

            if (software == null)
                return NotFound();

            _context.Softwares.Remove(software);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }
    }
}