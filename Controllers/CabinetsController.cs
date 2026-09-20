using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CabinetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CabinetsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cabinet>>> GetCabinets()
    {
        var cabinets = await _context.Cabinets
            .Include(c => c.Shelves.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Magazines.Where(m => !m.IsDeleted).OrderBy(m => m.OrderIndex))
                    .ThenInclude(m => m.Files.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .Include(c => c.Shelves)
                .ThenInclude(s => s.Folders.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .Include(c => c.Shelves)
                .ThenInclude(s => s.StandaloneFiles.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .OrderBy(c => c.CabinetNumber)
            .ToListAsync();

        return Ok(cabinets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cabinet>> GetCabinet(int id)
    {
        var cabinet = await _context.Cabinets
            .Include(c => c.Shelves.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Magazines.Where(m => !m.IsDeleted).OrderBy(m => m.OrderIndex))
                    .ThenInclude(m => m.Files.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .Include(c => c.Shelves)
                .ThenInclude(s => s.Folders.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .Include(c => c.Shelves)
                .ThenInclude(s => s.StandaloneFiles.Where(f => !f.IsDeleted).OrderBy(f => f.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == id || c.CabinetNumber == id);

        if (cabinet == null) return NotFound();
        return Ok(cabinet);
    }

    [HttpGet("flat-shelves")]
    public async Task<ActionResult> GetFlatShelves()
    {
        var shelves = await _context.Shelves
            .Include(s => s.Cabinet)
            .OrderBy(s => s.Cabinet!.CabinetNumber)
            .ThenBy(s => s.Section == "Upper" ? 1 : 2)
            .ThenBy(s => s.OrderIndex)
            .Select(s => new
            {
                s.Id,
                s.CabinetId,
                CabinetNumber = s.Cabinet!.CabinetNumber,
                CabinetName = s.Cabinet.Name,
                s.Section,
                s.ShelfCode,
                s.OrderIndex,
                DisplayName = $"{s.Cabinet.Name} > {s.Section} Section > Shelf {s.ShelfCode}"
            })
            .ToListAsync();

        return Ok(shelves);
    }

    [HttpPost("shelves/{shelfId}/reorder")]
    public async Task<IActionResult> ReorderShelfItems(int shelfId, [FromBody] ReorderShelfDto dto)
    {
        var shelf = await _context.Shelves.FindAsync(shelfId);
        if (shelf == null) return NotFound("Shelf not found.");

        if (dto?.Items == null) return BadRequest("Invalid items payload.");

        foreach (var item in dto.Items)
        {
            if (item.Type.Equals("Magazine", StringComparison.OrdinalIgnoreCase))
            {
                var mag = await _context.Magazines.FindAsync(item.Id);
                if (mag != null && !mag.IsDeleted && mag.ShelfId == shelfId)
                {
                    mag.OrderIndex = item.OrderIndex;
                }
            }
            else if (item.Type.Equals("Folder", StringComparison.OrdinalIgnoreCase))
            {
                var folder = await _context.Folders.FindAsync(item.Id);
                if (folder != null && !folder.IsDeleted && folder.ShelfId == shelfId)
                {
                    folder.OrderIndex = item.OrderIndex;
                }
            }
            else if (item.Type.Equals("File", StringComparison.OrdinalIgnoreCase))
            {
                var file = await _context.RecordFiles.FindAsync(item.Id);
                if (file != null && !file.IsDeleted && file.ShelfId == shelfId)
                {
                    file.OrderIndex = item.OrderIndex;
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
