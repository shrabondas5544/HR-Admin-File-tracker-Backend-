using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using CabinetMap.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public FilesController(AppDbContext context, IActivityLogger activityLogger)
    {
        _context = context;
        _activityLogger = activityLogger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecordFile>>> GetFiles()
    {
        var files = await _context.RecordFiles
            .Where(f => !f.IsDeleted)
            .Include(f => f.DocumentType)
            .Include(f => f.Magazine)
                .ThenInclude(m => m!.Shelf)
                    .ThenInclude(s => s!.Cabinet)
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync();

        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecordFile>> GetFile(int id)
    {
        var file = await _context.RecordFiles
            .Include(f => f.DocumentType)
            .Include(f => f.Magazine)
                .ThenInclude(m => m!.Shelf)
                    .ThenInclude(s => s!.Cabinet)
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (file == null) return NotFound();
        return Ok(file);
    }

    [HttpPost]
    public async Task<ActionResult<RecordFile>> CreateFile(CreateFileDto dto)
    {
        var finalCode = dto.Code?.Trim();
        var finalTitle = dto.Title?.Trim();

        if (string.IsNullOrWhiteSpace(finalCode))
        {
            if (!string.IsNullOrWhiteSpace(dto.MetadataJson) && dto.MetadataJson.Contains("employeeNo"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(dto.MetadataJson);
                    if (doc.RootElement.TryGetProperty("employeeNo", out var empNo) && !string.IsNullOrWhiteSpace(empNo.GetString()))
                    {
                        finalCode = empNo.GetString()!.Trim();
                    }
                    else if (doc.RootElement.TryGetProperty("staffId", out var staffId) && !string.IsNullOrWhiteSpace(staffId.GetString()))
                    {
                        finalCode = staffId.GetString()!.Trim();
                    }
                }
                catch { }
            }
        }

        if (string.IsNullOrWhiteSpace(finalCode))
        {
            finalCode = "FILE-" + DateTime.UtcNow.ToString("yyMMddHHmmss");
        }

        if (string.IsNullOrWhiteSpace(finalTitle))
        {
            finalTitle = "Untitled Record (" + finalCode + ")";
        }

        if (await _context.RecordFiles.AnyAsync(f => !f.IsDeleted && f.Code.ToLower() == finalCode.ToLower()))
        {
            return BadRequest($"File with code '{finalCode}' already exists.");
        }

        var file = new RecordFile
        {
            Code = finalCode,
            Title = finalTitle,
            DocumentTypeId = dto.DocumentTypeId,
            MetadataJson = dto.MetadataJson ?? "{}",
            MagazineId = dto.MagazineId,
            ShelfId = dto.MagazineId == null ? dto.ShelfId : null,
            AttachmentUrl = dto.AttachmentUrl,
            AttachmentName = dto.AttachmentName,
            AttachmentsJson = dto.AttachmentsJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (file.MagazineId.HasValue)
        {
            var maxOrder = await _context.RecordFiles
                .Where(f => !f.IsDeleted && f.MagazineId == file.MagazineId)
                .MaxAsync(f => (int?)f.OrderIndex) ?? 0;
            file.OrderIndex = maxOrder + 1;
        }
        else if (file.ShelfId.HasValue)
        {
            var maxOrder = await _context.RecordFiles
                .Where(f => !f.IsDeleted && f.ShelfId == file.ShelfId)
                .MaxAsync(f => (int?)f.OrderIndex) ?? 0;
            file.OrderIndex = maxOrder + 1;
        }

        _context.RecordFiles.Add(file);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "CREATE",
            entityType: "File",
            entityId: file.Id,
            entityTitle: $"{file.Title} ({file.Code})",
            details: $"Created new record file '{file.Title}' with code [{file.Code}].",
            httpContext: HttpContext
        );

        return CreatedAtAction(nameof(GetFile), new { id = file.Id }, file);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFile(int id, UpdateFileDto dto)
    {
        var file = await _context.RecordFiles.FindAsync(id);
        if (file == null || file.IsDeleted) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            file.Code = dto.Code.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            file.Title = dto.Title.Trim();
        }

        file.DocumentTypeId = dto.DocumentTypeId;
        file.MetadataJson = dto.MetadataJson;
        file.MagazineId = dto.MagazineId;
        file.ShelfId = dto.MagazineId == null ? dto.ShelfId : null;
        if (dto.AttachmentsJson != null)
        {
            file.AttachmentUrl = dto.AttachmentUrl;
            file.AttachmentName = dto.AttachmentName;
            file.AttachmentsJson = dto.AttachmentsJson;
        }
        else if (dto.AttachmentUrl != null)
        {
            file.AttachmentUrl = dto.AttachmentUrl;
            file.AttachmentName = dto.AttachmentName;
        }
        file.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "UPDATE",
            entityType: "File",
            entityId: file.Id,
            entityTitle: $"{file.Title} ({file.Code})",
            details: $"Updated metadata details for file '{file.Title}' [{file.Code}].",
            httpContext: HttpContext
        );

        return Ok(file);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveFile(int id, MoveItemDto dto)
    {
        var file = await _context.RecordFiles.FindAsync(id);
        if (file == null || file.IsDeleted) return NotFound();

        string locationDesc = "";

        if (dto.TargetMagazineId.HasValue)
        {
            file.MagazineId = dto.TargetMagazineId.Value;
            file.ShelfId = null;
            var mag = await _context.Magazines.Include(m => m.Shelf).ThenInclude(s => s!.Cabinet).FirstOrDefaultAsync(m => m.Id == dto.TargetMagazineId.Value);
            locationDesc = mag != null ? $"Magazine Box '{mag.Name}' ({mag.Shelf?.Cabinet?.Name} > Shelf {mag.Shelf?.ShelfCode})" : $"Magazine #{dto.TargetMagazineId.Value}";
        }
        else if (dto.TargetShelfId.HasValue)
        {
            file.ShelfId = dto.TargetShelfId.Value;
            file.MagazineId = null;
            var shelf = await _context.Shelves.Include(s => s.Cabinet).FirstOrDefaultAsync(s => s.Id == dto.TargetShelfId.Value);
            locationDesc = shelf != null ? $"{shelf.Cabinet?.Name} > Shelf {shelf.ShelfCode}" : $"Shelf #{dto.TargetShelfId.Value}";
        }
        else if (file.MagazineId.HasValue)
        {
            var mag = await _context.Magazines.Include(m => m.Shelf).ThenInclude(s => s!.Cabinet).FirstOrDefaultAsync(m => m.Id == file.MagazineId.Value);
            file.ShelfId = mag?.ShelfId;
            file.MagazineId = null;
            locationDesc = mag?.Shelf != null ? $"{mag.Shelf.Cabinet?.Name} > Shelf {mag.Shelf.ShelfCode}" : "Cabinet Shelf";
        }
        else
        {
            return BadRequest("Target shelf or target magazine must be specified.");
        }

        if (dto.OrderIndex.HasValue)
        {
            file.OrderIndex = dto.OrderIndex.Value;
        }

        file.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "MOVE",
            entityType: "File",
            entityId: file.Id,
            entityTitle: $"{file.Title} ({file.Code})",
            details: $"Moved file '{file.Title}' [{file.Code}] to {locationDesc}.",
            httpContext: HttpContext
        );

        return Ok(file);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var file = await _context.RecordFiles.FindAsync(id);
        if (file == null) return NotFound();

        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            actionType: "DELETE",
            entityType: "File",
            entityId: file.Id,
            entityTitle: $"{file.Title} ({file.Code})",
            details: $"Moved file '{file.Title}' [{file.Code}] to Trash Bin.",
            httpContext: HttpContext
        );

        return NoContent();
    }
}
