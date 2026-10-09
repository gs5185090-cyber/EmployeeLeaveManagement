using EmployeeLeaveManagement.Models;

namespace EmployeeLeaveManagement.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        if (db.Users.Any()) return;

        var depts = new[]
        {
            new Department { DepartmentName = "IT", Description = "Information Technology" },
            new Department { DepartmentName = "HR", Description = "Human Resources" },
            new Department { DepartmentName = "Finance", Description = "Accounts and Finance" },
            new Department { DepartmentName = "Sales", Description = "Sales and Business Development" },
            new Department { DepartmentName = "Marketing", Description = "Marketing and Communications" }
        };
        db.Departments.AddRange(depts);

        var types = new[]
        {
            new LeaveType { Name = "Casual Leave", MaxDaysPerYear = 12 },
            new LeaveType { Name = "Sick Leave", MaxDaysPerYear = 10 },
            new LeaveType { Name = "Earned Leave", MaxDaysPerYear = 20 },
            new LeaveType { Name = "Emergency Leave", MaxDaysPerYear = 5 }
        };
        db.LeaveTypes.AddRange(types);
        db.SaveChanges();

        Employee Make(string code, string first, string last, string email, Department d, string title) => new()
        {
            EmployeeCode = code, FirstName = first, LastName = last, Email = email,
            DepartmentId = d.DepartmentId, Designation = title,
            JoiningDate = new DateOnly(2024, 1, 15), Phone = "9876543210"
        };

        var admin = Make("EMP001", "System", "Admin", "admin@company.com", depts[1], "HR Admin");
        var manager = Make("EMP002", "Arun", "Kumar", "manager@company.com", depts[0], "Engineering Manager");
        db.Employees.AddRange(admin, manager);
        db.SaveChanges();

        var gowri = Make("EMP003", "Gowri", "S", "gowri@company.com", depts[0], "Software Developer");
        var rahul = Make("EMP004", "Rahul", "Verma", "rahul@company.com", depts[0], "QA Engineer");
        gowri.ManagerId = manager.EmployeeId;
        rahul.ManagerId = manager.EmployeeId;
        db.Employees.AddRange(gowri, rahul);
        db.SaveChanges();

        AppUser U(string name, string pwd, string role, Employee e) => new()
        {
            Username = name, PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd), Role = role, EmployeeId = e.EmployeeId
        };
        db.Users.AddRange(
            U("admin", "Admin@123", Roles.Admin, admin),
            U("manager", "Manager@123", Roles.Manager, manager),
            U("gowri", "Employee@123", Roles.Employee, gowri),
            U("rahul", "Employee@123", Roles.Employee, rahul));

        var year = DateTime.Today.Year;
        foreach (var e in new[] { admin, manager, gowri, rahul })
            foreach (var t in types)
                db.LeaveBalances.Add(new LeaveBalance
                { EmployeeId = e.EmployeeId, LeaveTypeId = t.LeaveTypeId, Year = year, TotalDays = t.MaxDaysPerYear });

        db.SaveChanges();
    }
}
