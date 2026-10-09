using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Models;
using EmployeeLeaveManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveManagement.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IAdminService svc) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await svc.GetDashboardAsync());

    [HttpGet("reports")]
    public async Task<IActionResult> Reports() => Ok(await svc.GetReportsAsync());

    [HttpGet("users")]
    public async Task<IActionResult> Users() => Ok(await svc.GetUsersAsync());

    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleDto dto)
    {
        await svc.ChangeRoleAsync(id, dto.Role);
        return NoContent();
    }

    [HttpPost("leave-types")]
    public async Task<IActionResult> CreateLeaveType(LeaveTypeDto dto) => Ok(await svc.CreateLeaveTypeAsync(dto));

    [HttpPut("leave-types/{id:int}")]
    public async Task<IActionResult> UpdateLeaveType(int id, LeaveTypeDto dto) => Ok(await svc.UpdateLeaveTypeAsync(id, dto));
}
