# What to do & where (FinShare roadmap)

This repo maps the checklist in **`Docs/Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf`** to concrete locations. Use the PDF as the full phase list; this file is a **quick index**. For all documentation in `Docs/`, see **[Docs/README.md](README.md)**. **Prioritized pending backlog + reference documents:** [`FinShare_ForAbhilash_PendingTasks.pdf`](Prompt/FinShare_ForAbhilash_PendingTasks.pdf). **Status snapshot:** [`FinShare_ForAbhilash_TrueStatus_v5.pdf`](Prompt/FinShare_ForAbhilash_TrueStatus_v5.pdf).

## Where the PDFs live

- [`Docs/Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf`](Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf)
- [`Docs/Prompt/FinShare_ForAbhilash_PendingTasks.pdf`](Prompt/FinShare_ForAbhilash_PendingTasks.pdf)
- [`Docs/Prompt/FinShare_ForAbhilash_TrueStatus_v5.pdf`](Prompt/FinShare_ForAbhilash_TrueStatus_v5.pdf)

## Phase 1 — Authentication (this solution)

| PDF / doc idea | In this repo |
|----------------|--------------|
| `PasswordValidator` | `FinTrackPortal.Services/PasswordValidator.cs` (static validate; used by `UserService`) |
| Email (verification + reset) | `FinTrackPortal.API/Services/*Email*` + `IEmailSender`; `AppEmailOptions` in `FinTrackPortal.Models` |
| `AuthController` register / login / verify / resend / forgot / reset / **refresh / revoke** | `FinTrackPortal.API/Controllers/AuthController.cs` |
| `Program.cs` DI | `FinTrackPortal.API/Program.cs` |
| Users email columns + SPs | `Database/FinTrackDB_Schema.sql` + `Database/FinTrackDB_Migration_Production_AuthEnhancements.sql` |
| **Naming note:** PDF uses `LoginAttempts`; DB uses **`FailedLoginCount`** with the same behaviour. **`sp_ResetLoginAttempts`** now wraps **`sp_ClearFailedLogins`**. |
| WebAuthn + Fido2 + 4 endpoints | **Not implemented** — start after Phase 1 auth is stable (per PDF). |

## Phase 2 — Security hardening

| PDF / doc idea | In this repo |
|----------------|--------------|
| `UseForwardedHeaders` + rate limiting | `FinTrackPortal.API/Program.cs` |
| **Login rate limit** | **5 requests per IP per 15 minutes** (`login` policy), per WhatToDo PDF |
| Register / forgot+resend limits | 3/hour/IP (`register`), 3/15 min/IP (`forgotpw`) |
| **`IpHelper.GetClientIP(HttpContext)`** | `FinTrackPortal.API/Helpers/IpHelper.cs` — used by `AuthController` for audit IP |
| Audit logs + `sp_WriteAuditLog` | `IAuditLogRepository` / `AuditLogRepository`; tables in `FinTrackDB_Schema.sql` + `FinTrackDB_Migration_Production_SecurityPhase2.sql` |
| Token cleanup + audit retention | `SecurityMaintenanceHostedService` |
| Refresh tokens, `/refresh`, `/revoke`, httpOnly cookie | **`UserRefreshTokens`** + SPs + `IRefreshTokenRepository`; cookie name **`finshare_refresh`**; run **`Database/FinTrackDB_Migration_UserRefreshTokens.sql`** (updates **`sp_CleanupExpiredTokens`**) |

## Phase 2A — Refresh tokens (Pending Tasks §2A)

| Spec idea | In this repo |
|-----------|----------------|
| Refresh table + `/refresh` + `/revoke` + httpOnly cookie | **Done** — `UserRefreshTokens` (not the PDF’s name `RefreshTokens`), `IRefreshTokenRepository`, `AuthController`, cookie **`finshare_refresh`**; migrations in `FinTrackDB_Schema.sql` / `FinTrackDB_Migration_UserRefreshTokens.sql`. The Pending Tasks PDF still describes this as “next”; treat the PDF as **roadmap** and **True Status** / this table as **current**. |

