using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrashController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrashController(AppDbContext context)
    {
        _context = context;
    }

    // Automatically purge items older than 30 days
    private async Task PurgeOldItemsAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var oldFiles = await _context.RecordFiles
            .Where(f => f.IsDeleted && f.DeletedAt != null && f.DeletedAt < cutoff)
            .ToListAsync();
        if (oldFiles.Count > 0) _context.RecordFiles.RemoveRange(oldFiles);

        var oldFolders = await _context.Folders
            .Where(f => f.IsDeleted && f.DeletedAt != null && f.DeletedAt < cutoff)
            .ToListAsync();
        if (oldFolders.Count > 0) _context.Folders.RemoveRange(oldFolders);

        var oldMagazines = await _context.Magazines
            .Include(m => m.Files)
            .Where(m => m.IsDeleted && m.DeletedAt != null && m.DeletedAt < cutoff)
            .ToListAsync();
        if (oldMagazines.Count > 0) _context.Magazines.RemoveRange(oldMagazines);

        if (oldFiles.Count > 0 || oldFolders.Count > 0 || oldMagazines.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrashItemDto>>> GetTrashItems()
    {
        await PurgeOldItemsAsync();

        var now = DateTime.UtcNow;
        var trash = new List<TrashItemDto>();

        // Deleted Files
        var files = await _context.RecordFiles
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Include(f => f.Magazine)
            .Where(f => f.IsDeleted)
            .ToListAsync();

        foreach (var file in files)
        {
            var deletedAt = file.DeletedAt ?? now;
            var daysPassed = (now - deletedAt).TotalDays;
            var daysRemaining = Math.Max(0, 30 - (int)daysPassed);

            var loc = file.Magazine != null 
                ? $"Magazine: {file.Magazine.Name}" 
                : (file.Shelf != null ? $"{file.Shelf.Cabinet?.Name} > {file.Shelf.ShelfCode}" : "Unassigned");

            trash.Add(new TrashItemDto
            {
                Type = "File",
                Id = file.Id,
                Title = file.Title,
                Code = file.Code,
                ColorHex = "#6366F1",
                DeletedAt = file.DeletedAt,
                DaysRemaining = daysRemaining,
                OriginalLocation = loc
            });
        }

        // Deleted Folders
        var folders = await _context.Folders
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Where(f => f.IsDeleted)
            .ToListAsync();

        foreach (var fld in folders)
        {
            var deletedAt = fld.DeletedAt ?? now;
            var daysPassed = (now - deletedAt).TotalDays;
            var daysRemaining = Math.Max(0, 30 - (int)daysPassed);

            var loc = fld.Shelf != null 
                ? $"{fld.Shelf.Cabinet?.Name} > {fld.Shelf.ShelfCode}" 
                : "Unassigned";

            trash.Add(new TrashItemDto
            {
                Type = "Folder",
                Id = fld.Id,
                Title = fld.Name,
                Code = fld.Code,
                ColorHex = fld.ColorHex,
                DeletedAt = fld.DeletedAt,
                DaysRemaining = daysRemaining,
                OriginalLocation = loc
            });
        }

        // Deleted Magazines
        var magazines = await _context.Magazines
            .Include(m => m.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Where(m => m.IsDeleted)
            .ToListAsync();

        foreach (var mag in magazines)
        {
            var deletedAt = mag.DeletedAt ?? now;
            var daysPassed = (now - deletedAt).TotalDays;
            var daysRemaining = Math.Max(0, 30 - (int)daysPassed);

            var loc = mag.Shelf != null 
                ? $"{mag.Shelf.Cabinet?.Name} > {mag.Shelf.ShelfCode}" 
                : "Unassigned";

            trash.Add(new TrashItemDto
            {
                Type = "Magazine",
                Id = mag.Id,
                Title = mag.Name,
                Code = mag.Code,
                ColorHex = mag.ColorHex,
                DeletedAt = mag.DeletedAt,
                DaysRemaining = daysRemaining,
                OriginalLocation = loc
            });
        }

        return Ok(trash.OrderByDescending(t => t.DeletedAt));
    }

    [HttpPost("restore/{type}/{id}")]
    public async Task<IActionResult> RestoreItem(string type, int id)
    {
        var defaultShelf = await _context.Shelves.OrderBy(s => s.Id).FirstOrDefaultAsync();
        var defaultShelfId = defaultShelf?.Id ?? 1;

        if (type.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            var file = await _context.RecordFiles.FindAsync(id);
            if (file == null) return NotFound();
            file.IsDeleted = false;
            file.DeletedAt = null;
            if (file.ShelfId == null && file.MagazineId == null)
            {
                file.ShelfId = defaultShelfId;
            }
        }
        else if (type.Equals("Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null) return NotFound();
            folder.IsDeleted = false;
            folder.DeletedAt = null;
            if (folder.ShelfId == null)
            {
                folder.ShelfId = defaultShelfId;
            }
        }
        else if (type.Equals("Magazine", StringComparison.OrdinalIgnoreCase))
        {
            var mag = await _context.Magazines.FindAsync(id);
            if (mag == null) return NotFound();
            mag.IsDeleted = false;
            mag.DeletedAt = null;
            if (mag.ShelfId == null)
            {
                mag.ShelfId = defaultShelfId;
            }
        }
        else
        {
            return BadRequest("Invalid type specified.");
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"{type} restored successfully" });
    }

    [HttpDelete("permanent/{type}/{id}")]
    public async Task<IActionResult> PermanentDelete(string type, int id)
    {
        if (type.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            var file = await _context.RecordFiles.FindAsync(id);
            if (file == null) return NotFound();
            _context.RecordFiles.Remove(file);
        }
        else if (type.Equals("Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null) return NotFound();
            _context.Folders.Remove(folder);
        }
        else if (type.Equals("Magazine", StringComparison.OrdinalIgnoreCase))
        {
            var mag = await _context.Magazines.Include(m => m.Files).FirstOrDefaultAsync(m => m.Id == id);
            if (mag == null) return NotFound();
            _context.Magazines.Remove(mag);
        }
        else
        {
            return BadRequest("Invalid type specified.");
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("empty")]
    public async Task<IActionResult> EmptyTrash()
    {
        var files = await _context.RecordFiles.Where(f => f.IsDeleted).ToListAsync();
        _context.RecordFiles.RemoveRange(files);

        var folders = await _context.Folders.Where(f => f.IsDeleted).ToListAsync();
        _context.Folders.RemoveRange(folders);

        var magazines = await _context.Magazines.Include(m => m.Files).Where(m => m.IsDeleted).ToListAsync();
        _context.Magazines.RemoveRange(magazines);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Trash emptied successfully" });
    }
}

