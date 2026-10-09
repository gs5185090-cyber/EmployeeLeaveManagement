using System.Security.Claims;
using System.Text;
using EmployeeLeaveManagement.Data;
using EmployeeLeaveManagement.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeLeaveManagement.Services;

public interface IAuthService { Task<LoginResponseDto?> LoginAsync(LoginDto dto); }

public class AuthService(ApplicationDbContext db, IConfiguration cfg) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await db.Users.Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user is null || !user.IsActive || user.Employee is null || !user.Employee.IsActive ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("employeeId", user.EmployeeId.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(cfg["Jwt:ExpiryMinutes"] ?? "480")),
            Issuer = cfg["Jwt:Issuer"],
            Audience = cfg["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        db.AuditLogs.Add(new() { UserId = user.UserId, Action = "Login", Details = user.Username });
        await db.SaveChangesAsync();

        return new LoginResponseDto(token, user.Username, user.Role, user.EmployeeId,
            $"{user.Employee.FirstName} {user.Employee.LastName}");
    }
}
