# What to do & where (FinShare roadmap)

This repo maps the checklist in **`Docs/Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf`** to concrete locations. Use the PDF as the full phase list; this file is a **quick index**. For all documentation in `Docs/`, see **[Docs/README.md](README.md)**. **Status snapshot:** [`FinShare_ForAbhilash_TrueStatus_v5.pdf`](Prompt/FinShare_ForAbhilash_TrueStatus_v5.pdf).

## Where the PDFs live

- [`Docs/Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf`](Prompt/FinShare_ForAbhilash_WhatToDoAndWhere.pdf)
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

## Phase 3–5

Profile, transactions, notifications, bank details, group events — **not in this index**; see the PDF and phase-specific `.docx` references listed there.

## Related docs

- [DeveloperGuide.md](DeveloperGuide.md)  
- [AppSettings.md](AppSettings.md)  
- [Email-Microsoft365-Setup.md](Email-Microsoft365-Setup.md)  
