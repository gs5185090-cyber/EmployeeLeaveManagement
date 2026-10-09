using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Helpers;
using EmployeeLeaveManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveManagement.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IEmployeeService employees) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await auth.LoginAsync(dto);
        return result is null ? Unauthorized(new { message = "Invalid username or password." }) : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me() => Ok(await employees.GetAsync(User.GetEmployeeId()));
}
