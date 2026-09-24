using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using CabinetMap.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public FoldersController(AppDbContext context, IActivityLogger activityLogger)
    {
        _context = context;
        _activityLogger = activityLogger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Folder>>> GetFolders()
    {
        var folders = await _context.Folders
            .Where(f => !f.IsDeleted)
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .OrderBy(f => f.Name)
            .ToListAsync();

        return Ok(folders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Folder>> GetFolder(int id)
    {
        var folder = await _context.Folders
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (folder == null) return NotFound();
        return Ok(folder);
    }

    [HttpPost]
    public async Task<ActionResult<Folder>> CreateFolder(CreateFolderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
        {
            return BadRequest("Name and Code are required.");
        }

        if (await _context.Folders.AnyAsync(f => !f.IsDeleted && f.Code.ToLower() == dto.Code.ToLower()))
        {
            return BadRequest($"Folder with code '{dto.Code}' already exists.");
        }

        var maxOrder = await _context.Folders
            .Where(f => !f.IsDeleted && f.ShelfId == dto.ShelfId)
            .MaxAsync(f => (int?)f.OrderIndex) ?? 0;

        var folder = new Folder
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            ColorHex = dto.ColorHex ?? "#10b981",
            ShelfId = dto.ShelfId,
            OrderIndex = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "CREATE",
            entityType: "Folder",
            entityId: folder.Id,
            entityTitle: $"{folder.Name} ({folder.Code})",
            details: $"Created new folder '{folder.Name}' with code [{folder.Code}].",
            httpContext: HttpContext
        );

        return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveFolder(int id, MoveItemDto dto)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.IsDeleted) return NotFound();

        if (!dto.TargetShelfId.HasValue)
        {
            return BadRequest("Target shelf must be specified for folders.");
        }

        folder.ShelfId = dto.TargetShelfId.Value;
        if (dto.OrderIndex.HasValue)
        {
            folder.OrderIndex = dto.OrderIndex.Value;
        }

        await _context.SaveChangesAsync();

        var shelf = await _context.Shelves.Include(s => s.Cabinet).FirstOrDefaultAsync(s => s.Id == dto.TargetShelfId.Value);
        var locationDesc = shelf != null ? $"{shelf.Cabinet?.Name} > Shelf {shelf.ShelfCode}" : $"Shelf #{dto.TargetShelfId.Value}";

        await _activityLogger.LogAsync(
            actionType: "MOVE",
            entityType: "Folder",
            entityId: folder.Id,
            entityTitle: $"{folder.Name} ({folder.Code})",
            details: $"Moved folder '{folder.Name}' [{folder.Code}] to {locationDesc}.",
            httpContext: HttpContext,
            customUser: !string.IsNullOrWhiteSpace(dto.UserName) ? new User
            {
                FullName = dto.UserName,
                Email = dto.UserEmail ?? "",
                Designation = dto.UserDesignation ?? "",
                Gender = dto.UserGender ?? "Male"
            } : null
        );

        return Ok(folder);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null) return NotFound();

        folder.IsDeleted = true;
        folder.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "DELETE",
            entityType: "Folder",
            entityId: folder.Id,
            entityTitle: $"{folder.Name} ({folder.Code})",
            details: $"Moved folder '{folder.Name}' [{folder.Code}] to Trash Bin.",
            httpContext: HttpContext
        );

        return NoContent();
    }
}
