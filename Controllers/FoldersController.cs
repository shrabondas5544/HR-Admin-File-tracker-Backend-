using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _context;

    public FoldersController(AppDbContext context)
    {
        _context = context;
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
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#10B981" : dto.ColorHex.Trim(),
            ShelfId = dto.ShelfId,
            OrderIndex = maxOrder + 1,
            AttachmentUrl = dto.AttachmentUrl,
            AttachmentName = dto.AttachmentName,
            AttachmentsJson = string.IsNullOrWhiteSpace(dto.AttachmentsJson) ? "[]" : dto.AttachmentsJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(int id, UpdateFolderDto dto)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.IsDeleted) return NotFound();

        if (!folder.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.Folders.AnyAsync(f => f.Id != id && !f.IsDeleted && f.Code.ToLower() == dto.Code.ToLower()))
            {
                return BadRequest($"Folder with code '{dto.Code}' already exists.");
            }
        }

        folder.Name = dto.Name.Trim();
        folder.Code = dto.Code.Trim();
        folder.ColorHex = dto.ColorHex.Trim();
        if (dto.ShelfId.HasValue) folder.ShelfId = dto.ShelfId.Value;
        if (dto.AttachmentUrl != null)
        if (dto.AttachmentsJson != null)
        {
            folder.AttachmentUrl = dto.AttachmentUrl;
            folder.AttachmentName = dto.AttachmentName;
            folder.AttachmentsJson = dto.AttachmentsJson;
        }
        folder.AttachmentUrl = dto.AttachmentUrl;
        folder.AttachmentName = dto.AttachmentName;

        await _context.SaveChangesAsync();
        return Ok(folder);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveFolder(int id, MoveItemDto dto)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.IsDeleted) return NotFound();

        if (!dto.TargetShelfId.HasValue)
        {
            return BadRequest("Target shelf must be specified.");
        }

        folder.ShelfId = dto.TargetShelfId.Value;
        if (dto.OrderIndex.HasValue)
        {
            folder.OrderIndex = dto.OrderIndex.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(folder);
    }

    // Soft delete folder to trash
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var folder = await _context.Folders.FindAsync(id);
        if (folder == null) return NotFound();

        folder.IsDeleted = true;
        folder.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
