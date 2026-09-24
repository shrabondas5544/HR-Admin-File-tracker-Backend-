using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using CabinetMap.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MagazinesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public MagazinesController(AppDbContext context, IActivityLogger activityLogger)
    {
        _context = context;
        _activityLogger = activityLogger;
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
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ColorHex = dto.ColorHex ?? "#f59e0b",
            ShelfId = dto.ShelfId,
            OrderIndex = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Magazines.Add(magazine);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "CREATE",
            entityType: "Magazine",
            entityId: magazine.Id,
            entityTitle: $"{magazine.Name} ({magazine.Code})",
            details: $"Created new magazine box '{magazine.Name}' with code [{magazine.Code}].",
            httpContext: HttpContext
        );

        return CreatedAtAction(nameof(GetMagazine), new { id = magazine.Id }, magazine);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveMagazine(int id, MoveItemDto dto)
    {
        var magazine = await _context.Magazines.FindAsync(id);
        if (magazine == null || magazine.IsDeleted) return NotFound();

        if (!dto.TargetShelfId.HasValue)
        {
            return BadRequest("Target shelf must be specified for magazine boxes.");
        }

        magazine.ShelfId = dto.TargetShelfId.Value;
        if (dto.OrderIndex.HasValue)
        {
            magazine.OrderIndex = dto.OrderIndex.Value;
        }

        await _context.SaveChangesAsync();

        var shelf = await _context.Shelves.Include(s => s.Cabinet).FirstOrDefaultAsync(s => s.Id == dto.TargetShelfId.Value);
        var locationDesc = shelf != null ? $"{shelf.Cabinet?.Name} > Shelf {shelf.ShelfCode}" : $"Shelf #{dto.TargetShelfId.Value}";

        await _activityLogger.LogAsync(
            actionType: "MOVE",
            entityType: "Magazine",
            entityId: magazine.Id,
            entityTitle: $"{magazine.Name} ({magazine.Code})",
            details: $"Moved magazine box '{magazine.Name}' [{magazine.Code}] to {locationDesc}.",
            httpContext: HttpContext,
            customUser: !string.IsNullOrWhiteSpace(dto.UserName) ? new User
            {
                FullName = dto.UserName,
                Email = dto.UserEmail ?? "",
                Designation = dto.UserDesignation ?? "",
                Gender = dto.UserGender ?? "Male"
            } : null
        );

        return Ok(magazine);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMagazine(int id)
    {
        var magazine = await _context.Magazines.Include(m => m.Files).FirstOrDefaultAsync(m => m.Id == id);
        if (magazine == null) return NotFound();

        var now = DateTime.UtcNow;
        magazine.IsDeleted = true;
        magazine.DeletedAt = now;

        // Also soft-delete all child files inside magazine
        foreach (var file in magazine.Files)
        {
            file.IsDeleted = true;
            file.DeletedAt = now;
        }

        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "DELETE",
            entityType: "Magazine",
            entityId: magazine.Id,
            entityTitle: $"{magazine.Name} ({magazine.Code})",
            details: $"Moved magazine box '{magazine.Name}' [{magazine.Code}] and its files to Trash Bin.",
            httpContext: HttpContext
        );

        return NoContent();
    }
}
