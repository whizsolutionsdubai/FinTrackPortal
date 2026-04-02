# FinTrackPortal — Developer Guide

> How the codebase is organized, how to extend it, and where configuration lives. For a concise overview and endpoint list, see [README.md](../README.md).

---

## 1. Architecture Overview

```
Client (Swagger / Postman / Mobile / Web)
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│  API Layer — ASP.NET Core 8 Web API                         │
│  JWT middleware · Static files (/attachments when Local)     │
│  Controllers: Auth, Group, Expense, Settlement, Member,   │
│               Subscription                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Services — IUserService, IGroupService, IExpenseService,   │
│  ISettlementService, IMemberService, ISubscriptionService   │
│  (return OperationResult<T>)                                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Repositories — Dapper + stored procedures only             │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  SQL Server — FinTrackDB (see Database/FinTrackDB_Schema.sql) │
└─────────────────────────────────────────────────────────────┘
```

**API-only services (no separate project):** `IAttachmentStorageService` with `BlobStorageService` (Azure) or `LocalFileStorageService` (disk), registered from `AttachmentStorage:Provider` in `Program.cs`.

### Request flow

```
HTTP Request
  → Controller (validation, JWT claims)
    → Service
      → Repository (Dapper + SP)
        → SQL Server
      ← OperationResult<T>
    ← OperationResult<T>
  ← ApiResponse<T> (JSON)
```

---

## 2. Project structure

| Path | Purpose |
|------|---------|
| `FinTrackPortal.API/` | Controllers, `Program.cs`, `Services/` (`BlobStorageService`, `LocalFileStorageService`, `IAttachmentStorageService`) |
| `FinTrackPortal.Services/` | Service interfaces + implementations |
| `FinTrackPortal.Repositories/` | Dapper repositories |
| `FinTrackPortal.Interfaces/` | Repository interfaces |
| `FinTrackPortal.Models/` | Request/response DTOs |
| `FinTrackPortal.Common/` | `ApiResponse<T>`, `OperationResult<T>` |
| `Database/FinTrackDB_Schema.sql` | Full database script |
| `Docs/` | This guide + wireframe |
| `FinTrackPortal.postman_collection.json` | Postman (repo root) |
| `appsettings.Production.example.json` | Production config template (API project) |

---

## 3. Configuration (development & production)

| File | Role |
|------|------|
| `appsettings.json` | Defaults; safe to commit without secrets |
| `appsettings.Development.json` | Local overrides (often gitignored) |
| `appsettings.Production.json` | Production overrides (**gitignored**) |
| `appsettings.Production.example.json` | **Committed** template — copy to `appsettings.Production.json` on the server |

### Attachment storage

| `AttachmentStorage:Provider` | Implementation | Required settings |
|-----------------------------|----------------|-------------------|
| `Azure` | `BlobStorageService` | `AzureStorage:ConnectionString`, `ContainerName` |
| `Local` | `LocalFileStorageService` | `LocalStorage:Path`, `LocalStorage:PublicBaseUrl` |

For **Local**, `Program.cs` registers static files at `/attachments` mapped to the resolved physical folder. Upload endpoint expects multipart form field **`file`**.

`LocalStorage:Path` can be relative to `IWebHostEnvironment.ContentRootPath` or an absolute Windows path (common on SmarterASP).

### JWT

Loaded from `JwtSettings` in `Program.cs`. Signing key must be non-empty; use a long random secret in production.

---

## 4. XML documentation & Swagger

XML comments are generated per project (`GenerateDocumentationFile`). Swagger loads `FinTrackPortal.API.xml` and `FinTrackPortal.Models.xml` from the output directory.

Do not commit `bin/**` XML files; they are build artifacts.

---

## 5. Adding a feature (checklist)

| Step | Action |
|------|--------|
| 1 | Add or alter tables / SPs in `Database/FinTrackDB_Schema.sql`, deploy to your database |
| 2 | Add models in `FinTrackPortal.Models` |
| 3 | Extend repository interface + implementation |
| 4 | Extend service interface + implementation |
| 5 | Add controller actions + `[Authorize]` / `[AllowAnonymous]` as needed |
| 6 | Register DI in `Program.cs` |
| 7 | XML-doc new public APIs |

