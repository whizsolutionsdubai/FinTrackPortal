# FinTrackPortal — Developer Guide

> How the codebase is organized, how to extend it, and where configuration lives. For a concise overview and endpoint list, see [README.md](../README.md).

---

## 1. Architecture Overview

**Visual wireframe** (kept in repo as `architecture-wireframe.png`):

![FinTrackPortal layered architecture](architecture-wireframe.png)

### Layer diagram (text)

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

**API-only infrastructure (no separate project):** `FinTrackPortal.API/Services/` — `IAttachmentStorageService` (`BlobStorageService` / `LocalFileStorageService`), **`IEmailSender`** (`MicrosoftGraphEmailSender`, `SmtpEmailSender`, `EmailSenderSelector`), registered in `Program.cs` from `appsettings`.

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
| `FinTrackPortal.API/` | Controllers, `Program.cs`, `Services/` (attachments, **email Graph/SMTP**, `IEmailSender` implementations) |
| `FinTrackPortal.Services/` | Service interfaces + implementations |
| `FinTrackPortal.Repositories/` | Dapper repositories |
| `FinTrackPortal.Interfaces/` | Repository interfaces |
| `FinTrackPortal.Models/` | Request/response DTOs |
| `FinTrackPortal.Common/` | `ApiResponse<T>`, `OperationResult<T>` |
| `Database/FinTrackDB_Schema.sql` | Full database script |
| `Docs/` | This guide, [`architecture-wireframe.png`](architecture-wireframe.png), [AppSettings](AppSettings.md), [Email setup](Email-Microsoft365-Setup.md) |
| `FinTrackPortal.postman_collection.json` | Postman (repo root) |
| `appsettings.Production.example.json` | Production config template (API project) |
| `appsettings.Development.example.json` | Local template — copy to `appsettings.Development.json` when that file is missing |

---

## 3. Configuration (development & production)

**Full key reference:** [AppSettings.md](AppSettings.md). **Microsoft 365 outbound mail:** [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md).

| File | Role |
|------|------|
| `appsettings.json` | Defaults; safe to commit without secrets |
| `appsettings.Development.json` | Local overrides (**gitignored** in this repo) |
| `appsettings.Development.example.json` | Committed template for Development |
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

### Email

`Email` binds to `AppEmailOptions`. **`Provider`** `MicrosoftGraph` uses Entra app registration + **`Mail.Send`** (application permission) and **`Graph:SenderMailbox`**. See [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md).

### Security Phase 2 (FinShare hardening spec)

After Phase 1 auth columns exist, run **`Database/FinTrackDB_Migration_Production_SecurityPhase2.sql`** (or use an updated **`FinTrackDB_Schema.sql`** baseline). This adds **`FailedLoginCount` / `LockoutUntil`**, **`AuditLogs`** + **`AuditLogs_Archive`**, lockout and audit stored procedures, **`sp_CleanupExpiredTokens`**, **`sp_GetUserEmailVerificationStatus`**, and **`sp_ResetLoginAttempts`** (alias for clearing lockout; PDF name). The API uses **`SecurityMaintenanceHostedService`**, **`IAuditLogRepository`**, ASP.NET **rate limiting** (login **5 / 15 min / IP** per *What To Do & Where*), **`UseForwardedHeaders`**, and **`Helpers/IpHelper`** for audit client IP. Roadmap index: [WhatToDoAndWhere.md](WhatToDoAndWhere.md). Spec sources: `Docs/Prompt/FinShare_SecurityHardening_V3.1.pdf`, `Docs/Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf`, master task list V4.

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
| POST | `/api/Auth/verify-email` |
| POST | `/api/Auth/forgot-password` |
| POST | `/api/Auth/reset-password` |

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
| POST | `/api/Expense/personal` |
| GET | `/api/Expense/personal` or `/api/Expense/personal/my` (`?category=` optional) |
| PUT | `/api/Expense/personal/edit` |
| POST | `/api/Expense/move` |
| POST | `/api/Expense/payer` |
| GET | `/api/Expense/{expenseId}/payers` |
| GET | `/api/Expense/accounts/{userId}` |
| POST | `/api/Expense/accounts` |
| DELETE | `/api/Expense/accounts/{accountId}` |
| GET | `/api/Expense/attachments/my` |
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
| Auth | Passwords use **BCrypt** in `UserRepository`; `sp_ValidateUser` returns `MemberId` + `PasswordHash` (match on **email** / `EmailAddress`). |
| Secrets | Keep production connection strings and JWT keys out of git; use host panel or GitHub Secrets. |

---

## 13. Related files on GitHub

| Document / file | Description |
|-----------------|-------------|
| [README.md](../README.md) | Overview, endpoints, configuration summary, Postman |
| [AppSettings.md](AppSettings.md) | All `appsettings` sections and environment layering |
| [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md) | Microsoft 365 / Graph email + Entra steps |
| [WhatToDoAndWhere.md](WhatToDoAndWhere.md) | PDF roadmap → repo files (`Docs/Prompt/…WhatToDoAndWhere.pdf`) |
| [architecture-wireframe.png](architecture-wireframe.png) | Layered architecture diagram (API, services, repos, SQL, Graph) |
| `Database/FinTrackDB_Schema.sql` | Authoritative schema |
| `appsettings.Production.example.json` | Production template |
| `appsettings.Development.example.json` | Development template |
| `FinTrackPortal.postman_collection.json` | API tests |
