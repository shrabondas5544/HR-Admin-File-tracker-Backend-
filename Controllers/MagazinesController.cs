using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MagazinesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MagazinesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Magazine>>> GetMagazines()
    {
        var magazines = await _context.Magazines
            .Where(m => !m.IsDeleted)
            .Include(m => m.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Include(m => m.Files.Where(f => !f.IsDeleted))
            .OrderBy(m => m.Name)
            .ToListAsync();

        return Ok(magazines);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Magazine>> GetMagazine(int id)
    {
        var magazine = await _context.Magazines
            .Include(m => m.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Include(m => m.Files.Where(f => !f.IsDeleted))
                .ThenInclude(f => f.DocumentType)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (magazine == null) return NotFound();
        return Ok(magazine);
    }

    [HttpPost]
    public async Task<ActionResult<Magazine>> CreateMagazine(CreateMagazineDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
        {
            return BadRequest("Name and Code are required.");
        }

        if (await _context.Magazines.AnyAsync(m => !m.IsDeleted && m.Code.ToLower() == dto.Code.ToLower()))
        {
            return BadRequest($"Magazine with code '{dto.Code}' already exists.");
        }

        var maxOrder = await _context.Magazines
            .Where(m => !m.IsDeleted && m.ShelfId == dto.ShelfId)
            .MaxAsync(m => (int?)m.OrderIndex) ?? 0;

        var magazine = new Magazine
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#3B82F6" : dto.ColorHex.Trim(),
            ShelfId = dto.ShelfId,
            OrderIndex = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Magazines.Add(magazine);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMagazine), new { id = magazine.Id }, magazine);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMagazine(int id, UpdateMagazineDto dto)
    {
        var magazine = await _context.Magazines.FindAsync(id);
        if (magazine == null || magazine.IsDeleted) return NotFound();

        if (!magazine.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.Magazines.AnyAsync(m => m.Id != id && !m.IsDeleted && m.Code.ToLower() == dto.Code.ToLower()))
            {
                return BadRequest($"Magazine with code '{dto.Code}' already exists.");
            }
        }

        magazine.Name = dto.Name.Trim();
        magazine.Code = dto.Code.Trim();
        magazine.ColorHex = dto.ColorHex.Trim();
        if (dto.ShelfId.HasValue) magazine.ShelfId = dto.ShelfId.Value;

        await _context.SaveChangesAsync();
        return Ok(magazine);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveMagazine(int id, MoveItemDto dto)
    {
        var magazine = await _context.Magazines.FindAsync(id);
        if (magazine == null || magazine.IsDeleted) return NotFound();

        if (!dto.TargetShelfId.HasValue)
        {
            return BadRequest("Target shelf must be specified.");
        }

        magazine.ShelfId = dto.TargetShelfId.Value;
        if (dto.OrderIndex.HasValue)
        {
            magazine.OrderIndex = dto.OrderIndex.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(magazine);
    }

    // Soft delete magazine with choice of deleting enclosed files or placing them on the shelf
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMagazine(int id, [FromQuery] bool deleteContents = true)
    {
        var magazine = await _context.Magazines
            .Include(m => m.Files)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (magazine == null) return NotFound();

        magazine.IsDeleted = true;
        magazine.DeletedAt = DateTime.UtcNow;

        if (deleteContents)
        {
            // Soft delete all active files inside this magazine box
            foreach (var f in magazine.Files.Where(f => !f.IsDeleted))
            {
                f.IsDeleted = true;
                f.DeletedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Unpack/unbind files from magazine and keep them directly on the shelf where magazine was located
            foreach (var f in magazine.Files.Where(f => !f.IsDeleted))
            {
                f.MagazineId = null;
                f.ShelfId = magazine.ShelfId;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
