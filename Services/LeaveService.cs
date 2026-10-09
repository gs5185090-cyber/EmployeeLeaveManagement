using EmployeeLeaveManagement.Data;
using EmployeeLeaveManagement.DTOs;
using EmployeeLeaveManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeaveManagement.Services;

public interface ILeaveService
{
    Task<List<LeaveType>> GetTypesAsync();
    Task<LeaveResponseDto> ApplyAsync(int employeeId, ApplyLeaveDto dto);
    Task<List<LeaveResponseDto>> GetMyLeavesAsync(int employeeId);
    Task<List<BalanceDto>> GetBalanceAsync(int employeeId);
    Task<LeaveResponseDto> CancelAsync(int leaveId, int employeeId);
    Task<List<LeaveResponseDto>> GetPendingAsync(int employeeId, string role);
    Task<List<LeaveResponseDto>> GetTeamHistoryAsync(int employeeId, string role);
    Task<LeaveResponseDto> ApproveAsync(int leaveId, int userId, int employeeId, string role, string? comment);
    Task<LeaveResponseDto> RejectAsync(int leaveId, int userId, int employeeId, string role, string? comment);
    Task<List<NotificationDto>> GetNotificationsAsync(int userId);
    Task MarkNotificationsReadAsync(int userId);
}

public class LeaveService(ApplicationDbContext db) : ILeaveService
{
    private static IQueryable<LeaveResponseDto> Project(IQueryable<LeaveRequest> q) =>
        q.OrderByDescending(l => l.AppliedDate).Select(l => new LeaveResponseDto(
            l.LeaveId, l.EmployeeId, l.Employee!.FirstName + " " + l.Employee.LastName, l.LeaveType!.Name,
            l.FromDate, l.ToDate, l.Days, l.Reason, l.Status.ToString(), l.AppliedDate,
            l.Approver == null ? null : l.Approver.FirstName + " " + l.Approver.LastName,
            l.ApprovedDate, l.ManagerComment));

