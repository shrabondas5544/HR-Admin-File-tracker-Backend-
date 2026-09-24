using System.Security.Claims;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using CabinetMap.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinetMap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IActivityLogger _activityLogger;

    public AuthController(IAuthService authService, IActivityLogger activityLogger)
    {
        _authService = authService;
        _activityLogger = activityLogger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var response = await _authService.RegisterAsync(dto);

            await _activityLogger.LogAsync(
                actionType: "REGISTER",
                entityType: "User",
                entityId: response.User.Id,
                entityTitle: response.User.FullName,
                details: $"{response.User.FullName} ({response.User.Designation}) registered a new account.",
                httpContext: HttpContext,
                customUser: new User
                {
                    Id = response.User.Id,
                    FullName = response.User.FullName,
                    Email = response.User.Email,
                    Designation = response.User.Designation,
                    Gender = response.User.Gender
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var response = await _authService.LoginAsync(dto);

            await _activityLogger.LogAsync(
                actionType: "LOGIN",
                entityType: "User",
                entityId: response.User.Id,
                entityTitle: response.User.FullName,
                details: $"{response.User.FullName} ({response.User.Designation}) logged into the system.",
                httpContext: HttpContext,
                customUser: new User
                {
                    Id = response.User.Id,
                    FullName = response.User.FullName,
                    Email = response.User.Email,
                    Designation = response.User.Designation,
                    Gender = response.User.Gender
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { message = "Authentication required." });

            await _authService.ChangePasswordAsync(userId, dto);

            var user = await _authService.GetUserByIdAsync(userId);

            await _activityLogger.LogAsync(
                actionType: "PASSWORD_CHANGE",
                entityType: "User",
                entityId: userId,
                entityTitle: user?.FullName ?? "User",
                details: $"User {user?.FullName ?? ""} changed their account password.",
                httpContext: HttpContext
            );

            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var resetCode = await _authService.ForgotPasswordAsync(dto);
            return Ok(new
            {
                message = "Password reset code generated.",
                resetCode = resetCode
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);

            await _activityLogger.LogAsync(
                actionType: "PASSWORD_RESET",
                entityType: "User",
                entityId: null,
                entityTitle: dto.Email,
                details: $"Password reset completed for account {dto.Email}.",
                httpContext: HttpContext
            );

            return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(user);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out int userId)) return userId;

        if (Request.Headers.TryGetValue("X-User-Id", out var headerId) && int.TryParse(headerId, out int hUserId))
        {
            return hUserId;
        }

        return 0;
    }
}
