namespace EmployeeLeaveManagement.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                KeyNotFoundException => (404, ex.Message),
                InvalidOperationException => (400, ex.Message),
                UnauthorizedAccessException => (403, ex.Message),
                _ => (500, "An unexpected error occurred.")
            };
            if (status == 500) logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = status;
            await ctx.Response.WriteAsJsonAsync(new { message });
        }
    }
}
