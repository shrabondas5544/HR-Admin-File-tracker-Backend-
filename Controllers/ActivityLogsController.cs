using CabinetMap.Api.Data;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ActivityLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActivityLog>>> GetLogs(
        [FromQuery] string? search = null,
        [FromQuery] string? actionType = null,
        [FromQuery] int limit = 100)
    {
        var query = _context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(l => l.ActionType.ToUpper() == actionType.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.UserName.ToLower().Contains(term) ||
                l.UserEmail.ToLower().Contains(term) ||
                l.UserDesignation.ToLower().Contains(term) ||
                l.Details.ToLower().Contains(term) ||
                l.EntityTitle.ToLower().Contains(term) ||
                l.ActionType.ToLower().Contains(term)
            );
        }

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(Math.Min(limit, 500))
            .ToListAsync();

        return Ok(logs);
    }
}
