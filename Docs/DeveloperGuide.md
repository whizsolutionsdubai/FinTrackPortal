# FinTrackPortal — Developer Guide

> This guide helps any developer understand, navigate, and extend the codebase.

---

## 1. Architecture Overview

![Architecture Wireframe](architecture-wireframe.png)

```
Client (Swagger / Postman / App)
        │
        ▼
┌─────────────────────────────────────────────────┐
│  API Layer  —  ASP.NET Core 8 Web API           │
│  JWT Auth Middleware                              │
│  Controllers: Auth, Group, Expense, Settlement,  │
│               Member                             │
└──────────────────────┬──────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────┐
│  Business Logic Layer  —  Services               │
│  UserService, GroupService, ExpenseService,       │
│  SettlementService, MemberService                │
│  (all return OperationResult<T>)                 │
└──────────────────────┬──────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────┐
│  Data Access Layer  —  Repositories (Dapper)     │
│  SqlConnection + Stored Procedures               │
│  No inline SQL — every query goes through an SP  │
└──────────────────────┬──────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────┐
│  Database  —  SQL Server (FinTrackDB)            │
│  7 Tables · 22 Stored Procedures                 │
└─────────────────────────────────────────────────┘
```

### Flow for every request

```
HTTP Request
  → Controller (validates input, extracts JWT claims)
    → Service (business rules, orchestration)
      → Repository (Dapper calls stored procedure)
        → SQL Server (executes SP, returns data)
      ← OperationResult<T>
    ← OperationResult<T>
  ← ApiResponse<T> (JSON envelope)
```

---

## 2. Project Structure

| Project | Purpose |
|---------|---------|
| `FinTrackPortal.API` | Controllers, Program.cs, JWT middleware, Extensions |
| `FinTrackPortal.Services` | Service interfaces + implementations (business logic) |
| `FinTrackPortal.Repositories` | Dapper data-access implementations |
| `FinTrackPortal.Interfaces` | Repository contracts (for DI/testability) |
| `FinTrackPortal.Models` | DTOs, request/response models, entity POCOs |
| `FinTrackPortal.Common` | `ApiResponse<T>` and `OperationResult<T>` wrappers |
| `Database/` | Full SQL schema script (tables + stored procedures) |
| `Docs/` | This guide + architecture wireframe |

---

## 3. How to Read the XML Documentation

Every class, interface, and method in the codebase has `/// <summary>` XML doc comments. Here's how to use them:

### 3.1 In Your IDE (Visual Studio / Cursor / VS Code)

Hover over any class or method name — the XML summary appears as a tooltip:

```csharp
// Hover over IUserRepository.RegisterAsync to see:
// "Register a new user via sp_RegisterUser.
//  Creates a Member row and a User row inside a single SQL transaction.
//  Returns the new MemberId."
```

Press **F12** (Go to Definition) on any service/repository to jump to the interface and read what each method does.

### 3.2 In Swagger UI

After building, open `/swagger` in the browser. You'll see:

- **Endpoint descriptions** pulled from controller `<summary>` tags
- **Model descriptions** pulled from request/response class `<summary>` tags
- **Property descriptions** from data annotations

This works because we enabled XML doc generation in every `.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

And configured Swagger to load them in `Program.cs`:

```csharp
var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
```

### 3.3 The Generated XML Files

After building, XML files appear in the `bin/Debug/net8.0/` folder:

```
FinTrackPortal.API.xml
FinTrackPortal.Models.xml
FinTrackPortal.Common.xml
FinTrackPortal.Interfaces.xml
FinTrackPortal.Services.xml
FinTrackPortal.Repositories.xml
```

These are auto-generated on every build. **Do not** commit them — they're in the `bin/` folder which is already in `.gitignore`.

---

## 4. Key Patterns to Follow

### 4.1 Adding a New Feature (step-by-step)

Follow this exact order when adding any new module:

| Step | What to do | Where |
|------|-----------|-------|
| 1 | Create the **stored procedure(s)** | `Database/FinTrackDB_Schema.sql` + run in SSMS |
| 2 | Create **request/response models** | `FinTrackPortal.Models/` |
| 3 | Add method(s) to the **repository interface** | `FinTrackPortal.Interfaces/` |
| 4 | Implement in the **repository** (Dapper + SP) | `FinTrackPortal.Repositories/` |
| 5 | Add method(s) to the **service interface** | `FinTrackPortal.Services/IXxxService.cs` |
| 6 | Implement in the **service** | `FinTrackPortal.Services/XxxService.cs` |
| 7 | Create the **controller** with endpoints | `FinTrackPortal.API/Controllers/` |
| 8 | Register **DI** in Program.cs | `builder.Services.AddScoped<>()` |
| 9 | Add **XML doc comments** to all new code | All files above |

### 4.2 Repository Pattern (Dapper)

Every repository follows the same structure:

```csharp
public class XxxRepository : IXxxRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<XxxRepository> _logger;

    public XxxRepository(IConfiguration config, ILogger<XxxRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    // Creates a new SqlConnection each time (disposed after use)
    private IDbConnection Connection =>
        new SqlConnection(_config.GetConnectionString("DefaultConnection"));
}
```

**Three query patterns:**

```csharp
// 1. Return a single value (e.g. new ID)
var id = await conn.QuerySingleAsync<long>(
    "sp_YourProcedure",
    new { Param1 = value1 },
    commandType: CommandType.StoredProcedure);
