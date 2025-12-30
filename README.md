# OurWealth - Budget and expense tracking, built for couples.

## Repository Structure
```
ourwealth/
├── OurWealth.Api/      # ASP.NET Core API
├── frontend/           # React/Next.js frontend
└── README.md           # This file
```

## Our Wealth API

The RESTful API for OurWealth, built with ASP.NET Core and PostgreSQL. 

OurWealth enables couples to collaboratively manage finances, track expenses, split bills, monitor debts, and achieve savings goals together.

---

### Features

- **Two-user household management** - Secure, isolated data per household
- **Expense tracking** - Categorized spending with history
- **Budget management** - Set monthly budgets by category or overall
- **Income tracking** - Monitor earnings by source and user
- **Recurring bills** - Manage subscriptions and regular payments
- **Debt tracking** - Credit cards and loans with automatic balance updates
- **Savings goals** - Set targets and track progress
- **Expense splitting** - Track settlements between household members 
- **Dashboard analytics** - Comprehensive financial overview in a single endpoint
- **Advanced reporting** - Monthly summaries, category breakdowns, and month comparisons
- **Smart filtering** - Search expenses by date range, amount, category, or description
- **Pagination** - Efficient handling of large expense datasets
- **Upcoming bills tracking** - View unpaid bills due in the next X days
- **JWT authentication** - Secure token-based auth with BCrypt password hashing

### Technology Stack

- **Framework:** ASP.NET Core 10.0 (C#)
- **Database:** PostgreSQL 18
- **ORM:** Entity Framework Core
- **Authentication:** JWT with BCrypt password hashing

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 18
- Git

### Installation & Setup

#### 1. Clone the Repository

```bash
git clone https://github.com/DanPearce/ourwealth.git
cd ourwealth
cd OurWealth.Api
```

#### 2. Configure Database Connection

Update `appsettings.json` with your PostgreSQL credentials - use the below for local development:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ourwealth;Username=your_username;Password=your_password"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-at-least-32-characters-long",
    "Issuer": "OurWealth",
    "Audience": "OurWealth",
    "ExpirationDays": 7
  }
}
```

#### 3. Create Database

```bash
# Start PostgreSQL service
brew services start postgresql@18

# Create database
createdb ourwealth
```

#### 4. Run Migrations

```bash
dotnet ef database update
```

#### 5. Run the API

```bash
dotnet run
```

The API will be available at `http://localhost:5000`

### Authentication

#### Register & Login

All endpoints except `/api/auth/register` and `/api/auth/login` require JWT authentication.

**Register Endpoint:** `POST /api/auth/register`  
Creates a new user account and automatically creates or assigns them to a household.

**Login Endpoint:** `POST /api/auth/login`  
Authenticates user and returns a JWT token.

**Using the Token:**  
Include the JWT token in the `Authorization` header for all protected endpoints:
```
Authorization: Bearer <your_jwt_token>
```

### API Endpoints

All endpoints except `/api/auth/*` require JWT authentication via the `Authorization: Bearer <token>` header.

#### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create new user account |
| POST | `/api/auth/login` | Authenticate and receive JWT token |

#### Expenses

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/expenses` | List all household expenses with filtering & pagination | `page`, `pageSize`, `startDate`, `endDate`, `minAmount`, `maxAmount`, `search`, `categoryId`, `paidById` |
| GET | `/api/expenses/{id}` | Get single expense details | - |
| POST | `/api/expenses` | Create new expense | - |
| PUT | `/api/expenses/{id}` | Update expense | - |
| DELETE | `/api/expenses/{id}` | Soft delete expense | - |

#### Categories

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/categories` | List all household categories | `isActive` |
| GET | `/api/categories/{id}` | Get single category with subcategories | - |
| POST | `/api/categories` | Create new category | - |
| PUT | `/api/categories/{id}` | Update category | - |
| DELETE | `/api/categories/{id}` | Soft delete category | - |

