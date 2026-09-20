using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FilesController(AppDbContext context)
    {
        _context = context;
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

        // If code not provided, derive from metadata (employeeNo or staffId) or fallback
        if (string.IsNullOrWhiteSpace(finalCode))
        {
            if (!string.IsNullOrWhiteSpace(dto.MetadataJson))
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

            if (string.IsNullOrWhiteSpace(finalCode))
            {
                finalCode = $"FILE-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }
        }

        // If title not provided, derive from employeeName or fallback to code
        if (string.IsNullOrWhiteSpace(finalTitle))
        {
            if (!string.IsNullOrWhiteSpace(dto.MetadataJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(dto.MetadataJson);
                    if (doc.RootElement.TryGetProperty("employeeName", out var empName) && !string.IsNullOrWhiteSpace(empName.GetString()))
                    {
                        finalTitle = $"{empName.GetString()!.Trim()} ({finalCode})";
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(finalTitle))
            {
                finalTitle = finalCode;
            }
        }

        var file = new RecordFile
        {
            Code = finalCode,
            Title = finalTitle,
            DocumentTypeId = dto.DocumentTypeId,
            MetadataJson = string.IsNullOrWhiteSpace(dto.MetadataJson) ? "{}" : dto.MetadataJson,
            MagazineId = dto.MagazineId,
            ShelfId = dto.MagazineId == null ? dto.ShelfId : null,
            AttachmentUrl = dto.AttachmentUrl,
            AttachmentName = dto.AttachmentName,
            AttachmentsJson = string.IsNullOrWhiteSpace(dto.AttachmentsJson) ? "[]" : dto.AttachmentsJson,
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
        return Ok(file);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveFile(int id, MoveItemDto dto)
    {
        var file = await _context.RecordFiles.FindAsync(id);
        if (file == null || file.IsDeleted) return NotFound();

        if (dto.TargetMagazineId.HasValue)
        {
            file.MagazineId = dto.TargetMagazineId.Value;
            file.ShelfId = null;
        }
        else if (dto.TargetShelfId.HasValue)
        {
            file.ShelfId = dto.TargetShelfId.Value;
            file.MagazineId = null;
        }
        else if (file.MagazineId.HasValue)
        {
            // Extract from magazine to magazine's shelf
            var mag = await _context.Magazines.FindAsync(file.MagazineId.Value);
            file.ShelfId = mag?.ShelfId;
            file.MagazineId = null;
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

        return Ok(file);
    }

    // Soft delete to Trash
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var file = await _context.RecordFiles.FindAsync(id);
        if (file == null) return NotFound();

        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
