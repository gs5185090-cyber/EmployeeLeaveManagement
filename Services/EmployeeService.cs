using EmployeeLeaveManagement.Data;
using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeaveManagement.Services;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync(string? search);
    Task<EmployeeDto> GetAsync(int id);
    Task<List<EmployeeDto>> GetTeamAsync(int managerId);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task SetStatusAsync(int id, bool active);
    Task DeleteAsync(int id);
}

public class EmployeeService(ApplicationDbContext db) : IEmployeeService
{
    private static IQueryable<EmployeeDto> Project(IQueryable<Employee> q) =>
        q.Select(e => new EmployeeDto(e.EmployeeId, e.EmployeeCode, e.FirstName, e.LastName, e.Email, e.Phone,
            e.DepartmentId, e.Department!.DepartmentName, e.Designation, e.JoiningDate, e.IsActive, e.ManagerId));

    public Task<List<EmployeeDto>> GetAllAsync(string? search)
    {
        var q = db.Employees.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e => e.EmployeeCode.Contains(s) || e.FirstName.Contains(s) ||
                             e.LastName.Contains(s) || e.Email.Contains(s));
        }
        return Project(q.OrderBy(e => e.EmployeeCode)).ToListAsync();
    }

    public async Task<EmployeeDto> GetAsync(int id) =>
        await Project(db.Employees.Where(e => e.EmployeeId == id)).FirstOrDefaultAsync()
        ?? throw new KeyNotFoundException("Employee not found.");

    public Task<List<EmployeeDto>> GetTeamAsync(int managerId) =>
        Project(db.Employees.Where(e => e.ManagerId == managerId).OrderBy(e => e.FirstName)).ToListAsync();

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        if (!Roles.All.Contains(dto.Role)) throw new InvalidOperationException("Invalid role.");
        if (!await db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
            throw new InvalidOperationException("Department does not exist.");
        if (dto.ManagerId is not null && !await db.Employees.AnyAsync(e => e.EmployeeId == dto.ManagerId))
            throw new InvalidOperationException("Manager does not exist.");
        if (await db.Employees.AnyAsync(e => e.EmployeeCode == dto.EmployeeCode))
            throw new InvalidOperationException("Employee code already exists.");
        if (await db.Employees.AnyAsync(e => e.Email == dto.Email))
            throw new InvalidOperationException("Email already exists.");
        if (await db.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("Username already exists.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var emp = new Employee
        {
            EmployeeCode = dto.EmployeeCode, FirstName = dto.FirstName, LastName = dto.LastName,
            Email = dto.Email, Phone = dto.Phone, DepartmentId = dto.DepartmentId,
            Designation = dto.Designation, JoiningDate = dto.JoiningDate, ManagerId = dto.ManagerId
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.Users.Add(new AppUser
        {
            Username = dto.Username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role, EmployeeId = emp.EmployeeId
        });

        var year = DateTime.Today.Year;
        foreach (var t in await db.LeaveTypes.ToListAsync())
            db.LeaveBalances.Add(new LeaveBalance
            { EmployeeId = emp.EmployeeId, LeaveTypeId = t.LeaveTypeId, Year = year, TotalDays = t.MaxDaysPerYear });

        db.AuditLogs.Add(new() { Action = "EmployeeCreated", Details = emp.EmployeeCode });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await GetAsync(emp.EmployeeId);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var emp = await db.Employees.FindAsync(id) ?? throw new KeyNotFoundException("Employee not found.");
        if (dto.ManagerId == id) throw new InvalidOperationException("An employee cannot be their own manager.");
        if (!await db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
            throw new InvalidOperationException("Department does not exist.");
        if (dto.ManagerId is not null && !await db.Employees.AnyAsync(e => e.EmployeeId == dto.ManagerId))
            throw new InvalidOperationException("Manager does not exist.");
        if (await db.Employees.AnyAsync(e => e.Email == dto.Email && e.EmployeeId != id))
            throw new InvalidOperationException("Email already exists.");

        emp.FirstName = dto.FirstName; emp.LastName = dto.LastName; emp.Email = dto.Email;
        emp.Phone = dto.Phone; emp.DepartmentId = dto.DepartmentId;
        emp.Designation = dto.Designation; emp.ManagerId = dto.ManagerId;
        db.AuditLogs.Add(new() { Action = "EmployeeUpdated", Details = emp.EmployeeCode });
        await db.SaveChangesAsync();
        return await GetAsync(id);
    }

    public async Task SetStatusAsync(int id, bool active)
    {
        var emp = await db.Employees.FindAsync(id) ?? throw new KeyNotFoundException("Employee not found.");
        emp.IsActive = active;
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user is not null) user.IsActive = active;
        db.AuditLogs.Add(new() { Action = active ? "EmployeeActivated" : "EmployeeDeactivated", Details = emp.EmployeeCode });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var emp = await db.Employees.FindAsync(id) ?? throw new KeyNotFoundException("Employee not found.");
        if (await db.Employees.AnyAsync(e => e.ManagerId == id))
            throw new InvalidOperationException("Employee manages a team. Reassign the team first.");
        if (await db.LeaveRequests.AnyAsync(l => l.EmployeeId == id || l.ApprovedBy == id))
            throw new InvalidOperationException("Employee has leave history. Deactivate instead of deleting.");
        db.Employees.Remove(emp);
        db.AuditLogs.Add(new() { Action = "EmployeeDeleted", Details = emp.EmployeeCode });
        await db.SaveChangesAsync();
    }
}