## Phase 3–5 — Implementation status (vs [`FinShare_ForAbhilash_PendingTasks.pdf`](Prompt/FinShare_ForAbhilash_PendingTasks.pdf))

The PDF is an **April 2025** backlog; this table reflects the **current repo**. PDF order after 2A: **3A → 3B → 3C → 3D → 4 → 5 → 2B**.

| PDF section | Status | Notes |
|-------------|--------|--------|
| **2A** Refresh tokens | **Done** | See **Phase 2A** section above. |
| **3A** Profile + photo | **Done** | `UserController`, **`FinTrackDB_Migration_NewFeatures_Phase3to5.sql`**. |
| **3B** Change password | **Done** | `PUT /api/User/change-password`; SPs `sp_GetUserAuthByMemberId` / `sp_UpdateUserPasswordByMemberId` (PDF may say `sp_ChangePassword` — same behaviour). |
| **3C** Transaction history | **Done** | One endpoint returns **history + summary** (`GET /api/Transaction/history`); PDF mentioned two endpoints — **combined**. |
| **3D** Notifications | **Done** | List + mark one + **mark all** + `sp_CreateNotification` + controller triggers (`Expense`, `Settlement`, `Group`) are wired. |
| **4** Bank details | **Done** | Requires **`Encryption`** in config for production IBAN. |
| **5** Group events | **Done** | `GroupEventsController` → `/api/Group/{groupId}/events`. DB adds **`@GroupId`** on update/delete (stricter than PDF); use **`FinTrackDB_Migration_Production_GroupEvents_GroupIdGuard.sql`** if you deployed an older step 5. |
| **2B** WebAuthn | **Not done** | Optional per PDF. |

### `FinShare_ForAbhilash_PendingTasks.pdf` — naming & design differences

Equivalent behaviour, different names or shapes (not necessarily wrong).

| PDF / spec | This repository |
|------------|-----------------|
| Table **`RefreshTokens`**, `UserId` **int** | **`UserRefreshTokens`**, **`MemberId`** **bigint** |
| Cookie **`refreshToken`** | **`finshare_refresh`** |
| **`RefreshTokenService.cs`**, token **SHA-256 hashed** in DB | **`IRefreshTokenRepository`** + **`AuthController`**; DB stores **opaque token** for lookup (*PDF hash-at-rest is stricter; optional future hardening*). |
| **`POST /api/Auth/revoke`** with **`[Authorize]`** | **`revoke`** is **anonymous** (uses refresh **cookie** only) so logout works **without** a valid access JWT |
| **`POST /api/Auth/logout`** | Not separate — **`/api/Auth/revoke`** is the refresh logout |
| Profile columns all on **`Members`** | **Name** on **`Member`**, **email/phone** on **`Users`** (`sp_GetUserProfile` joins both) |
| **Two** transaction endpoints | **One** response with items + summary |
| **`GroupEventsController`** naming | Same HTTP routes; class is **`GroupEventsController`** |
| Settlement **Payer/Payee** | DB **`FromMemberId` / `ToMemberId`** |
| **`sp_UpdateGroupEvent` / Delete** (PDF params) | Includes **`@GroupId`** so URL group matches the event |
| Table name **`Notification`** (PDF / singular) | **`dbo.Notifications`** — singular **`Notification`** is a **SQL Server reserved keyword**; columns follow **`FinTrackDB_Schema.sql`** style: **`CreatedDate`**, **`ModifiedDate`**, **`IsActive`** (not `CreatedAt` alone). Use `FinTrackDB_Migration_Production_Notifications_RenameTable.sql` to upgrade older DBs. |

### Still incomplete vs PDF

1. **WebAuthn (2B)** — not implemented.  
2. **Refresh tokens** — optional: store **hash only** in DB per PDF.  
3. **Frontend** — group events: switch from **localStorage** to API (per PDF).  

### All [`FinShare_ForAbhilash_PendingTasks.pdf`](Prompt/FinShare_ForAbhilash_PendingTasks.pdf) modules — flow, layers & naming

