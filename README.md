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

### Settlements (`api/Settlement`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Settlement/record` | Record a payment between members |
| GET | `/api/Settlement/group/{groupId}` | Get settlement history for a group |
| GET | `/api/Settlement/suggested/{groupId}` | Get suggested payments to clear all debts |

### Members (`api/Member`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/Member/create` | Create a member profile |
| PUT | `/api/Member/edit` | Edit member name |
| DELETE | `/api/Member/delete/{memberId}` | Soft-delete a member |

> All endpoints except Auth require a valid JWT Bearer token.

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
| `Expense` | Group or personal expense records |
| `ExpenseSplit` | Per-member share of each expense |
| `Settlement` | Debt settlements between members |

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
| `sp_AddExpense` / `sp_UpdateExpense` / `sp_DeleteExpense` | Expense CRUD |
| `sp_AddExpenseSplit` / `sp_DeleteExpenseSplits` | Manage expense splits |
| `sp_AddPersonalExpense` / `sp_GetPersonalExpenses` | Personal expense tracking |
| `sp_GetExpensesByGroup` | List group expenses |
| `sp_RecordSettlement` | Record a payment between members |
| `sp_GetSettlementsByGroup` | Get settlement history for a group |

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

Pre-built Postman collection and environment files are included in the repository root:

- `FinTrackPortal.postman_collection.json`
- `FinTrackPortal.postman_environment_dev.json`
- `FinTrackPortal.postman_environment_production.json`

Import them into Postman for quick API testing.

## CI/CD

The project uses **GitHub Actions** to automatically build and deploy on every push:

1. Restores dependencies
2. Builds in Release configuration
3. Publishes the API project
4. Deploys to SmarterASP.NET via FTP

Workflow file: `.github/workflows/main.yml`

## License

This project is for personal/educational use.