#### Budgets

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/budgets` | List household budgets | `month`, `year` |
| GET | `/api/budgets/{id}` | Get single budget | - |
| POST | `/api/budgets` | Create budget | - |
| PUT | `/api/budgets/{id}` | Update budget | - |
| DELETE | `/api/budgets/{id}` | Delete budget | - |

#### Income

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/income` | List household income | `month`, `year`, `userId` |
| GET | `/api/income/{id}` | Get single income record | - |
| POST | `/api/income` | Create income entry | - |
| PUT | `/api/income/{id}` | Update income | - |
| DELETE | `/api/income/{id}` | Delete income | - |

#### Recurring Bills

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/recurringbills` | List household recurring bills | `isActive` |
| GET | `/api/recurringbills/{id}` | Get single bill with payment history | - |
| GET | `/api/recurringbills/upcoming` | Get unpaid bills due in next X days | `days` |
| POST | `/api/recurringbills` | Create recurring bill | - |
| PUT | `/api/recurringbills/{id}` | Update bill | - |
| DELETE | `/api/recurringbills/{id}` | Soft delete bill | - |

#### Bill Payments

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/billpayments` | List all bill payments | `recurringBillId`, `month`, `year` |
| GET | `/api/billpayments/{id}` | Get single payment | - |
| POST | `/api/billpayments` | Record payment | - |
| PUT | `/api/billpayments/{id}` | Update payment | - |
| DELETE | `/api/billpayments/{id}` | Delete payment | - |

#### Debts

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/debts` | List household debts | `isActive` |
| GET | `/api/debts/{id}` | Get single debt with payment history | - |
| POST | `/api/debts` | Create debt | - |
| PUT | `/api/debts/{id}` | Update debt | - |
| DELETE | `/api/debts/{id}` | Soft delete debt | - |

#### Debt Payments

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/debtpayments` | List all debt payments | `debtId` |
| GET | `/api/debtpayments/{id}` | Get single payment | - |
| POST | `/api/debtpayments` | Record payment | - |
| PUT | `/api/debtpayments/{id}` | Update payment | - |
| DELETE | `/api/debtpayments/{id}` | Delete payment | - |

#### Savings Goals

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/savingsgoals` | List household savings goals | `isActive` |
| GET | `/api/savingsgoals/{id}` | Get single goal with contributions | - |
| POST | `/api/savingsgoals` | Create savings goal | - |
| PUT | `/api/savingsgoals/{id}` | Update goal | - |
| DELETE | `/api/savingsgoals/{id}` | Soft delete goal | - |

#### Savings Contributions

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/savingscontributions` | List all contributions | `savingsGoalId` |
| POST | `/api/savingscontributions` | Add contribution (auto-updates goal balance) | - |
| DELETE | `/api/savingscontributions/{id}` | Delete contribution | - |

#### Settlements

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/settlements` | List all household settlements | - |
| GET | `/api/settlements/{id}` | Get single settlement | - |
| GET | `/api/settlements/balance` | Calculate net balance | - |
| POST | `/api/settlements` | Record who owes whom | - |
| PUT | `/api/settlements/{id}` | Update settlement | - |
| DELETE | `/api/settlements/{id}` | Delete settlement | - |

#### Dashboard

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/dashboard` | Get comprehensive financial overview | `month`, `year` |

#### Reports

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/reports/monthly-summary` | Complete monthly financial summary | `month`, `year` |
| GET | `/api/reports/spending-by-category` | Category spending breakdown | `month`, `year` |
| GET | `/api/reports/budget-comparison` | Budget vs actual per category | `month`, `year` |
| GET | `/api/reports/month-comparison` | Compare two months side-by-side | `month1`, `year1`, `month2`, `year2` |

#### Users

| Method | Endpoint | Description | Query Params |
|--------|----------|-------------|--------------|
| GET | `/api/users/profile` | Get current user profile with household info | - |
| GET | `/api/users/household-members` | List all household members | - |

### Database Schema

#### Entity Relationships

```
Households (1) ←→ (Many) Users
Households (1) ←→ (Many) Categories
Households (1) ←→ (Many) Expenses
Households (1) ←→ (Many) Budgets
Households (1) ←→ (Many) Income
Households (1) ←→ (Many) RecurringBills
Households (1) ←→ (Many) Debts
Households (1) ←→ (Many) SavingsGoals
Households (1) ←→ (Many) Settlements

