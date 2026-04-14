using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Data;
using ITAssetManagement.Models;

namespace ITAssetManagement.Controllers
{
    [Route("api/hardware")]
    [ApiController]
    public class HardwareController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HardwareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // 1. Get All Hardware
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetHardwareList()
        {
            var hardwareList = await _context.ITAssets
                .Include(a => a.Category)
                .Include(a => a.Status)
                .Include(a => a.Location)
                .Select(a => new
                {
                    a.AssetId,
                    a.AssetControlNumber,
                    a.AssetName,
                    a.Manufacturer,
                    a.Model,
                    a.SerialNumber,
                    Category = a.Category.CategoryName,
                    Status = a.Status.StatusName,
                    Location = a.Location.LocationName
                })
                .ToListAsync();

            return Ok(hardwareList);
        }

        // ===============================
        // 2. Get Hardware Detail
        // ===============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHardwareDetail(int id)
        {
            var asset = await _context.ITAssets
                .Include(a => a.Category)
                .Include(a => a.Status)
                .Include(a => a.Location)
                .Where(a => a.AssetId == id)
                .Select(a => new
                {
                    a.AssetId,
                    a.AssetControlNumber,
                    a.AssetName,
                    a.Manufacturer,
                    a.Model,
                    a.SerialNumber,
                    Category = a.Category.CategoryName,
                    Status = a.Status.StatusName,
                    Location = a.Location.LocationName
                })
                .FirstOrDefaultAsync();

            if (asset == null)
                return NotFound("Hardware not found");

            return Ok(asset);
        }

        // ===============================
        // 3. Create Hardware
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CreateHardware([FromBody] ITAsset asset)
        {
            _context.ITAssets.Add(asset);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hardware created successfully",
                asset.AssetId
            });
        }

        // ===============================
        // 4. Update Hardware
        // ===============================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHardware(int id, [FromBody] ITAsset updatedAsset)
        {
            var asset = await _context.ITAssets.FindAsync(id);

            if (asset == null)
                return NotFound("Hardware not found");

            asset.AssetName = updatedAsset.AssetName;
            asset.Manufacturer = updatedAsset.Manufacturer;
            asset.Model = updatedAsset.Model;
            asset.SerialNumber = updatedAsset.SerialNumber;
            asset.AssetControlNumber = updatedAsset.AssetControlNumber;
            asset.CategoryId = updatedAsset.CategoryId;
            asset.StatusId = updatedAsset.StatusId;
            asset.LocationId = updatedAsset.LocationId;

            await _context.SaveChangesAsync();

            return Ok("Hardware updated successfully");
        }

        // ===============================
        // 5. Delete Hardware
        // ===============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHardware(int id)
        {
            var asset = await _context.ITAssets.FindAsync(id);

            if (asset == null)
                return NotFound("Hardware not found");

            _context.ITAssets.Remove(asset);
            await _context.SaveChangesAsync();

            return Ok("Hardware deleted successfully");
        }

        // ===============================
        // SEARCH HARDWARE
        // ===============================
        [HttpPost("search")]
        public async Task<IActionResult> SearchHardware([FromBody] ITAssetManagement.DTOs.HardwareSearchDto dto)
        {
            var query = _context.ITAssets
                .Include(a => a.Category)
                .Include(a => a.Status)
                .Include(a => a.Location)
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.AssetControlNumber))
                query = query.Where(a => a.AssetControlNumber.Contains(dto.AssetControlNumber));

            if (!string.IsNullOrEmpty(dto.AssetName))
                query = query.Where(a => a.AssetName.Contains(dto.AssetName));

            if (!string.IsNullOrEmpty(dto.Manufacturer))
                query = query.Where(a => a.Manufacturer.Contains(dto.Manufacturer));

            if (!string.IsNullOrEmpty(dto.Model))
                query = query.Where(a => a.Model.Contains(dto.Model));

            var total = await query.CountAsync();

            int page = dto.Page > 0 ? dto.Page : 1;
            int pageSize = dto.PageSize > 0 ? dto.PageSize : 10;

            var hardwareList = await query
                .OrderBy(a => a.AssetId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.AssetId,
                    a.AssetControlNumber,
                    a.AssetName,
                    a.Manufacturer,
                    a.Model,
                    a.SerialNumber,
                    Category = a.Category != null ? a.Category.CategoryName : null,
                    Status = a.Status != null ? a.Status.StatusName : null,
                    Location = a.Location != null ? a.Location.LocationName : null
                })
                .ToListAsync();

            return Ok(new { total = total, data = hardwareList });
        }
    }
}