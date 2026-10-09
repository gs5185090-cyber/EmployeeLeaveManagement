using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeLeaveManagement.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string ManagerOrAdmin = "Manager,Admin";
    public static readonly string[] All = [Admin, Manager, Employee];
}

public enum LeaveStatus { Pending, Approved, Rejected, Cancelled }

public class Department
{
    public int DepartmentId { get; set; }
    [Required, MaxLength(100)] public string DepartmentName { get; set; } = "";
    [MaxLength(300)] public string? Description { get; set; }
    public List<Employee> Employees { get; set; } = new();
}

public class Employee
{
    public int EmployeeId { get; set; }
    [Required, MaxLength(20)] public string EmployeeCode { get; set; } = "";
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    [Required, MaxLength(100)] public string Designation { get; set; } = "";
    public DateOnly JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
}

public class AppUser
{
    [Key]public int UserId { get; set; }
    [Required, MaxLength(50)] public string Username { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required, MaxLength(20)] public string Role { get; set; } = Roles.Employee;
    public bool IsActive { get; set; } = true;
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public class LeaveType
{
    public int LeaveTypeId { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    public int MaxDaysPerYear { get; set; }
}

public class LeaveRequest
{
    [Key] public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int Days { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = "";
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public int? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedDate { get; set; }
    [MaxLength(300)] public string? ManagerComment { get; set; }
}

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    [NotMapped] public int RemainingDays => TotalDays - UsedDays;
}

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [Required, MaxLength(300)] public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    [Required, MaxLength(100)] public string Action { get; set; } = "";
    [MaxLength(500)] public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
