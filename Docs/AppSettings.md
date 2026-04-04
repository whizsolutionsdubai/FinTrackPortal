# Application settings (`appsettings`)

This document describes every configuration section used by **FinTrackPortal.API**, how ASP.NET Core merges files by environment, and where secrets should live.

---

## 1. How configuration is layered

ASP.NET Core loads settings in this order (later sources override earlier ones):

1. **`appsettings.json`** — shared defaults; commit to source control **without** real secrets.
2. **`appsettings.{Environment}.json`** — `Development` or `Production` based on `ASPNETCORE_ENVIRONMENT`.
3. **Environment variables** — use `__` (double underscore) for nested keys, e.g. `Email__Graph__ClientSecret`.
4. **User Secrets** (Development only) — see [Email — Microsoft 365 → §3](Email-Microsoft365-Setup.md#3-application-side-configuration).

| File | Typical use | In repository? |
|------|-------------|------------------|
| `appsettings.json` | Defaults, structure, placeholders | Yes |
| `appsettings.Development.json` | Local SQL, JWT dev key, email tests | Often **gitignored** (this repo ignores it) |
| `appsettings.Development.example.json` | Template to copy → `appsettings.Development.json` | Yes |
| `appsettings.Production.json` | Host paths, production connection string | **Gitignored** — create on server |
| `appsettings.Production.example.json` | Production template | Yes |

If `appsettings.Development.json` or `appsettings.Production.json` is missing after clone, copy the matching **`.example`** file and fill in values.

**GitHub push protection:** Never commit real **`Email:Graph:ClientSecret`**, database passwords, or JWT keys. This repo **gitignores** `appsettings.Development.json` and `appsettings.Production.json` so they stay local. If either file was ever committed, remove it from history (e.g. `git filter-branch` / `git filter-repo`) and **rotate** any exposed Entra app secret or password.

---

## 2. `ConnectionStrings`

| Key | Description |
|-----|-------------|
| `DefaultConnection` | SQL Server connection string for FinTrackDB (Dapper + stored procedures). |

Use `Encrypt=True` (and appropriate trust settings) in production. Do not commit production passwords; use environment-specific files or hosting configuration.

---

## 3. `JwtSettings`

| Key | Description |
|-----|-------------|
| `Key` | Symmetric signing key for JWT. Must be non-empty; use a long random string (at least 32 characters) in production. |
| `Issuer` | Token issuer claim (e.g. `FinShare`). |
| `Audience` | Token audience claim (e.g. `FinShareUsers`). |
| `ExpiryMinutes` | Access token (JWT) lifetime in minutes. |
| `RefreshTokenDays` | Opaque refresh token lifetime (stored server-side; issued as **httpOnly** cookie `finshare_refresh` on login / refresh). Default `14`. |

Configured in `Program.cs` with `Configure<JwtSettings>`. SPA clients calling **`/api/Auth/refresh`** must send **`credentials: 'include'`** (or equivalent) so the cookie is included.


---

## 4. `AttachmentStorage`

| Key | Description |
|-----|-------------|
| `Provider` | `Azure` — store blobs in Azure Storage. `Local` — store files on disk under `LocalStorage:Path`. |

---

## 5. `AzureStorage` (when `AttachmentStorage:Provider` is `Azure`)

| Key | Description |
|-----|-------------|
| `ConnectionString` | Azure Storage account connection string. |
| `ContainerName` | Blob container name (default `attachments`). |

---

## 6. `LocalStorage` (when `AttachmentStorage:Provider` is `Local`)

| Key | Description |
|-----|-------------|
| `Path` | Relative to the API content root or an absolute path (e.g. hosting provider folder). |
| `PublicBaseUrl` | Base URL where clients download files (must end with `/attachments` if you use the default static file mapping). |

The API exposes uploaded files under `/attachments` when using local storage.

---

## 7. `Email`

Transactional email (registration verification, password reset). Bound to `AppEmailOptions` in code.

### Common keys

| Key | Description |
|-----|-------------|
| `Enabled` | If `false`, outbound email is skipped (actions are logged). Useful for local work without Graph/SMTP. |
| `Provider` | `MicrosoftGraph` (Microsoft 365 / Microsoft Graph API) or `Smtp` (legacy SMTP relay). |
| `FromName` | Display name shown to recipients. |
| `FromEmail` | Address used in templates / branding; for Graph the **sending mailbox** is `Graph:SenderMailbox`. |
| `AppPublicUrl` | Public site base URL **without** trailing slash — used to build verify/reset links in emails. |

### `Email:Graph` (required when `Provider` is `MicrosoftGraph`)

| Key | Description |
|-----|-------------|
| `TenantId` | Microsoft Entra **Directory (tenant) ID** (GUID). |
| `ClientId` | App registration **Application (client) ID**. |
| `ClientSecret` | Client secret **value** (treat as a secret — User Secrets, Key Vault, or env vars). |
| `SenderMailbox` | UPN or object ID of the **licensed mailbox** that sends mail (e.g. `noreply@contoso.com`). |

Full Azure setup: [Email — Microsoft 365 setup](Email-Microsoft365-Setup.md).

### SMTP-only keys (when `Provider` is `Smtp`)

| Key | Description |
|-----|-------------|
| `SmtpHost`, `SmtpPort`, `UseSsl` | SMTP server endpoint. |
| `SmtpUser`, `SmtpPassword` | Credentials if the relay requires authentication. |

---

## 8. `Logging`

Standard ASP.NET Core `Logging:LogLevel` hierarchy. Typical production setting: `Default` = `Information`, `Microsoft.AspNetCore` = `Warning`.

---

## 9. `AllowedHosts`

Host filtering for the Kestrel pipeline. `*` allows any host header; restrict in production if you use fixed domains.

---

## 10. Quick reference — three environments

| Concern | Base `appsettings.json` | Development | Production |
|---------|-------------------------|-------------|------------|
| Intent | Placeholders, `Email.Enabled` often `false` | Local DB, local URLs, optional real Graph for testing | Real connection string, public URLs, secrets via host/env |
| Secrets | Never | User Secrets + optional `Development.json` (gitignored) | `Production.json` (gitignored) or portal env vars |
| Template | — | `appsettings.Development.example.json` | `appsettings.Production.example.json` |

---

## Related documentation

- [Docs README](README.md) — index of all files under `Docs/`.
- [Email — Microsoft 365 (Graph) setup](Email-Microsoft365-Setup.md) — Entra app registration, permissions, and app configuration steps.
- [Developer Guide](DeveloperGuide.md) — architecture and configuration overview; diagram: [architecture-wireframe.png](architecture-wireframe.png).
- [What to do & where](WhatToDoAndWhere.md) — roadmap vs. repo locations.

**Rate limiting** (login, register, forgot-password, resend-verification, refresh) and **account lockout** are configured in **`Program.cs`**, not in `appsettings`. See [README.md](../README.md) (Authentication section).
