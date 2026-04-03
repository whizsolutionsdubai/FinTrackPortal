# Email — Microsoft 365 (Microsoft Graph)

FinTrackPortal sends transactional mail (email verification, password reset) using the **Microsoft Graph** `sendMail` API with **OAuth 2.0 client credentials** (no SMTP). This matches how Microsoft 365 is commonly locked down for automated sending.

---

## 1. Prerequisites

- A **Microsoft 365** tenant with a mailbox that will send mail (user mailbox or shared mailbox), licensed as required by your tenant.
- **Global Administrator** or another role that can **grant admin consent** for application permissions on Microsoft Graph.

---

## 2. Azure / Microsoft Entra — step by step

Use the [Microsoft Entra admin center](https://entra.microsoft.com) (or Azure Portal → **Microsoft Entra ID**).

### Step A — Collect your Tenant ID

1. Open **Microsoft Entra ID** → **Overview**.
2. Copy **Tenant ID** (a GUID).
3. This value maps to **`Email:Graph:TenantId`** in configuration.

### Step B — Register an application

1. Go to **App registrations** → **New registration**.
2. **Name:** e.g. `FinTrackPortal Mail`.
3. **Supported account types:** *Accounts in this organizational directory only* (typical for a single-tenant API).
4. Click **Register**.
5. On the app **Overview** page, copy **Application (client) ID** → **`Email:Graph:ClientId`**.

### Step C — Create a client secret

1. Open **Certificates & secrets** → **Client secrets** → **New client secret**.
2. Choose a description and expiry; click **Add**.
3. Copy the **Value** immediately (it is shown only once) → **`Email:Graph:ClientSecret`**.
4. Store this only in a secure place (User Secrets, Key Vault, hosting app settings). **Do not commit** to Git.

### Step D — API permissions (Microsoft Graph)

1. Open **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions**.
2. Add **`Mail.Send`**.
3. Click **Grant admin consent for [your organization]** and confirm.

Without **admin consent**, token acquisition may succeed but **send mail will fail** with permission errors.

> **Note:** `Mail.Send` as an **application** permission allows the app to send as **any** user in the tenant that you specify in code. Your app is configured to send only as **`Email:Graph:SenderMailbox`**.

### Step E — Choose the sending mailbox

- Use the **User principal name (UPN)** of the mailbox, e.g. `noreply@yourdomain.com` or `yourname@tenant.onmicrosoft.com`.
- Put that UPN in **`Email:Graph:SenderMailbox`**.
- The mailbox must exist in Microsoft 365 and be able to send mail normally.

No extra “assign user to app” step is required for this **client credentials** flow; consent is at the tenant level for the application permission.

---

## 3. Application-side configuration

### 3.1 Configuration keys

In `appsettings` (or environment variables), under the **`Email`** section:

| Setting | Value |
|---------|--------|
| `Enabled` | `true` when you want the API to send mail. |
| `Provider` | `MicrosoftGraph` |
| `FromName` | Display name (e.g. `FinShare`). |
| `FromEmail` | Usually align with your product/domain (used in templates; actual sender is the Graph mailbox). |
| `AppPublicUrl` | Your public site URL **without** trailing slash (used in verification/reset links). |
| `Graph:TenantId` | From §2 Step A. |
| `Graph:ClientId` | From §2 Step B. |
| `Graph:ClientSecret` | From §2 Step C (secret, not secret **ID**). |
| `Graph:SenderMailbox` | From §2 Step E. |

### 3.2 Per environment

- **`appsettings.json`** — structure and placeholders; keep `Enabled` false if you do not want accidental sends from generic environments.
- **`appsettings.Development.json`** — local overrides (this repo **gitignores** this file). Copy from `appsettings.Development.example.json` if needed.
- **`appsettings.Production.json`** — production values on the server (also **gitignored**). Use `appsettings.Production.example.json` as a template.

### 3.3 User Secrets (recommended for local development)

From the API project directory:

```bash
cd FinTrackPortal.API
dotnet user-secrets set "Email:Graph:ClientSecret" "YOUR_SECRET_VALUE_HERE"
```

Ensure the API `.csproj` contains a `UserSecretsId` (this solution includes one). User Secrets apply when `ASPNETCORE_ENVIRONMENT` is `Development`.

### 3.4 Environment variables (e.g. Azure App Service, containers)

Nested keys use double underscores:

```text
Email__Enabled=true
Email__Provider=MicrosoftGraph
Email__Graph__TenantId=...
Email__Graph__ClientId=...
Email__Graph__ClientSecret=...
Email__Graph__SenderMailbox=noreply@yourdomain.com
```

---

## 4. Optional: SMTP instead of Graph

If you must use a relay that speaks SMTP, set:

- `Email:Provider` = `Smtp`
- Fill `SmtpHost`, `SmtpPort`, `UseSsl`, `SmtpUser`, `SmtpPassword`, `FromEmail`

Graph is the recommended path for Microsoft 365 when SMTP is disabled or unreliable.

---

## 5. Verification checklist

| Check | |
|-------|---|
| Tenant ID, Client ID, Client Secret are correct | Secret is the **value**, not the secret ID from the portal. |
| Admin consent granted | **API permissions** shows green “Granted for …” for `Mail.Send`. |
| `SenderMailbox` | Exact UPN of an existing mailbox in that tenant. |
| `AppPublicUrl` | Matches the URL users open in the browser (HTTPS in production). |
| `Email:Enabled` | `true` when testing sends. |

If sending fails, check API logs for Graph or authentication errors. Common issues: missing admin consent, wrong tenant, expired client secret, or invalid `SenderMailbox`.

---

## 6. Further reading

- [Application configuration reference](AppSettings.md) — all `appsettings` sections.
- Microsoft Learn: [Get access without a user](https://learn.microsoft.com/en-us/graph/auth-v2-service-principal) (client credentials).
