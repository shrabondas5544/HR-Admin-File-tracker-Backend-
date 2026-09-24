using System.Security.Claims;
using CabinetMap.Api.Data;
using CabinetMap.Api.Models;

namespace CabinetMap.Api.Services;

public interface IActivityLogger
{
    Task LogAsync(
        string actionType,
        string entityType,
        int? entityId,
        string entityTitle,
        string details,
        HttpContext? httpContext = null,
        User? customUser = null);
}

public class ActivityLogger : IActivityLogger
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogger(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string actionType,
        string entityType,
        int? entityId,
        string entityTitle,
        string details,
        HttpContext? httpContext = null,
        User? customUser = null)
    {
        var context = httpContext ?? _httpContextAccessor.HttpContext;

        int? userId = customUser?.Id;
        string userName = customUser?.FullName ?? "Anonymous User";
        string userEmail = customUser?.Email ?? "";
        string userDesignation = customUser?.Designation ?? "";
        string userGender = customUser?.Gender ?? "Male";

        if (customUser == null && context?.User?.Identity?.IsAuthenticated == true)
        {
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int parsedId))
            {
                userId = parsedId;
            }
            userName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? userName;
            userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? userEmail;
            userDesignation = context.User.FindFirst("Designation")?.Value ?? userDesignation;
            userGender = context.User.FindFirst("Gender")?.Value ?? userGender;
        }

        // Check if fallback headers passed from frontend
        if (context != null)
        {
            if (context.Request.Headers.TryGetValue("X-User-Name", out var hName) && !string.IsNullOrEmpty(hName))
                userName = hName.ToString();
            if (context.Request.Headers.TryGetValue("X-User-Email", out var hEmail) && !string.IsNullOrEmpty(hEmail))
                userEmail = hEmail.ToString();
            if (context.Request.Headers.TryGetValue("X-User-Designation", out var hDesig) && !string.IsNullOrEmpty(hDesig))
                userDesignation = hDesig.ToString();
            if (context.Request.Headers.TryGetValue("X-User-Gender", out var hGender) && !string.IsNullOrEmpty(hGender))
                userGender = hGender.ToString();
            if (context.Request.Headers.TryGetValue("X-User-Id", out var hId) && int.TryParse(hId, out int hParsedId))
                userId = hParsedId;
        }

        var log = new ActivityLog
        {
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "System User" : userName,
            UserEmail = userEmail,
            UserDesignation = string.IsNullOrWhiteSpace(userDesignation) ? "HR Staff" : userDesignation,
            UserGender = string.IsNullOrWhiteSpace(userGender) ? "Male" : userGender,
            ActionType = actionType.ToUpper(),
            EntityType = entityType,
            EntityId = entityId,
            EntityTitle = entityTitle,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
