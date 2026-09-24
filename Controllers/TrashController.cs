using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using CabinetMap.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrashController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public TrashController(AppDbContext context, IActivityLogger activityLogger)
    {
        _context = context;
        _activityLogger = activityLogger;
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

        foreach (var folder in folders)
        {
            var deletedAt = folder.DeletedAt ?? now;
            var daysPassed = (now - deletedAt).TotalDays;
            var daysRemaining = Math.Max(0, 30 - (int)daysPassed);

            var loc = folder.Shelf != null ? $"{folder.Shelf.Cabinet?.Name} > {folder.Shelf.ShelfCode}" : "Unassigned";

            trash.Add(new TrashItemDto
            {
                Type = "Folder",
                Id = folder.Id,
                Title = folder.Name,
                Code = folder.Code,
                ColorHex = folder.ColorHex ?? "#10B981",
                DeletedAt = folder.DeletedAt,
                DaysRemaining = daysRemaining,
                OriginalLocation = loc
            });
        }

        // Deleted Magazines
        var magazines = await _context.Magazines
            .Include(m => m.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Include(m => m.Files)
            .Where(m => m.IsDeleted)
            .ToListAsync();

        foreach (var mag in magazines)
        {
            var deletedAt = mag.DeletedAt ?? now;
            var daysPassed = (now - deletedAt).TotalDays;
            var daysRemaining = Math.Max(0, 30 - (int)daysPassed);

            var loc = mag.Shelf != null ? $"{mag.Shelf.Cabinet?.Name} > {mag.Shelf.ShelfCode}" : "Unassigned";

            trash.Add(new TrashItemDto
            {
                Type = "Magazine",
                Id = mag.Id,
                Title = mag.Name,
                Code = mag.Code,
                ColorHex = mag.ColorHex ?? "#F59E0B",
                DeletedAt = mag.DeletedAt,
                DaysRemaining = daysRemaining,
                OriginalLocation = loc,
                FileCount = mag.Files.Count
            });
        }

        trash.Sort((a, b) => (b.DeletedAt ?? DateTime.MinValue).CompareTo(a.DeletedAt ?? DateTime.MinValue));
        return Ok(trash);
    }

    [HttpPost("restore/{type}/{id}")]
    public async Task<IActionResult> RestoreItem(string type, int id)
    {
        var defaultShelf = await _context.Shelves.OrderBy(s => s.Id).FirstOrDefaultAsync();
        var defaultShelfId = defaultShelf?.Id ?? 1;

        string itemTitle = "";
        string itemCode = "";

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
            itemTitle = file.Title;
            itemCode = file.Code;
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
            itemTitle = folder.Name;
            itemCode = folder.Code;
        }
        else if (type.Equals("Magazine", StringComparison.OrdinalIgnoreCase))
        {
            var mag = await _context.Magazines.Include(m => m.Files).FirstOrDefaultAsync(m => m.Id == id);
            if (mag == null) return NotFound();
            mag.IsDeleted = false;
            mag.DeletedAt = null;
            if (mag.ShelfId == null)
            {
                mag.ShelfId = defaultShelfId;
            }
            foreach (var f in mag.Files)
            {
                f.IsDeleted = false;
                f.DeletedAt = null;
            }
            itemTitle = mag.Name;
            itemCode = mag.Code;
        }
        else
        {
            return BadRequest("Invalid type specified.");
        }

        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "RESTORE",
            entityType: type,
            entityId: id,
            entityTitle: $"{itemTitle} ({itemCode})",
            details: $"Restored {type} '{itemTitle}' [{itemCode}] back from Trash Bin.",
            httpContext: HttpContext
        );

        return Ok(new { message = $"{type} restored successfully" });
    }

    [HttpDelete("permanent/{type}/{id}")]
    public async Task<IActionResult> PermanentDelete(string type, int id)
    {
        string itemTitle = "";

        if (type.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            var file = await _context.RecordFiles.FindAsync(id);
            if (file == null) return NotFound();
            itemTitle = $"{file.Title} ({file.Code})";
            _context.RecordFiles.Remove(file);
        }
        else if (type.Equals("Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null) return NotFound();
            itemTitle = $"{folder.Name} ({folder.Code})";
            _context.Folders.Remove(folder);
        }
        else if (type.Equals("Magazine", StringComparison.OrdinalIgnoreCase))
        {
            var mag = await _context.Magazines.Include(m => m.Files).FirstOrDefaultAsync(m => m.Id == id);
            if (mag == null) return NotFound();
            itemTitle = $"{mag.Name} ({mag.Code})";
            _context.Magazines.Remove(mag);
        }
        else
        {
            return BadRequest("Invalid type specified.");
        }

        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "PERMANENT_DELETE",
            entityType: type,
            entityId: id,
            entityTitle: itemTitle,
            details: $"Permanently deleted {type} '{itemTitle}' from database.",
            httpContext: HttpContext
        );

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

        await _activityLogger.LogAsync(
            actionType: "EMPTY_TRASH",
            entityType: "System",
            entityId: null,
            entityTitle: "Trash Bin",
            details: "Emptied all deleted items from Trash Bin permanently.",
            httpContext: HttpContext
        );

        return Ok(new { message = "Trash emptied successfully" });
    }
}
