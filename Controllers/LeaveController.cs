using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Helpers;
using EmployeeLeaveManagement.Models;
using EmployeeLeaveManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveManagement.Controllers;

[ApiController]
[Route("api/leave")]
[Authorize]
public class LeaveController(ILeaveService svc) : ControllerBase
{
    [HttpGet("types")]
    public async Task<IActionResult> Types() => Ok(await svc.GetTypesAsync());

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(ApplyLeaveDto dto) => Ok(await svc.ApplyAsync(User.GetEmployeeId(), dto));

    [HttpGet("my")]
    public async Task<IActionResult> My() => Ok(await svc.GetMyLeavesAsync(User.GetEmployeeId()));

    [HttpGet("balance")]
    public async Task<IActionResult> Balance() => Ok(await svc.GetBalanceAsync(User.GetEmployeeId()));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id) => Ok(await svc.CancelAsync(id, User.GetEmployeeId()));

    [HttpGet("pending"), Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Pending() => Ok(await svc.GetPendingAsync(User.GetEmployeeId(), User.GetRole()));

    [HttpGet("team"), Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Team() => Ok(await svc.GetTeamHistoryAsync(User.GetEmployeeId(), User.GetRole()));

    [HttpPost("{id:int}/approve"), Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Approve(int id, DecisionDto dto) =>
        Ok(await svc.ApproveAsync(id, User.GetUserId(), User.GetEmployeeId(), User.GetRole(), dto.Comment));

    [HttpPost("{id:int}/reject"), Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Reject(int id, DecisionDto dto) =>
        Ok(await svc.RejectAsync(id, User.GetUserId(), User.GetEmployeeId(), User.GetRole(), dto.Comment));

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications() => Ok(await svc.GetNotificationsAsync(User.GetUserId()));

    [HttpPost("notifications/read")]
    public async Task<IActionResult> MarkRead()
    {
        await svc.MarkNotificationsReadAsync(User.GetUserId());
        return NoContent();
    }
}