---

## 6. Repository pattern (Dapper)

- One `SqlConnection` per operation (or explicit transaction when needed).
- `CommandType.StoredProcedure` only — no ad hoc SQL in repositories.
- Wrap failures in `OperationResult<T>.Failure(message)` and log exceptions.

---

## 7. Controller pattern

- Validate `ModelState`, return `ApiResponse` errors as lists when needed.
- Use `User.GetMemberId()` and `User.GetEmail()` from `ClaimsPrincipalExtensions`.
- Return `ApiResponse<T>` for success and consistent error shape.

---

## 8. Database conventions

- Soft deletes: `IsActive = 0` where applicable.
- Audit fields: `CreatedBy`, `ModifiedBy`, dates.
- Subscriptions and group limits: `GroupController` calls `ISubscriptionService.IsActionAllowedAsync` before create group / add member.

---

## 9. Authentication

- JWT with claims: email + `MemberId`.
- **No JWT:** `AuthController` (login/register), `GET /api/Subscription/plans`, Swagger.
- `SubscriptionController` is `[Authorize]` except `[AllowAnonymous]` on `plans`.

---

## 10. API quick reference

### Auth (no token)

| Method | Route |
|--------|-------|
| POST | `/api/Auth/login` |
| POST | `/api/Auth/register` |

### Subscription

| Method | Route |
|--------|-------|
| GET | `/api/Subscription/plans` (public) |
| GET | `/api/Subscription/my` |
| GET | `/api/Subscription/user/{memberId}` |
| POST | `/api/Subscription/activate` |
| POST | `/api/Subscription/cancel` |

### Group

| Method | Route |
|--------|-------|
| POST | `/api/Group/create` |
| POST | `/api/Group/add-member` |
| GET | `/api/Group/my-groups` |
| GET | `/api/Group/summary/{groupId}` |
| GET | `/api/Group/{groupId}/members` |

### Expense

| Method | Route |
|--------|-------|
| POST | `/api/Expense/add` |
| PUT | `/api/Expense/edit` |
| DELETE | `/api/Expense/delete/{expenseId}` |
| GET | `/api/Expense/group/{groupId}` |
| POST / GET | `/api/Expense/personal` |
| POST | `/api/Expense/move` |
| GET | `/api/Expense/accounts/{userId}` |
| POST | `/api/Expense/accounts` |
| DELETE | `/api/Expense/accounts/{accountId}` |
| POST | `/api/Expense/{expenseId}/attachment` |
| GET | `/api/Expense/{expenseId}/attachments` |
| DELETE | `/api/Expense/attachment/{attachmentId}` |

### Settlement

| Method | Route |
|--------|-------|
| POST | `/api/Settlement/record` |
| GET | `/api/Settlement/group/{groupId}` |
| GET | `/api/Settlement/suggested/{groupId}` |

### Member

| Method | Route |
|--------|-------|
| POST | `/api/Member/create` |
| PUT | `/api/Member/edit` |
| DELETE | `/api/Member/delete/{memberId}` |

---

## 11. CI/CD

`.github/workflows/main.yml`: checkout, .NET 8, restore, build Release, publish `FinTrackPortal.API`, FTP deploy to SmarterASP (adjust secrets in repo settings).

---

## 12. Known improvements

| Item | Notes |
|------|--------|
| Password hashing | Ensure production uses strong hashing (e.g. BCrypt) for `PasswordHash`; align with `sp_ValidateUser` expectations. |
| Secrets | Keep production connection strings and JWT keys out of git; use host panel or GitHub Secrets. |

---

## 13. Related files on GitHub

| Document / file | Description |
|-----------------|-------------|
| [README.md](../README.md) | Overview, endpoints, configuration summary, Postman |
| `Database/FinTrackDB_Schema.sql` | Authoritative schema |
| `appsettings.Production.example.json` | Production template |
| `FinTrackPortal.postman_collection.json` | API tests |
