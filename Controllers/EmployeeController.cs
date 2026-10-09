using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Helpers;
using EmployeeLeaveManagement.Models;
using EmployeeLeaveManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveManagement.Controllers;

[ApiController]
[Route("api/employee")]
[Authorize]
public class EmployeeController(IEmployeeService svc) : ControllerBase
{
    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll([FromQuery] string? search) => Ok(await svc.GetAllAsync(search));

    [HttpGet("{id:int}", Name = "GetEmployee"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Get(int id) => Ok(await svc.GetAsync(id));

    [HttpGet("me")]
    public async Task<IActionResult> Me() => Ok(await svc.GetAsync(User.GetEmployeeId()));

    [HttpGet("team"), Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Team() => Ok(await svc.GetTeamAsync(User.GetEmployeeId()));

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CreateEmployeeDto dto)
    {
        var created = await svc.CreateAsync(dto);
        return CreatedAtRoute("GetEmployee", new { id = created.EmployeeId }, created);
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, UpdateEmployeeDto dto) => Ok(await svc.UpdateAsync(id, dto));

    [HttpPatch("{id:int}/status"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SetStatus(int id, [FromQuery] bool active)
    {
        if (id == User.GetEmployeeId() && !active)
            throw new InvalidOperationException("You cannot deactivate your own account.");
        await svc.SetStatusAsync(id, active);
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == User.GetEmployeeId()) throw new InvalidOperationException("You cannot delete your own account.");
        await svc.DeleteAsync(id);
        return NoContent();
    }
}
