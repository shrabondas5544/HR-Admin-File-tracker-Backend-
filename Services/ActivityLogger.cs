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

        if (customUser != null && !string.IsNullOrWhiteSpace(customUser.FullName))
        {
            userId = customUser.Id > 0 ? customUser.Id : userId;
            userName = customUser.FullName;
            userEmail = !string.IsNullOrEmpty(customUser.Email) ? customUser.Email : userEmail;
            userDesignation = !string.IsNullOrEmpty(customUser.Designation) ? customUser.Designation : userDesignation;
            userGender = !string.IsNullOrEmpty(customUser.Gender) ? customUser.Gender : userGender;
        }
        else if (context?.User?.Identity?.IsAuthenticated == true)
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

        // Direct JWT Bearer token decode if present
        if (context != null && (userName == "Anonymous User" || string.IsNullOrWhiteSpace(userName)))
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authH))
            {
                var hStr = authH.ToString();
                if (hStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var tokenStr = hStr.Substring(7).Trim();
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(tokenStr))
                        {
                            var jwt = handler.ReadJwtToken(tokenStr);
                            var sub = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
                            if (int.TryParse(sub, out int subId)) userId = subId;

                            var name = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name" || c.Type == "name")?.Value;
                            if (!string.IsNullOrEmpty(name)) userName = name;

                            var email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
                            if (!string.IsNullOrEmpty(email)) userEmail = email;

                            var desig = jwt.Claims.FirstOrDefault(c => c.Type == "Designation")?.Value;
                            if (!string.IsNullOrEmpty(desig)) userDesignation = desig;

                            var gen = jwt.Claims.FirstOrDefault(c => c.Type == "Gender")?.Value;
                            if (!string.IsNullOrEmpty(gen)) userGender = gen;
                        }
                    }
                    catch { }
                }
            }
        }

        // Check fallback headers passed from frontend (case-insensitive)
        if (context != null)
        {
            foreach (var h in context.Request.Headers)
            {
                if (h.Key.Equals("X-User-Name", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(h.Value))
                {
                    try { userName = Uri.UnescapeDataString(h.Value.ToString()); }
                    catch { userName = h.Value.ToString(); }
                }
                else if (h.Key.Equals("X-User-Email", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(h.Value))
                {
                    try { userEmail = Uri.UnescapeDataString(h.Value.ToString()); }
                    catch { userEmail = h.Value.ToString(); }
                }
                else if (h.Key.Equals("X-User-Designation", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(h.Value))
                {
                    try { userDesignation = Uri.UnescapeDataString(h.Value.ToString()); }
                    catch { userDesignation = h.Value.ToString(); }
                }
                else if (h.Key.Equals("X-User-Gender", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(h.Value))
                    userGender = h.Value.ToString();
                else if (h.Key.Equals("X-User-Id", StringComparison.OrdinalIgnoreCase) && int.TryParse(h.Value.ToString(), out int hParsedId))
                    userId = hParsedId;
            }
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
