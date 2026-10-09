# Employee Leave Management System

ASP.NET Core Web API (.NET 10) + Entity Framework Core + SQL Server 2022, with a small HTML/CSS/JS frontend served from `wwwroot`.

## Run
1. Install .NET 10 SDK and SQL Server 2022 (or SQL Express / LocalDB).
2. Edit `ConnectionStrings:DefaultConnection` in `appsettings.json` if your server is not `localhost` with Windows auth.
   SQL login example: `Server=localhost;Database=EmployeeLeaveDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;`
3. Change `Jwt:Key` to your own secret (32+ characters).
4. In VS Code terminal:
   ```
   dotnet restore
   dotnet run
   ```
5. Open http://localhost:5080. The database, tables and demo data are created on first run.

## Demo logins
| Role | Username | Password |
|---|---|---|
| Admin | admin | Admin@123 |
| Manager | manager | Manager@123 |
| Employee | gowri / rahul | Employee@123 |

## Features
- JWT login with BCrypt password hashing; roles: Admin, Manager, Employee
- Employees: add / update / delete / search / activate-deactivate, department, designation, manager
- Departments and leave types (policies) managed by Admin
- Apply leave (weekends excluded, overlap check, balance check), cancel pending, history, balance
- Manager approves/rejects only own team's requests; Admin can decide any; nobody can decide their own
- Approval runs in a DB transaction and deducts balance; notifications + audit log
- Admin dashboard and reports (department-wise, monthly)

## API (all under /api, JWT Bearer except login)
- `POST auth/login`, `GET auth/me`
- `GET/POST employee`, `GET/PUT/DELETE employee/{id}`, `PATCH employee/{id}/status?active=`, `GET employee/me`, `GET employee/team`
- `GET/POST department`, `PUT/DELETE department/{id}`
- `GET leave/types`, `POST leave/apply`, `GET leave/my`, `GET leave/balance`, `POST leave/{id}/cancel`
- `GET leave/pending`, `GET leave/team`, `POST leave/{id}/approve|reject`, `GET leave/notifications`
- `GET admin/dashboard`, `GET admin/reports`, `GET admin/users`, `PUT admin/users/{id}/role`, `POST/PUT admin/leave-types`
- OpenAPI JSON in Development: `/openapi/v1.json`

## Switch to EF migrations (optional, good for interviews)
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```
Then in `Program.cs` replace `db.Database.EnsureCreated();` with `db.Database.Migrate();`.
Drop the existing database first if it was created by `EnsureCreated`.

## Structure
Controllers (HTTP) -> Services (business rules) -> ApplicationDbContext (EF Core) -> SQL Server.
DTOs separate API contracts from entities; `ExceptionMiddleware` maps exceptions to 400/403/404/500 JSON.
