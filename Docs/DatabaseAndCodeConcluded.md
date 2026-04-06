# Concluded database & code changes (Pending Tasks / Phase 3–5 alignment)

This document **freezes** the current **SQL/table** outcome and **application code** changes after the recent updates. Use it when reviewing deployments or onboarding.

---

## 1. Important: `FinTrackDB_Schema.sql` vs migrations

`Database/FinTrackDB_Schema.sql` is a **full rebuild** script and **does not** currently include objects added only in Phase 3–5 migrations:

- `dbo.Notifications`
- `dbo.MemberBankDetails`
- `dbo.GroupEvents`
- `Member.ProfilePhotoUrl` and related Phase 3–5 procedures

**Greenfield:** run `FinTrackDB_Schema.sql` **then** apply migrations **1 → 5** (see §3), **or** rely solely on migrations on an empty DB in order (as in README).

---

## 2. Concluded table: `dbo.Notifications`

**Final name:** `dbo.Notifications` (not `dbo.Notification` — reserved keyword conflict).

**Columns (aligned with `FinTrackDB_Schema.sql` entity style: `CreatedDate` / `ModifiedDate` / `IsActive`):**

| Column | Type | Notes |
|--------|------|--------|
| `NotificationId` | `bigint` IDENTITY PK | |
| `MemberId` | `bigint` NOT NULL | FK → `Member(MemberId)` |
| `Title` | `nvarchar(200)` NOT NULL | |
| `Body` | `nvarchar(2000)` NULL | |
| `NotificationType` | `nvarchar(50)` NULL | |
| `IsRead` | `bit` NOT NULL | default `0` |
| `LinkUrl` | `nvarchar(500)` NULL | |
| `CreatedDate` | `datetime2(7)` NOT NULL | default `GETUTCDATE()` |
| `ModifiedDate` | `datetime2(7)` NULL | set when marked read |
| `IsActive` | `bit` NOT NULL | default `1`; list filters `IsActive = 1` |

**Procedures (always use these names):**

- `sp_GetNotifications`
- `sp_MarkNotificationRead`
- `sp_CreateNotification`

**Removed / obsolete (do not use in new work):**

- Table name `dbo.Notification` (singular)
- Column name `CreatedAt` on this table only (replaced by `CreatedDate`)

---

## 3. Migration scripts — apply order (existing DB)

Back up first. Order matches `README.md`:

| Order | Script |
|-------|--------|
| 1 | `Database/FinTrackDB_Migration_Production_Phase3.sql` |
| 2 | `Database/FinTrackDB_Migration_Production_AuthEnhancements.sql` |
| 3 | `Database/FinTrackDB_Migration_Production_SecurityPhase2.sql` |
| 4 | `Database/FinTrackDB_Migration_UserRefreshTokens.sql` |
| 5 | `Database/FinTrackDB_Migration_NewFeatures_Phase3to5.sql` |
| 6 | `Database/FinTrackDB_Migration_Production_BackendChanges_v6.sql` |

**Optional (only if your DB predates the fix):**

| Script | When |
|--------|------|
| `Database/FinTrackDB_Migration_sp_ResetLoginAttempts.sql` | Phase 2 applied before this procedure existed |
| `Database/FinTrackDB_Migration_Production_GroupEvents_GroupIdGuard.sql` | Step 5 applied **before** `sp_UpdateGroupEvent` / `sp_DeleteGroupEvent` had `@GroupId` |
| `Database/FinTrackDB_Migration_Production_Notifications_RenameTable.sql` | Old table `dbo.Notification` **or** `dbo.Notifications` with `CreatedAt` and **without** `ModifiedDate` / `IsActive` |
| `Database/FinTrackDB_Migration_Production_TestAccounts_EmailVerified.sql` | One-time unblock for two known test emails (`Users.IsEmailVerified = 1`) |

---

## 4. Other Phase 3–5 objects (from step 5 script)

Created/updated in `FinTrackDB_Migration_NewFeatures_Phase3to5.sql` (not repeated here column-by-column):

- **`Member`:** `ProfilePhotoUrl`
- **`MemberBankDetails`:** encrypted IBAN storage + SPs (`sp_SaveBankDetails`, `sp_GetBankDetails`, etc.)
- **`GroupEvents`:** group calendar + SPs (`sp_GetGroupEvents`, `sp_CreateGroupEvent`, …); stricter **`@GroupId`** on update/delete when applicable
- **Transaction history:** SPs used by `ITransactionRepository` / `GET /api/Transaction/history`
- **User profile / password:** `sp_GetUserProfile`, `sp_UpdateUserProfile`, `sp_UpdateProfilePhoto`, `sp_GetUserAuthByMemberId`, `sp_UpdateUserPasswordByMemberId`

---

## 5. Application code — concluded changes

### Notifications

| Area | Concluded state |
|------|-----------------|
| Model | `FinTrackPortal.Models/Notifications/NotificationItem.cs` — **`CreatedDate`**, **`ModifiedDate`** (replaces **`CreatedAt`** on the API model for this resource). |
| Service | `INotificationService` / `NotificationService` — **`GetAsync`**, **`MarkReadAsync`**, **`MarkAllReadAsync`**, **`CreateAsync`**. |
| API JSON | Clients should use **`createdDate`** / **`modifiedDate`** (camelCase). **`createdAt`** for this resource is **obsolete**. |

### Nothing removed from the repo

No feature modules or controllers were **deleted** as part of these updates. Changes were **renames**, **schema alignment**, **documentation**, and **service surface** (`CreateAsync` on `INotificationService`).

### Optional cleanup (not done automatically)

- **Clients** (web/mobile) still expecting notification field **`createdAt`**: update to **`createdDate`** or add a compatibility mapping.
- **`UserController`** still injects **`IUserRepository`** for profile **photo URL** steps only; can be folded into **`IProfileService`** later for stricter layering (behavior unchanged).

---

## 6. Cross-reference

- PDF parity and module map: [`WhatToDoAndWhere.md`](WhatToDoAndWhere.md)
- Developer overview: [`DeveloperGuide.md`](DeveloperGuide.md)
