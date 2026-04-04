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
│  JWT middleware · Rate limiting · Forwarded headers        │
│  Static files (/attachments when Local)                     │
│  Controllers: Auth, Group, Expense, Settlement, Member,     │
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

**API-only infrastructure (no separate project):** `FinTrackPortal.API/Services/` — `IAttachmentStorageService` (`BlobStorageService` / `LocalFileStorageService`), **`IEmailSender`** (`MicrosoftGraphEmailSender`, `SmtpEmailSender`, `EmailSenderSelector`); **`SecurityMaintenanceHostedService`** (token + audit retention); **`Helpers/IpHelper`**. **`IRefreshTokenRepository`** / **`IAuditLogRepository`** live in **Interfaces** + **Repositories** (registered in `Program.cs`).

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
| `FinTrackPortal.API/` | Controllers, `Program.cs`, `Services/`, `Helpers/` (`IpHelper`) |
| `FinTrackPortal.Services/` | Service interfaces + implementations |
| `FinTrackPortal.Repositories/` | Dapper repositories (`UserRepository`, `RefreshTokenRepository`, `AuditLogRepository`, …) |
| `FinTrackPortal.Interfaces/` | Repository interfaces (`IUserRepository`, `IRefreshTokenRepository`, `IAuditLogRepository`, …) |
| `FinTrackPortal.Models/` | Request/response DTOs |
| `FinTrackPortal.Common/` | `ApiResponse<T>`, `OperationResult<T>` |
| `Database/FinTrackDB_Schema.sql` | Full database script |
| `Docs/` | [README.md](README.md) (index), this guide, [AppSettings](AppSettings.md), [Email setup](Email-Microsoft365-Setup.md), [WhatToDoAndWhere](WhatToDoAndWhere.md), images |
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

### JWT and refresh tokens

Loaded from `JwtSettings` in `Program.cs`. Signing key must be non-empty; use a long random secret in production. **`ExpiryMinutes`** controls the **access token** (Bearer) lifetime. **`RefreshTokenDays`** controls the **opaque refresh token** stored in **`UserRefreshTokens`** and issued as httpOnly cookie **`finshare_refresh`** (see `AuthController`). **`POST /api/Auth/refresh`** rotates the refresh token and returns a new JWT; **`POST /api/Auth/revoke`** logs out. Password reset calls **`sp_RevokeAllUserRefreshTokens`**.

### Email

`Email` binds to `AppEmailOptions`. **`Provider`** `MicrosoftGraph` uses Entra app registration + **`Mail.Send`** (application permission) and **`Graph:SenderMailbox`**. See [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md).

### Security Phase 2 (FinShare hardening spec)

After Phase 1 auth columns exist, run **`Database/FinTrackDB_Migration_Production_SecurityPhase2.sql`** (or use an updated **`FinTrackDB_Schema.sql`** baseline). This adds **`FailedLoginCount` / `LockoutUntil`**, **`AuditLogs`** + **`AuditLogs_Archive`**, lockout and audit stored procedures, **`sp_CleanupExpiredTokens`**, **`sp_GetUserEmailVerificationStatus`**, and **`sp_ResetLoginAttempts`** (alias for clearing lockout; PDF name). The API uses **`SecurityMaintenanceHostedService`**, **`IAuditLogRepository`**, ASP.NET **rate limiting** (login **5 / 15 min / IP** per *What To Do & Where*), **`UseForwardedHeaders`**, and **`Helpers/IpHelper`** for audit client IP. **Refresh tokens:** `UserRefreshTokens` table, `POST /api/Auth/refresh` and `/revoke`, httpOnly cookie `finshare_refresh` (see `AuthController`). Migration: `Database/FinTrackDB_Migration_UserRefreshTokens.sql`. Roadmap: [WhatToDoAndWhere.md](WhatToDoAndWhere.md). Spec sources: `FinShare_SecurityHardening_V3.1.pdf`, `FinShare_ForAbhilash_WhatToDoAndWhere.pdf`, `FinShare_ForAbhilash_TrueStatus_v5.pdf`.

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

- **Access token:** JWT with claims **email** + **`MemberId`** (Bearer).
- **Refresh token:** Opaque value in httpOnly cookie **`finshare_refresh`**; not a JWT. Validated via **`sp_ValidateRefreshToken`**; rotation on **`POST /api/Auth/refresh`**.
- **No access JWT required:** all **`AuthController`** actions (login, register, verify-email, resend-verification, forgot/reset password, **refresh**, **revoke**), `GET /api/Subscription/plans`, Swagger.
- Other controllers require **`[Authorize]`** and a valid Bearer access token.
- `SubscriptionController` is `[Authorize]` except `[AllowAnonymous]` on `plans`.

---

## 10. API quick reference

### Auth (no Bearer token)

| Method | Route |
|--------|-------|
| POST | `/api/Auth/login` |
| POST | `/api/Auth/refresh` |
| POST | `/api/Auth/revoke` |
| POST | `/api/Auth/register` |
| POST | `/api/Auth/verify-email` |
| POST | `/api/Auth/resend-verification` |
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
| Auth | Passwords use **BCrypt** in `UserRepository`; `sp_ValidateUser` returns `MemberId` + `PasswordHash` (match on **email** / `EmailAddress`). **Refresh tokens** implemented (`UserRefreshTokens`, `/refresh`, `/revoke`). **WebAuthn** and Phase 3+ features (profile, notifications, …) still open per FinShare specs — see [WhatToDoAndWhere.md](WhatToDoAndWhere.md). |
| Secrets | Keep production connection strings, JWT keys, and Graph client secrets out of git; use host panel, User Secrets, or GitHub Secrets. |
| DB migrations | Order: Phase3 → AuthEnhancements → SecurityPhase2 → **UserRefreshTokens** — see [README.md](../README.md#database-schema). |

---

## 13. Related files on GitHub

| Document / file | Description |
|-----------------|-------------|
| [README.md](../README.md) | Overview, endpoints, configuration summary, Postman, **migration order** |
| [Docs/README.md](README.md) | Index of all documentation in `Docs/` |
| [AppSettings.md](AppSettings.md) | All `appsettings` sections and environment layering |
| [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md) | Microsoft 365 / Graph email + Entra steps |
| [WhatToDoAndWhere.md](WhatToDoAndWhere.md) | PDF roadmap → repo files (`Docs/Prompt/…WhatToDoAndWhere.pdf`) |
| [architecture-wireframe.png](architecture-wireframe.png) | Layered architecture diagram (API, services, repos, SQL, Graph) |
| `Database/FinTrackDB_Schema.sql` | Authoritative schema (greenfield) |
| `Database/FinTrackDB_Migration_UserRefreshTokens.sql` | Refresh tokens + `sp_CleanupExpiredTokens` extension |
| `Database/FinTrackDB_Migration_Production_SecurityPhase2.sql` | Audit, lockout, token cleanup |
| `appsettings.Production.example.json` | Production template |
| `appsettings.Development.example.json` | Development template |
| `FinTrackPortal.postman_collection.json` | API tests |
