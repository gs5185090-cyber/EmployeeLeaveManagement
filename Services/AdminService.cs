using System.Globalization;
using EmployeeLeaveManagement.Data;
using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeaveManagement.Services;

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<ReportDto> GetReportsAsync();
    Task<List<UserDto>> GetUsersAsync();
    Task ChangeRoleAsync(int userId, string role);
    Task<LeaveType> CreateLeaveTypeAsync(LeaveTypeDto dto);
    Task<LeaveType> UpdateLeaveTypeAsync(int id, LeaveTypeDto dto);
}

public class AdminService(ApplicationDbContext db) : IAdminService
{
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new DashboardDto(
            await db.Employees.CountAsync(e => e.IsActive),
            await db.Departments.CountAsync(),
            await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending),
            await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Approved),
            await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Rejected),
            await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Approved &&
                                                   l.FromDate <= today && l.ToDate >= today));
    }

    public async Task<ReportDto> GetReportsAsync()
    {
        var year = DateTime.Today.Year;
        var dept = await db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Approved && l.FromDate.Year == year)
            .GroupBy(l => l.Employee!.Department!.DepartmentName)
            .Select(g => new NameValueDto(g.Key, g.Sum(x => x.Days))).ToListAsync();

        var monthly = await db.LeaveRequests.Where(l => l.FromDate.Year == year)
            .GroupBy(l => l.FromDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() }).ToListAsync();

        return new ReportDto(dept,
            monthly.OrderBy(m => m.Month)
                .Select(m => new NameValueDto(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m.Month), m.Count))
                .ToList());
    }

    public Task<List<UserDto>> GetUsersAsync() =>
        db.Users.OrderBy(u => u.Username)
            .Select(u => new UserDto(u.UserId, u.Username, u.Role, u.IsActive,
                u.Employee!.FirstName + " " + u.Employee.LastName)).ToListAsync();

    public async Task ChangeRoleAsync(int userId, string role)
    {
        if (!Roles.All.Contains(role)) throw new InvalidOperationException("Invalid role.");
        var user = await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");
        user.Role = role;
        db.AuditLogs.Add(new() { Action = "RoleChanged", Details = $"{user.Username} -> {role}" });
        await db.SaveChangesAsync();
    }

    public async Task<LeaveType> CreateLeaveTypeAsync(LeaveTypeDto dto)
    {
        if (await db.LeaveTypes.AnyAsync(t => t.Name == dto.Name))
            throw new InvalidOperationException("Leave type already exists.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var type = new LeaveType { Name = dto.Name.Trim(), MaxDaysPerYear = dto.MaxDaysPerYear };
        db.LeaveTypes.Add(type);
        await db.SaveChangesAsync();

        var year = DateTime.Today.Year;
        foreach (var empId in await db.Employees.Select(e => e.EmployeeId).ToListAsync())
            db.LeaveBalances.Add(new LeaveBalance
            { EmployeeId = empId, LeaveTypeId = type.LeaveTypeId, Year = year, TotalDays = type.MaxDaysPerYear });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return type;
    }

    public async Task<LeaveType> UpdateLeaveTypeAsync(int id, LeaveTypeDto dto)
    {
        var type = await db.LeaveTypes.FindAsync(id) ?? throw new KeyNotFoundException("Leave type not found.");
        if (await db.LeaveTypes.AnyAsync(t => t.Name == dto.Name && t.LeaveTypeId != id))
            throw new InvalidOperationException("Leave type name already exists.");
        type.Name = dto.Name.Trim();
        type.MaxDaysPerYear = dto.MaxDaysPerYear;
        var year = DateTime.Today.Year;
        await db.LeaveBalances.Where(b => b.LeaveTypeId == id && b.Year == year)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.TotalDays, dto.MaxDaysPerYear));
        db.AuditLogs.Add(new() { Action = "LeaveTypeUpdated", Details = type.Name });
        await db.SaveChangesAsync();
        return type;
    }
}
