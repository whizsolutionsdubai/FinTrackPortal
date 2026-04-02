# FinTrackPortal

A RESTful Web API for tracking shared and personal expenses within groups. Built with ASP.NET Core 8, Dapper, and SQL Server.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 8 / ASP.NET Core Web API |
| Database | SQL Server |
| ORM | Dapper (stored procedures) |
| Authentication | JWT Bearer tokens |
| API Docs | Swagger / Swashbuckle |
| CI/CD | GitHub Actions → SmarterASP.NET (FTP) |

## Architecture

The solution follows a **layered architecture** with clear separation of concerns:

```
FinTrackPortal.sln
│
├── FinTrackPortal.API            # Controllers, middleware, Program.cs
├── FinTrackPortal.Services       # Business logic (service interfaces + implementations)
├── FinTrackPortal.Repositories   # Data access via Dapper + stored procedures
├── FinTrackPortal.Interfaces     # Repository contracts
├── FinTrackPortal.Models         # DTOs, request/response models, entities
└── FinTrackPortal.Common         # Shared wrappers (ApiResponse<T>, OperationResult<T>)
```

## API Endpoints

### Auth (`api/Auth`) -- No token required

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Auth/login` | Authenticate and receive a JWT |
| POST | `/api/Auth/register` | Register a new user (creates Member + User) |

### Groups (`api/Group`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Group/create` | Create a new group |
| POST | `/api/Group/add-member` | Add a member to a group |
| GET | `/api/Group/summary/{groupId}` | Get balance summary (paid, share, net per member) |
| GET | `/api/Group/my-groups` | List groups for the current user |
| GET | `/api/Group/{groupId}/members` | List members of a group with roles |

### Expenses (`api/Expense`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Expense/add` | Add a group expense with equal or custom split |
| PUT | `/api/Expense/edit` | Edit an existing group expense |
| DELETE | `/api/Expense/delete/{expenseId}` | Soft-delete an expense |
| GET | `/api/Expense/group/{groupId}` | Get expenses for a group |
| POST | `/api/Expense/personal` | Add a personal (non-group) expense |
| GET | `/api/Expense/personal` | Get personal expenses for logged-in user |
| POST | `/api/Expense/move` | Move an expense to a different group |
| GET | `/api/Expense/accounts/{userId}` | List account labels for a user |
| POST | `/api/Expense/accounts` | Create a new account label |
| DELETE | `/api/Expense/accounts/{accountId}` | Soft-delete an account label |
| POST | `/api/Expense/{expenseId}/attachment` | Upload a receipt/invoice (JPG, PNG, PDF) |
| GET | `/api/Expense/{expenseId}/attachments` | List attachments for an expense |
| DELETE | `/api/Expense/attachment/{attachmentId}` | Soft-delete an attachment |

### Settlements (`api/Settlement`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Settlement/record` | Record a payment between members |
| GET | `/api/Settlement/group/{groupId}` | Get settlement history for a group |
| GET | `/api/Settlement/suggested/{groupId}` | Get suggested payments to clear all debts |

### Subscriptions (`api/Subscription`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/Subscription/plans` | List available plans (public) |
| GET | `/api/Subscription/my` | Get current plan for logged-in user |
| GET | `/api/Subscription/user/{memberId}` | Get current plan for a specific member |
| POST | `/api/Subscription/activate` | Activate a plan after payment |
| POST | `/api/Subscription/cancel` | Cancel the logged-in user's subscription |

### Members (`api/Member`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Member/create` | Create a member profile |
| PUT | `/api/Member/edit` | Edit member name |
| DELETE | `/api/Member/delete/{memberId}` | Soft-delete a member |

> All endpoints except Auth and Subscription/plans require a valid JWT Bearer token.

## Database Schema

The full SQL Server schema (tables, constraints, and stored procedures) is included at:

```
Database/FinTrackDB_Schema.sql
```

### Tables