return OperationResult<long>.Success(id);

// 2. Return a list
var list = (await conn.QueryAsync<YourModel>(
    "sp_YourProcedure",
    new { Param1 = value1 },
    commandType: CommandType.StoredProcedure)).ToList();
return OperationResult<List<YourModel>>.Success(list);

// 3. Execute (no return value)
await conn.ExecuteAsync(
    "sp_YourProcedure",
    new { Param1 = value1 },
    commandType: CommandType.StoredProcedure);
return OperationResult<bool>.Success(true);
```

**Multi-step with transaction:**

```csharp
using var conn = Connection;
conn.Open();
using var tx = conn.BeginTransaction();

await conn.ExecuteAsync("sp_Step1", new { ... },
    transaction: tx, commandType: CommandType.StoredProcedure);
await conn.ExecuteAsync("sp_Step2", new { ... },
    transaction: tx, commandType: CommandType.StoredProcedure);

tx.Commit();
```

**Error handling — every method:**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error doing X for {Id}", id);
    return OperationResult<T>.Failure(ex.Message);
}
```

### 4.3 Controller Pattern

```csharp
// Validation
if (!ModelState.IsValid)
{
    var errors = ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage).ToList();
    return BadRequest(ApiResponse<object?>.ErrorResponse("Validation failed", errors));
}

// Get caller identity from JWT
var memberId = User.GetMemberId();
var email = User.GetEmail();

// Call service
var result = await _service.DoSomethingAsync(...);

// Return standardised response
if (!result.IsSuccess)
    return BadRequest(ApiResponse<object?>.ErrorResponse("Failed", result.ErrorMessage!));

return Ok(ApiResponse<object>.SuccessResponse(new { id = result.Data }, "Done"));
```

### 4.4 Response Envelope

Every endpoint returns this JSON shape:

```json
{
  "success": true,
  "message": "Human-readable message",
  "data": { },
  "errors": null
}
```

---

## 5. Database Conventions

- **Soft deletes**: All tables use `IsActive` (bit). Delete = set to 0, never physically remove rows.
- **Audit columns**: Every table has `CreatedBy`, `ModifiedBy`, `CreatedDate`, `ModifiedDate`.
- **Stored procedures**: Named `sp_VerbNoun` (e.g. `sp_CreateGroup`, `sp_GetExpensesByGroup`).
- **No inline SQL**: Every query goes through a named stored procedure.
- **Transactions**: Multi-step operations use either SQL-level (`BEGIN TRANSACTION` inside the SP) or C#-level (`conn.BeginTransaction()`) transactions.

---

## 6. Authentication

- **JWT Bearer tokens** with claims: `Email` + `MemberId`
- Tokens are issued by `AuthController.Login`
- Settings come from `appsettings.json` → `JwtSettings` section
- All controllers except `AuthController` have `[Authorize]`
- Claims are extracted via `ClaimsPrincipalExtensions.GetMemberId()` and `.GetEmail()`

---

## 7. Quick Reference — All API Endpoints

### Auth (no token required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Auth/login` | Login → returns JWT |
| POST | `/api/Auth/register` | Register → creates Member + User |

### Group

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Group/create` | Create group (auto Admin) |
| POST | `/api/Group/add-member` | Add member to group |
| GET | `/api/Group/my-groups` | List my groups |
| GET | `/api/Group/summary/{groupId}` | Balance summary |
| GET | `/api/Group/{groupId}/members` | List group members |

### Expense

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Expense/add` | Add group expense |
| PUT | `/api/Expense/edit` | Edit group expense |
| DELETE | `/api/Expense/delete/{id}` | Soft-delete expense |
| GET | `/api/Expense/group/{groupId}` | List group expenses |
| POST | `/api/Expense/personal` | Add personal expense |
| GET | `/api/Expense/personal` | List personal expenses |

### Settlement

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Settlement/record` | Record payment |
| GET | `/api/Settlement/group/{groupId}` | Payment history |
| GET | `/api/Settlement/suggested/{groupId}` | Suggested payments |

### Member

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Member/create` | Create member |
| PUT | `/api/Member/edit` | Edit member name |
| DELETE | `/api/Member/delete/{id}` | Soft-delete member |

---

## 8. CI/CD

GitHub Actions workflow (`.github/workflows/main.yml`):

1. Checkout → Setup .NET 8 → Restore → Build (Release) → Publish
2. Deploy to SmarterASP.NET via FTP

Triggers on every push to any branch.

---

## 9. Pending Items

| Item | Status | Notes |
|------|--------|-------|
| Password hashing (BCrypt) | Pending | `HashPassword()` currently returns plain text. Replace with `BCrypt.Net.BCrypt.HashPassword()` before production. See section 5.1 of the FinShare Developer Reference. |
| Email uniqueness | Done | `sp_RegisterUser` blocks duplicate emails |
| Settlement suggested | Done | Calculated in C# — greedy debtor/creditor algorithm |
