using System.Security.Claims;

namespace EmployeeLeaveManagement.Helpers;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static int GetEmployeeId(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue("employeeId")!);
    public static string GetRole(this ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.Role)!;
}
