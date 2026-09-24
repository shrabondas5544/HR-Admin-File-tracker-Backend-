using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CabinetMap.Api.Data;
using CabinetMap.Api.DTOs;
using CabinetMap.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CabinetMap.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower().Trim());
        if (existingUser != null)
        {
            throw new Exception("An account with this email address already exists.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.ToLower().Trim(),
            Designation = dto.Designation.Trim(),
            Gender = dto.Gender.Trim(),
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        user.PasswordHash = HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower().Trim());
        if (user == null || !VerifyPassword(user, dto.Password))
        {
            throw new Exception("Invalid email address or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        if (!VerifyPassword(user, dto.CurrentPassword))
        {
            throw new Exception("Current password is incorrect.");
        }

        user.PasswordHash = HashPassword(user, dto.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower().Trim());
        if (user == null)
        {
            // Do not expose whether email exists for security, return mock success message
            return "If an account exists with this email, a reset code has been generated.";
        }

        // Generate 6-digit random reset code
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        user.ResetCode = code;
        user.ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(30);

        await _context.SaveChangesAsync();
        return code; // Returns reset code so user can enter it immediately in UI
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower().Trim());
        if (user == null)
        {
            throw new Exception("Invalid email or reset code.");
        }

        if (string.IsNullOrEmpty(user.ResetCode) || user.ResetCode != dto.ResetCode.Trim())
        {
            throw new Exception("Invalid reset code.");
        }

        if (user.ResetCodeExpiresAt.HasValue && user.ResetCodeExpiresAt.Value < DateTime.UtcNow)
        {
            throw new Exception("Reset code has expired. Please request a new one.");
        }

        user.PasswordHash = HashPassword(user, dto.NewPassword);
        user.ResetCode = null;
        user.ResetCodeExpiresAt = null;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;
        return MapToUserDto(user);
    }

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["Jwt:Secret"] ?? "CabinetMap_Super_Secure_Secret_Key_2026_JWT_Token_Secret!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Designation", user.Designation),
            new Claim("Gender", user.Gender),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "CabinetMap",
            audience: _configuration["Jwt:Audience"] ?? "CabinetMapClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Designation = user.Designation,
            Gender = user.Gender,
            Role = user.Role
        };
    }
}
