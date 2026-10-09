using EmployeeLeaveManagement.Data;
using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeaveManagement.Controllers;

[ApiController]
[Route("api/department")]
[Authorize]
public class DepartmentController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Departments.OrderBy(d => d.DepartmentName)
            .Select(d => new { d.DepartmentId, d.DepartmentName, d.Description, EmployeeCount = d.Employees.Count })
            .ToListAsync());

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(DepartmentDto dto)
    {
        if (await db.Departments.AnyAsync(d => d.DepartmentName == dto.DepartmentName))
            throw new InvalidOperationException("Department already exists.");
        var dep = new Department { DepartmentName = dto.DepartmentName.Trim(), Description = dto.Description };
        db.Departments.Add(dep);
        await db.SaveChangesAsync();
        return Ok(new { dep.DepartmentId, dep.DepartmentName, dep.Description });
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, DepartmentDto dto)
    {
        var dep = await db.Departments.FindAsync(id) ?? throw new KeyNotFoundException("Department not found.");
        if (await db.Departments.AnyAsync(d => d.DepartmentName == dto.DepartmentName && d.DepartmentId != id))
            throw new InvalidOperationException("Department name already exists.");
        dep.DepartmentName = dto.DepartmentName.Trim();
        dep.Description = dto.Description;
        await db.SaveChangesAsync();
        return Ok(new { dep.DepartmentId, dep.DepartmentName, dep.Description });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var dep = await db.Departments.FindAsync(id) ?? throw new KeyNotFoundException("Department not found.");
        if (await db.Employees.AnyAsync(e => e.DepartmentId == id))
            throw new InvalidOperationException("Department has employees. Move them first.");
        db.Departments.Remove(dep);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
