using System.Text.Json;
using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new List<SearchResultDto>());
        }

        var query = q.Trim().ToLower();
        var results = new List<SearchResultDto>();

        // 1. Search in Files (Active non-deleted only)
        var files = await _context.RecordFiles
            .Where(f => !f.IsDeleted)
            .Include(f => f.Magazine)
                .ThenInclude(m => m!.Shelf)
                    .ThenInclude(s => s!.Cabinet)
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Include(f => f.DocumentType)
            .ToListAsync();

        foreach (var file in files)
        {
            var matchFound = false;
            var highlight = "";

            if (file.Code.ToLower().Contains(query))
            {
                matchFound = true;
                highlight = $"File Code: {file.Code}";
            }
            else if (file.Title.ToLower().Contains(query))
            {
                matchFound = true;
                highlight = $"File Title: {file.Title}";
            }
            else if (!string.IsNullOrWhiteSpace(file.MetadataJson) && file.MetadataJson.ToLower().Contains(query))
            {
                matchFound = true;
                try
                {
                    using var doc = JsonDocument.Parse(file.MetadataJson);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var val = prop.Value.ToString();
                        if (val.ToLower().Contains(query))
                        {
                            highlight = $"{prop.Name}: {val}";
                            break;
                        }
                    }
                }
                catch
                {
                    highlight = "Matched metadata";
                }
            }

            if (matchFound)
            {
                var shelf = file.Magazine != null ? file.Magazine.Shelf : file.Shelf;
                if (shelf != null && shelf.Cabinet != null)
                {
                    var subtitleParts = new List<string>();
                    try
                    {
                        using var doc = JsonDocument.Parse(file.MetadataJson);
                        if (doc.RootElement.TryGetProperty("department", out var dept)) subtitleParts.Add(dept.GetString() ?? "");
                        if (doc.RootElement.TryGetProperty("designation", out var desig)) subtitleParts.Add(desig.GetString() ?? "");
                        if (doc.RootElement.TryGetProperty("employeeNo", out var empNo) && !string.IsNullOrEmpty(empNo.GetString())) subtitleParts.Add($"ID: {empNo.GetString()}");
                        else if (doc.RootElement.TryGetProperty("staffId", out var staffId) && !string.IsNullOrEmpty(staffId.GetString())) subtitleParts.Add($"Staff ID: {staffId.GetString()}");
                    }
                    catch { }

                    results.Add(new SearchResultDto
                    {
                        Type = "File",
                        Id = file.Id,
                        Title = file.Title,
                        Code = file.Code,
                        Subtitle = subtitleParts.Count > 0 ? string.Join(" • ", subtitleParts.Where(s => !string.IsNullOrEmpty(s))) : (file.DocumentType?.Name ?? "Document"),
                        CabinetNumber = shelf.Cabinet.CabinetNumber,
                        CabinetName = shelf.Cabinet.Name,
                        Section = shelf.Section,
                        ShelfCode = shelf.ShelfCode,
                        ShelfId = shelf.Id,
                        MagazineId = file.MagazineId,
                        MagazineName = file.Magazine?.Name,
                        ColorHex = file.Magazine?.ColorHex ?? "#6366F1",
                        HighlightField = highlight
                    });
                }
            }
        }

        // 2. Search in Magazines (Non-deleted only)
        var magazines = await _context.Magazines
            .Where(m => !m.IsDeleted)
            .Include(m => m.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Where(m => m.Name.ToLower().Contains(query) || m.Code.ToLower().Contains(query))
            .ToListAsync();

        foreach (var mag in magazines)
        {
            if (mag.Shelf != null && mag.Shelf.Cabinet != null)
            {
                results.Add(new SearchResultDto
                {
                    Type = "Magazine",
                    Id = mag.Id,
                    Title = mag.Name,
                    Code = mag.Code,
                    Subtitle = $"Magazine Box ({mag.Code})",
                    CabinetNumber = mag.Shelf.Cabinet.CabinetNumber,
                    CabinetName = mag.Shelf.Cabinet.Name,
                    Section = mag.Shelf.Section,
                    ShelfCode = mag.Shelf.ShelfCode,
                    ShelfId = mag.Shelf.Id,
                    MagazineId = mag.Id,
                    MagazineName = mag.Name,
                    ColorHex = mag.ColorHex,
                    HighlightField = mag.Code.ToLower().Contains(query) ? $"Magazine Code: {mag.Code}" : $"Magazine Name: {mag.Name}"
                });
            }
        }

        // 3. Search in Folders (Non-deleted only)
        var folders = await _context.Folders
            .Where(f => !f.IsDeleted)
            .Include(f => f.Shelf)
                .ThenInclude(s => s!.Cabinet)
            .Where(f => f.Name.ToLower().Contains(query) || f.Code.ToLower().Contains(query))
            .ToListAsync();

        foreach (var folder in folders)
        {
            if (folder.Shelf != null && folder.Shelf.Cabinet != null)
            {
                results.Add(new SearchResultDto
                {
                    Type = "Folder",
                    Id = folder.Id,
                    Title = folder.Name,
                    Code = folder.Code,
                    Subtitle = $"Standalone Binder ({folder.Code})",
                    CabinetNumber = folder.Shelf.Cabinet.CabinetNumber,
                    CabinetName = folder.Shelf.Cabinet.Name,
                    Section = folder.Shelf.Section,
                    ShelfCode = folder.Shelf.ShelfCode,
                    ShelfId = folder.Shelf.Id,
                    MagazineId = null,
                    MagazineName = null,
                    ColorHex = folder.ColorHex,
                    HighlightField = folder.Code.ToLower().Contains(query) ? $"Folder Code: {folder.Code}" : $"Folder Name: {folder.Name}"
                });
            }
        }

        return Ok(results);
    }
}
