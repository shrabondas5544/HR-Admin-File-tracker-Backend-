using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;

namespace CabinetMap.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<string> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<UserDto?> GetUserByIdAsync(int userId);
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
    string GenerateJwtToken(User user);
}
