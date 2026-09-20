using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DocumentTypesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentType>>> GetDocumentTypes()
    {
        return await _context.DocumentTypes.OrderBy(d => d.Id).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<DocumentType>> CreateDocumentType(CreateDocumentTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("Document type name is required.");
        }

        var docType = new DocumentType
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            IsBuiltIn = false,
            FieldsJson = string.IsNullOrWhiteSpace(dto.FieldsJson) ? "[]" : dto.FieldsJson
        };

        _context.DocumentTypes.Add(docType);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocumentTypes), new { id = docType.Id }, docType);
    }
}

