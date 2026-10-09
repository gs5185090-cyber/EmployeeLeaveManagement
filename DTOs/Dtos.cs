using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveManagement.DTOs;

public sealed class LoginDto
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public record LoginResponseDto(string Token, string Username, string Role, int EmployeeId, string FullName);

public record EmployeeDto(int EmployeeId, string EmployeeCode, string FirstName, string LastName, string Email,
    string? Phone, int DepartmentId, string DepartmentName, string Designation, DateOnly JoiningDate,
    bool IsActive, int? ManagerId);

public sealed class CreateEmployeeDto
{
    [Required, MaxLength(20)] public string EmployeeCode { get; set; } = "";
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    [Range(1, int.MaxValue)] public int DepartmentId { get; set; }
    [Required, MaxLength(100)] public string Designation { get; set; } = "";
    public DateOnly JoiningDate { get; set; }
    public int? ManagerId { get; set; }
    [Required, MaxLength(50)] public string Username { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Employee";
}

public sealed class UpdateEmployeeDto
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    [Range(1, int.MaxValue)] public int DepartmentId { get; set; }
    [Required, MaxLength(100)] public string Designation { get; set; } = "";
    public int? ManagerId { get; set; }
}

public sealed class DepartmentDto
{
    [Required, MaxLength(100)] public string DepartmentName { get; set; } = "";
    [MaxLength(300)] public string? Description { get; set; }
}

public sealed class LeaveTypeDto
{
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    [Range(1, 365)] public int MaxDaysPerYear { get; set; }
}

public sealed class ApplyLeaveDto
{
    [Range(1, int.MaxValue)] public int LeaveTypeId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
}

public sealed class DecisionDto
{
    [MaxLength(300)] public string? Comment { get; set; }
}

public sealed class ChangeRoleDto
{
    [Required] public string Role { get; set; } = "";
}

public record LeaveResponseDto(int LeaveId, int EmployeeId, string EmployeeName, string LeaveType,
    DateOnly FromDate, DateOnly ToDate, int Days, string Reason, string Status, DateTime AppliedDate,
    string? ApprovedByName, DateTime? ApprovedDate, string? ManagerComment);

public record BalanceDto(string LeaveType, int TotalDays, int UsedDays, int RemainingDays);
public record NotificationDto(int NotificationId, string Message, bool IsRead, DateTime CreatedAt);
public record DashboardDto(int Employees, int Departments, int Pending, int Approved, int Rejected, int OnLeave);
public record NameValueDto(string Name, int Value);
public record ReportDto(List<NameValueDto> DepartmentWise, List<NameValueDto> Monthly);
public record UserDto(int UserId, string Username, string Role, bool IsActive, string EmployeeName);