    private static int CountWorkingDays(DateOnly from, DateOnly to)
    {
        var days = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) days++;
        return days;
    }

    public Task<List<LeaveType>> GetTypesAsync() => db.LeaveTypes.OrderBy(t => t.LeaveTypeId).ToListAsync();

    public async Task<LeaveResponseDto> ApplyAsync(int employeeId, ApplyLeaveDto dto)
    {
        if (dto.ToDate < dto.FromDate) throw new InvalidOperationException("To date cannot be before from date.");
        if (dto.FromDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("Leave cannot start in the past.");
        if (dto.FromDate.Year != dto.ToDate.Year)
            throw new InvalidOperationException("Leave cannot span two years. Please apply separately.");

        var type = await db.LeaveTypes.FindAsync(dto.LeaveTypeId)
                   ?? throw new KeyNotFoundException("Leave type not found.");
        var days = CountWorkingDays(dto.FromDate, dto.ToDate);
        if (days == 0) throw new InvalidOperationException("Selected dates contain only weekends.");

        var overlap = await db.LeaveRequests.AnyAsync(l => l.EmployeeId == employeeId &&
            (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved) &&
            l.FromDate <= dto.ToDate && l.ToDate >= dto.FromDate);
        if (overlap) throw new InvalidOperationException("You already have a leave request for these dates.");

        var year = dto.FromDate.Year;
        var bal = await db.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                      b.LeaveTypeId == type.LeaveTypeId && b.Year == year)
                  ?? throw new InvalidOperationException("No leave balance allocated for this year.");
        var reserved = await db.LeaveRequests.Where(l => l.EmployeeId == employeeId &&
            l.LeaveTypeId == type.LeaveTypeId && l.Status == LeaveStatus.Pending && l.FromDate.Year == year)
            .SumAsync(l => l.Days);
        if (bal.TotalDays - bal.UsedDays - reserved < days)
            throw new InvalidOperationException($"Insufficient {type.Name} balance. Requested {days} day(s).");

        var req = new LeaveRequest
        {
            EmployeeId = employeeId, LeaveTypeId = type.LeaveTypeId, FromDate = dto.FromDate,
            ToDate = dto.ToDate, Days = days, Reason = dto.Reason.Trim()
        };
        db.LeaveRequests.Add(req);

        var emp = await db.Employees.FindAsync(employeeId);
        if (emp?.ManagerId is int mgrId)
        {
            var mgrUser = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == mgrId);
            if (mgrUser is not null)
                db.Notifications.Add(new()
                { UserId = mgrUser.UserId, Message = $"{emp.FirstName} {emp.LastName} applied for {days} day(s) of {type.Name}." });
        }
        db.AuditLogs.Add(new() { Action = "LeaveApplied", Details = $"Employee {employeeId}, {days} day(s)" });
        await db.SaveChangesAsync();
        return await Project(db.LeaveRequests.Where(l => l.LeaveId == req.LeaveId)).FirstAsync();
    }

    public Task<List<LeaveResponseDto>> GetMyLeavesAsync(int employeeId) =>
        Project(db.LeaveRequests.Where(l => l.EmployeeId == employeeId)).ToListAsync();

    public Task<List<BalanceDto>> GetBalanceAsync(int employeeId)
    {
        var year = DateTime.Today.Year;
        return db.LeaveBalances.Where(b => b.EmployeeId == employeeId && b.Year == year)
            .OrderBy(b => b.LeaveTypeId)
            .Select(b => new BalanceDto(b.LeaveType!.Name, b.TotalDays, b.UsedDays, b.TotalDays - b.UsedDays))
            .ToListAsync();
    }

    public async Task<LeaveResponseDto> CancelAsync(int leaveId, int employeeId)
    {
        var req = await db.LeaveRequests.FindAsync(leaveId) ?? throw new KeyNotFoundException("Leave request not found.");
        if (req.EmployeeId != employeeId) throw new UnauthorizedAccessException("This is not your leave request.");
        if (req.Status != LeaveStatus.Pending) throw new InvalidOperationException("Only pending requests can be cancelled.");
        req.Status = LeaveStatus.Cancelled;
        db.AuditLogs.Add(new() { Action = "LeaveCancelled", Details = $"Leave {leaveId}" });
        await db.SaveChangesAsync();
        return await Project(db.LeaveRequests.Where(l => l.LeaveId == leaveId)).FirstAsync();
    }

    public Task<List<LeaveResponseDto>> GetPendingAsync(int employeeId, string role)
    {
        var q = db.LeaveRequests.Where(l => l.Status == LeaveStatus.Pending && l.EmployeeId != employeeId);
        if (role == Roles.Manager) q = q.Where(l => l.Employee!.ManagerId == employeeId);
        return Project(q).ToListAsync();
    }

    public Task<List<LeaveResponseDto>> GetTeamHistoryAsync(int employeeId, string role)
    {
        var q = db.LeaveRequests.AsQueryable();
        if (role == Roles.Manager) q = q.Where(l => l.Employee!.ManagerId == employeeId);
        return Project(q).ToListAsync();
    }

    public Task<LeaveResponseDto> ApproveAsync(int leaveId, int userId, int employeeId, string role, string? comment) =>
        DecideAsync(leaveId, userId, employeeId, role, true, comment);

    public Task<LeaveResponseDto> RejectAsync(int leaveId, int userId, int employeeId, string role, string? comment) =>
        DecideAsync(leaveId, userId, employeeId, role, false, comment);

    private async Task<LeaveResponseDto> DecideAsync(int leaveId, int userId, int actorEmployeeId, string role,
        bool approve, string? comment)
    {
        var req = await db.LeaveRequests.Include(l => l.Employee).Include(l => l.LeaveType)
                      .FirstOrDefaultAsync(l => l.LeaveId == leaveId)
                  ?? throw new KeyNotFoundException("Leave request not found.");
        if (req.Status != LeaveStatus.Pending) throw new InvalidOperationException("Request is no longer pending.");
        if (req.EmployeeId == actorEmployeeId) throw new UnauthorizedAccessException("You cannot decide your own leave request.");
        if (role == Roles.Manager && req.Employee!.ManagerId != actorEmployeeId)
            throw new UnauthorizedAccessException("This employee is not in your team.");

        await using var tx = await db.Database.BeginTransactionAsync();
        if (approve)
        {
            var bal = await db.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == req.EmployeeId &&
                          b.LeaveTypeId == req.LeaveTypeId && b.Year == req.FromDate.Year)
                      ?? throw new InvalidOperationException("Leave balance not found.");
            if (bal.TotalDays - bal.UsedDays < req.Days)
                throw new InvalidOperationException("Employee has insufficient leave balance.");
            bal.UsedDays += req.Days;
        }
        req.Status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        req.ApprovedBy = actorEmployeeId;
        req.ApprovedDate = DateTime.UtcNow;
        req.ManagerComment = comment;

        var empUser = await db.Users.FirstOrDefaultAsync(u => u.EmployeeId == req.EmployeeId);
        if (empUser is not null)
            db.Notifications.Add(new()
            {
                UserId = empUser.UserId,
                Message = $"Your {req.LeaveType!.Name} ({req.FromDate:dd MMM} - {req.ToDate:dd MMM}) was {req.Status.ToString().ToLower()}."
            });
        db.AuditLogs.Add(new() { UserId = userId, Action = approve ? "LeaveApproved" : "LeaveRejected", Details = $"Leave {leaveId}" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await Project(db.LeaveRequests.Where(l => l.LeaveId == leaveId)).FirstAsync();
    }

    public Task<List<NotificationDto>> GetNotificationsAsync(int userId) =>
        db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(20)
            .Select(n => new NotificationDto(n.NotificationId, n.Message, n.IsRead, n.CreatedAt)).ToListAsync();

    public async Task MarkNotificationsReadAsync(int userId)
    {
        await db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