| Table | Description |
|-------|-------------|
| `Member` | Base member profile |
| `Users` | Login credentials linked to a Member |
| `Groups` | Expense-sharing groups |
| `GroupMember` | Many-to-many link between groups and members (with role) |
| `Expense` | Group or personal expense records (with optional AccountId, CostCenterId) |
| `ExpenseSplit` | Per-member share of each expense |
| `Settlement` | Debt settlements between members |
| `SubscriptionPlan` | Plan definitions (Free / Premium) with limits and pricing |
| `UserSubscription` | Each member's active subscription and billing cycle |
| `ExpenseAccount` | User-defined account labels (Personal, Flat, Customer, etc.) |
| `ExpenseAttachment` | Metadata for receipt/invoice files stored in Azure Blob Storage |
| `Organisation` | Corporate/B2B tier (foundation table — used later) |
| `CostCenter` | Cost centers within an Organisation (foundation — used later) |

### Stored Procedures

| Procedure | Purpose |
|-----------|---------|
| `sp_ValidateUser` | Authenticate by email + password hash |
| `sp_GetExpiry` | Check user account expiry |
| `sp_RegisterUser` | Register new user (Member + User in one transaction) |
| `sp_CreateMember` / `sp_EditMember` / `sp_DeleteMember` | Member CRUD |
| `sp_CreateGroup` | Create group + auto-add creator as Admin |
| `sp_AddMemberToGroup` | Add member to a group |
| `sp_GetMyGroups` / `sp_GetGroupMembers` / `sp_GetGroupSummary` | Group queries |
| `sp_IsMemberOfGroup` | Membership check |
| `sp_AddExpense` / `sp_UpdateExpense` / `sp_DeleteExpense` | Expense CRUD (now with optional @AccountId) |
| `sp_AddExpenseSplit` / `sp_DeleteExpenseSplits` | Manage expense splits |
| `sp_AddPersonalExpense` / `sp_GetPersonalExpenses` | Personal expense tracking |
| `sp_GetExpensesByGroup` | List group expenses |
| `sp_MoveExpense` | Move an expense to a different group |
| `sp_RecordSettlement` | Record a payment between members |
| `sp_GetSettlementsByGroup` | Get settlement history for a group |
| `sp_GetSubscriptionPlans` | List active subscription plans |
| `sp_GetUserSubscription` | Get member's current subscription |
| `sp_CreateUserSubscription` | Activate a subscription after payment |
| `sp_CancelUserSubscription` | Deactivate a member's subscription |
| `sp_CheckUserLimit` | Check group/member limits against plan |
| `sp_GetUserAccounts` / `sp_CreateAccount` / `sp_DeleteAccount` | Account label CRUD |
| `sp_AddExpenseAttachment` / `sp_GetExpenseAttachments` / `sp_DeleteExpenseAttachment` | Attachment CRUD |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ (local or remote)

### Setup

1. **Clone the repository**

```bash
git clone https://github.com/<your-username>/FinTrackPortal.git
cd FinTrackPortal
```

2. **Create the database** by running the schema script against your SQL Server:

```bash
sqlcmd -S localhost -i Database/FinTrackDB_Schema.sql
```

   Or open `Database/FinTrackDB_Schema.sql` in SSMS and execute it.

3. **Configure the database connection and JWT settings** in `FinTrackPortal.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<your-sql-server-connection-string>"
  },
  "JwtSettings": {
    "Key": "<your-secret-key>",
    "Issuer": "<issuer>",
    "Audience": "<audience>",
    "ExpiryMinutes": 60
  }
}
```

4. **Build and run**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project FinTrackPortal.API
   ```

5. **Open Swagger UI** at `https://localhost:<port>/swagger` to explore and test the API.

### Postman

Import from the repository root:

- `FinTrackPortal.postman_collection.json` — all API modules
- `FinTrackPortal.postman_environment_Local.json` — `baseUrl` for local dev (default `https://localhost:7124`)
- `FinTrackPortal.postman_environment_Production.json` — production `baseUrl`

In each environment, set **loginEmail** and **loginPassword**, select the environment in Postman, then run **Auth → Login** (saves the JWT to collection variables).

## CI/CD

The project uses **GitHub Actions** to automatically build and deploy on every push:

1. Restores dependencies
2. Builds in Release configuration
3. Publishes the API project
4. Deploys to SmarterASP.NET via FTP

Workflow file: `.github/workflows/main.yml`

## License

This project is for personal/educational use.