Users (1) ←→ (Many) Expenses (as PaidByUser)
Users (1) ←→ (Many) Income
Users (1) ←→ (Many) BillPayments (as PaidByUser)
Users (1) ←→ (Many) DebtPayments (as PaidBy)
Users (1) ←→ (Many) Settlements (as FromUser or ToUser)

Categories (1) ←→ (Many) Expenses
Categories (1) ←→ (Many) Budgets
Categories (1) ←→ (Many) RecurringBills
Categories (1) ←→ (Many) Categories (parent/child hierarchy)

RecurringBills (1) ←→ (Many) BillPayments
Debts (1) ←→ (Many) DebtPayments
SavingsGoals (1) ←→ (Many) SavingsContributions
```

#### Core Tables

**Users**
- Id (PK)
- Username (unique)
- Email (unique)
- PasswordHash (BCrypt)
- DisplayName
- HouseholdId (FK)
- CreatedAt, UpdatedAt

**Households**
- Id (PK)
- Name
- CreatedAt, UpdatedAt

**Categories**
- Id (PK)
- HouseholdId (FK)
- Name
- ParentCategoryId (FK, nullable)
- Priority (Essential/Important/Optional)
- Icon, Color
- IsActive
- CreatedAt

**Expenses**
- Id (PK)
- HouseholdId (FK)
- CategoryId (FK)
- Amount, Date, Description
- PaidByUserId (FK)
- IsActive
- CreatedAt

**Budgets**
- Id (PK)
- HouseholdId (FK)
- Month (1-12), Year
- CategoryId (FK, nullable)
- Amount, Date

**Income**
- Id (PK)
- UserId (FK), HouseholdId (FK)
- Month (1-12), Year
- Amount, Source
- RecievedDate, CreatedAt

**RecurringBills**
- Id (PK)
- HouseholdId (FK), CategoryId (FK)
- Description, Amount
- IsVariableAmount, Frequency
- DayOfMonth, DueDate
- ReminderDaysBefore
- IsActive, PaidByUserId (FK)
- Notes, CreatedAt

**BillPayments**
- Id (PK)
- RecurringBillId (FK)
- Month (1-12), Year
- Amount, PaidDate
- PaidByUserId (FK)
- Notes, CreatedAt

**Debts**
- Id (PK)
- HouseholdId (FK)
- Name, DebtType
- OriginalAmount, CurrentBalance (auto-updated)
- InterestRate, MinimumPayment
- PaymentDayOfMonth, Creditor
- IsActive, Notes
- CreatedAt, UpdatedAt

**DebtPayments**
- Id (PK)
- DebtId (FK)
- Amount, PaymentDate
- PaidById (FK)
- Notes, CreatedAt

**SavingsGoals**
- Id (PK)
- HouseholdId (FK)
- Name
- TargetAmount, CurrentAmount (auto-updated)
- TargetDate, Priority
- IsActive, Notes
- CreatedAt, UpdatedAt

**SavingsContributions**
- Id (PK)
- SavingsGoalId (FK)
- Amount, ContributionDate
- Notes, CreatedAt

**Settlements**
- Id (PK)
- HouseholdId (FK)
- FromUserId (FK), ToUserId (FK)
- Amount, SettlementDate
- Notes, CreatedAt

### Security Features

#### Authentication & Authorization
- **JWT Tokens:** Token-based authentication
- **BCrypt Hashing:** Secure password storage
- **Token Expiration:** Configurable token lifetime
- **Protected Endpoints:** All endpoints except `/api/auth/*` require authentication

#### Data Isolation
- **Household-Level Security:** Users can only access data from their own household
- **Query Filtering:** All database queries automatically filter by `HouseholdId`
- **User Verification:** FromUserId/ToUserId validated against household membership
- **Cross-Household Prevention:** Cannot create records linking to other households

#### Input Validation
- **DTO Pattern:** Separate Data Transfer Objects prevent over-posting attacks
- **Model Validation:** Required fields, data types, and constraints enforced
- **Business Rules:** e.g., Cannot settle with yourself, both users must be in household
- **SQL Injection Protection:** Entity Framework parameterized queries

### Key Design Patterns

#### Automatic Balance Tracking

**DebtPaymentsController:**
```csharp
// POST: Subtracts payment from debt balance
debt.CurrentBalance -= request.Amount;

// PUT: Adjusts balance (restore old, apply new)
debt.CurrentBalance += payment.Amount;  // Restore old
debt.CurrentBalance -= request.Amount;  // Apply new

// DELETE: Adds payment back to balance
debt.CurrentBalance += payment.Amount;
```

**SavingsContributionsController:**
```csharp
// POST: Adds contribution to goal
goal.CurrentAmount += request.Amount;

// DELETE: Subtracts contribution from goal
goal.CurrentAmount -= contribution.Amount;
```

**SettlementsController:**
```csharp
// GET /api/settlements/balance: Calculates net
var owedToMe = settlements.Where(s => s.ToUserId == userId).Sum(s => s.Amount);
var iOwe = settlements.Where(s => s.FromUserId == userId).Sum(s => s.Amount);
var netBalance = owedToMe - iOwe;
```

#### Soft vs. Hard Deletes

**Soft Delete (Preserves History):**
- Categories
- Recurring Bills
- Debts
- Savings Goals

**Hard Delete (Complete Removal):**
- Expenses
- Budgets
- Income
- Bill Payments
- Debt Payments
- Savings Contributions
- Settlements

### Advanced Features

#### Pagination

The Expenses endpoint supports pagination to efficiently handle large datasets:
```csharp
GET /api/expenses?page=1&pageSize=50
// Returns: { items: [...], totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }
```

#### Advanced Filtering

Expenses can be filtered by multiple criteria simultaneously:
```csharp
GET /api/expenses?startDate=2024-01-01&endDate=2024-12-31&minAmount=50&maxAmount=500&search=tesco&categoryId=2
```

#### Dashboard Aggregation

The Dashboard endpoint returns comprehensive financial data in a single API call:
- Total income and expenses for the month
- Expenses grouped by category
- Budget progress with remaining amounts
- Active debts with balances
- Savings goals with progress
- Upcoming unpaid bills
- Recent expense transactions
- Settlement balance between household members

#### Monthly Reports

The Reports controller provides analytics endpoints:
- **Monthly Summary:** Complete income/expense breakdown with category details
- **Spending by Category:** Simplified data for pie charts and visualizations
- **Budget Comparison:** Budget vs actual spending per category
- **Month Comparison:** Side-by-side analysis of two months with trend indicators

#### Upcoming Bills

Track bills that need to be paid:
```csharp
GET /api/recurringbills/upcoming?days=30
// Returns bills due in the next 30 days that haven't been paid yet
```

### Testing

All endpoints have been thoroughly tested using curl commands with JWT authentication. The API supports manual testing via tools like:
- curl (command line)
- Postman
- Insomnia
- Any HTTP client

Testing includes verification of:
- JWT authentication flow
- Household-level data isolation
- Automatic balance updates (debts, savings, settlements)
- Soft delete data preservation
- Query parameter filtering
- Error handling and validation

### Project Structure

```
OurWealth.Api/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── ExpensesController.cs
│   ├── CategoriesController.cs
│   ├── BudgetsController.cs
│   ├── IncomeController.cs
│   ├── RecurringBillsController.cs
│   ├── BillPaymentsController.cs
│   ├── DebtsController.cs
│   ├── DebtPaymentsController.cs
│   ├── SavingsGoalsController.cs
│   ├── SavingsContributionsController.cs
│   ├── SettlementsController.cs
│   ├── DashboardController.cs
│   ├── ReportsController.cs
│   └── UsersController.cs
├── Models/              # Entity models
│   ├── User.cs
│   ├── Household.cs
│   ├── Category.cs
│   ├── Expense.cs
│   ├── Budget.cs
│   ├── Income.cs
│   ├── RecurringBill.cs
│   ├── BillPayment.cs
│   ├── Debt.cs
│   ├── DebtPayment.cs
│   ├── SavingsGoal.cs
│   ├── SavingsContribution.cs
│   └── Settlement.cs
├── Data/
│   └── AppDbContext.cs  # EF Core context
├── Migrations/          # Database migrations
├── appsettings.json     # Configuration
└── Program.cs           # App startup
```

### Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [JWT.io](https://jwt.io/) - JWT token debugger