Use this table to see **where each PDF item lives** and whether the **API → service → repository** chain is intact. *Naming differences from the PDF are intentional unless noted in the “Naming & design differences” table above.*

| PDF track | What the PDF expects | Primary API | Application service(s) | Data access |
|-----------|----------------------|-------------|-------------------------|-------------|
| **Phase 1** — core auth | Register, login, verify email, forgot/reset password | `AuthController` | `IUserService` | `UserRepository` via `IUserRepository` |
| **Phase 2** — security | Rate limits, audit, IP helper | `Program.cs` policies; `AuthController` | — | `IAuditLogRepository` / `AuditLogRepository` |
| **2A** — refresh | `/refresh`, `/revoke`, httpOnly cookie | `AuthController` (`/refresh`, `/revoke`) | `IUserService` (validation, JWT) | `IRefreshTokenRepository` (also used from `AuthController` and `ProfileService` for revoke-on-password-change) |
| **3A** — profile + photo | Profile GET/PUT, photo upload | `UserController` | `IProfileService`, `IProfilePhotoService` | `IUserRepository` inside those services; **photo path also calls `IUserRepository` from `UserController`** for `GetUserProfileAsync` / `UpdateProfilePhotoUrlAsync` (thin orchestration) |
| **3B** — change password | Logged-in password change | `UserController` | `IProfileService.ChangePasswordAsync` | `IUserRepository` + refresh revoke via `IRefreshTokenRepository` in `ProfileService` |
| **3C** — transactions | Personal expense + settlement history (+ summary) | `TransactionController` | `ITransactionService` | `ITransactionRepository` |
| **3D** — notifications | List, mark read, mark all, create trigger notifications | `NotificationController` | `INotificationService` (`GetAsync`, `MarkReadAsync`, `MarkAllReadAsync`, `CreateAsync`) | `INotificationRepository` → `dbo.Notifications`, SPs `sp_GetNotifications` / `sp_MarkNotificationRead` / `sp_MarkAllNotificationsRead` / `sp_CreateNotification` |
| **4** — bank / IBAN | Masked bank row; settlement payee decrypt | `UserController`; `SettlementController` (`…/payee-bank-details`) | `IBankDetailsService` | `IBankDetailsRepository` → `MemberBankDetails` |
| **5** — group events | CRUD under group | `GroupEventsController` | `IGroupEventService` | `IGroupEventRepository` → `GroupEvents` + SPs |
| **2B** — WebAuthn | FIDO2 / credential endpoints | *Not in repo* | — | — |

**Clean architecture (this solution):**

- **Default rule:** controllers depend on **`*Service`** interfaces in `FinTrackPortal.Services`; services depend on **`*Repository`** interfaces in `FinTrackPortal.Interfaces`; implementations live in `FinTrackPortal.Repositories`. **`Program.cs`** is the composition root (wires interfaces to types).
- **Intentional exceptions:**  
  - **`AuthController`** uses **`IRefreshTokenRepository`** and **`IAuditLogRepository`** directly (token rotation + audit next to HTTP concerns). Behaviour matches the PDF; layering is pragmatic, not a broken flow.  
  - **`UserController`** uses **`IUserRepository`** only for **profile photo** URL read/update after upload (could be moved behind `IProfileService` later without changing routes).
- **Flows not “lost”:** Each implemented row still runs end-to-end (HTTP → service/repository → SQL SP / table). Items marked **Partial** or **Not done** above are **feature gaps vs the PDF** (auto-notifications, WebAuthn, optional hash-only refresh, client localStorage), not missing wiring inside a completed module.

Reference **`.docx`** files in Pending Tasks; this repo’s **`Prompt/`** also has PDFs (e.g. **`FinShare_ForAbhilash_MasterPendingTasks_V4.pdf`**) if Word sources are missing.

## Related docs

- [DeveloperGuide.md](DeveloperGuide.md)  
- [AppSettings.md](AppSettings.md)  
- [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md)  
