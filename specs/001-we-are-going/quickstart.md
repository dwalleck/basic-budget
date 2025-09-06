# Basic Budget Quickstart Guide

## Prerequisites
*Implementation Note: This quickstart assumes the implementation following the hexagonal architecture structure defined in [plan.md](./plan.md#project-structure). All GraphQL operations reference the schema defined in [contracts/graphql-schema.graphql](./contracts/graphql-schema.graphql)*
- .NET 10 SDK installed
- Docker Desktop (for .NET Aspire PostgreSQL containers)
- .NET Aspire 9.4+ workload: `dotnet workload install aspire`
- Basic understanding of GraphQL

## Setup and Installation

### 1. Clone and Build
```bash
git clone <repository-url>
cd basic-budget
dotnet restore
dotnet build
```

### 2. Start with .NET Aspire (Recommended)
```bash
# Start the entire application stack with Aspire
dotnet run --project src/BasicBudget.AppHost

# This automatically:
# - Starts PostgreSQL container with persistent data
# - Starts GraphQL API with service discovery
# - Opens Aspire Dashboard at: https://localhost:15888
# - GraphQL playground available at: http://localhost:5000/graphql
```

### 3. Alternative: Manual Database Setup
```bash
# If not using Aspire, ensure PostgreSQL is running manually
# Update connection string in appsettings.json
dotnet ef database update -p src/BasicBudget.Infrastructure
dotnet run --project src/BasicBudget.GraphQL
```

### 4. Access Points
- **Aspire Dashboard**: https://localhost:15888 (logs, metrics, health)
- **GraphQL Playground**: http://localhost:5000/graphql (API testing)
- **GraphQL Subscriptions**: ws://localhost:5000/graphql (WebSocket for real-time)
- **pgAdmin**: http://localhost:8080 (database management)
- **Health Checks**: http://localhost:5000/health

## Quick Start Scenarios

### Scenario 1: Create Your First Account
**Goal**: Set up a checking account to track transactions

**GraphQL Mutation**:
```graphql
mutation CreateCheckingAccount {
  createAccount(input: {
    accountNumber: "12345678"
    name: "Primary Checking"
    accountType: CHECKING
    initialBalance: {
      amount: 1000.00
      currency: "USD"
    }
  }) {
    account {
      id
      name
      currentBalance {
        formatted
      }
    }
    errors {
      message
      code
    }
  }
}
```

**Expected Result**: Account created with $1,000.00 initial balance

### Scenario 2: Add Manual Transaction
**Goal**: Record a grocery store purchase

**GraphQL Mutation**:
```graphql
mutation AddGroceryTransaction {
  createTransaction(input: {
    accountId: "<account-id-from-step-1>"
    amount: {
      amount: -85.43
      currency: "USD"
    }
    transactionDate: "2025-09-06T14:30:00Z"
    description: "Whole Foods Market - Groceries"
  }) {
    transaction {
      id
      amount {
        formatted
      }
      description
    }
    errors {
      message
    }
  }
}
```

**Expected Result**: Transaction recorded, account balance reduced to $914.57

### Scenario 3: Create Spending Categories
**Goal**: Set up categories for budget tracking

**GraphQL Mutations**:
```graphql
# Create main category
mutation CreateGroceryCategory {
  createCategory(input: {
    name: "Groceries"
    description: "Food and household items"
    color: "#4CAF50"
  }) {
    category {
      id
      name
    }
    errors {
      message
    }
  }
}

# Create subcategory
mutation CreateFoodSubcategory {
  createCategory(input: {
    name: "Food"
    parentCategoryId: "<grocery-category-id>"
    color: "#66BB6A"
  }) {
    category {
      id
      name
      parentCategory {
        name
      }
    }
    errors {
      message
    }
  }
}
```

**Expected Result**: Category hierarchy created (Groceries > Food)

### Scenario 4: Create Monthly Budget
**Goal**: Set up a basic monthly budget with spending limits

**GraphQL Mutations**:
```graphql
# Create budget
mutation CreateMonthlyBudget {
  createBudget(input: {
    name: "September 2025 Budget"
    budgetType: BASIC
    startDate: "2025-09-01T00:00:00Z"
    endDate: "2025-09-30T23:59:59Z"
    accountIds: ["<checking-account-id>"]
  }) {
    budget {
      id
      name
      startDate
      endDate
    }
    errors {
      message
    }
  }
}

# Add grocery category to budget
mutation AddGroceryToBudget {
  addBudgetCategory(input: {
    budgetId: "<budget-id>"
    categoryId: "<grocery-category-id>"
    allocatedAmount: {
      amount: 400.00
      currency: "USD"
    }
    alertThreshold: 0.8
  }) {
    budgetCategory {
      allocatedAmount {
        formatted
      }
      alertThreshold
    }
    errors {
      message
    }
  }
}
```

**Expected Result**: Monthly budget with $400 grocery allocation and 80% alert threshold

### Scenario 5: Import Bank Statement
**Goal**: Import transactions from a CSV bank statement

**GraphQL Mutation**:
```graphql
mutation ImportStatement {
  importStatement(input: {
    accountId: "<checking-account-id>"
    fileData: <csv-file-upload>
    fileFormat: CSV
  }) {
    importedTransactions {
      id
      amount {
        formatted
      }
      description
      transactionDate
    }
    duplicateCount
    errors {
      message
    }
  }
}
```

**CSV File Format Example**:
```csv
Date,Description,Amount
2025-09-05,"Coffee Shop Purchase",-4.50
2025-09-04,"Paycheck Deposit",2500.00
2025-09-03,"Gas Station",-35.20
```

**Expected Result**: 3 transactions imported, duplicates detected and reported

### Scenario 6: Query Transaction History with Filtering
**Goal**: View transactions for a specific time period with category filtering

**GraphQL Query**:
```graphql
query MonthlyTransactions {
  transactions(
    first: 50
    where: {
      transactionDate: {
        gte: "2025-09-01T00:00:00Z"
        lte: "2025-09-30T23:59:59Z"
      }
      amount: {
        lt: 0  # Only expenses
      }
    }
    order: [{ transactionDate: DESC }]
  ) {
    nodes {
      id
      amount {
        formatted
      }
      description
      transactionDate
      category {
        name
      }
    }
    totalCount
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

**Expected Result**: Paginated list of September expenses, ordered by date

### Scenario 7: Generate Monthly Spending Summary
**Goal**: Get spending breakdown by category for budget analysis

**GraphQL Query**:
```graphql
query MonthlySummary {
  monthlySpending(
    accountIds: ["<checking-account-id>"]
    year: 2025
    month: 9
  ) {
    totalSpent {
      formatted
    }
    budgetComparison {
      budgeted {
        formatted
      }
      actual {
        formatted
      }
      variancePercentage
    }
    topCategories {
      category {
        name
      }
      totalAmount {
        formatted
      }
      transactionCount
    }
  }
}
```

**Expected Result**: Monthly spending summary with budget vs actual comparison

### Scenario 8: Check Budget Progress
**Goal**: Monitor budget performance and identify overspending

**GraphQL Query**:
```graphql
query BudgetProgress {
  budgetProgress(budgetId: "<budget-id>") {
    overallProgress
    categoryProgress {
      budgetCategory {
        category {
          name
        }
        allocatedAmount {
          formatted
        }
        spentAmount {
          formatted
        }
      }
      progressPercentage
      isOverBudget
    }
    projectedOverage {
      formatted
    }
    daysRemaining
  }
}
```

**Expected Result**: Budget progress percentages and overage projections

### Scenario 9: Subscribe to Real-time Budget Alerts
**Goal**: Receive real-time notifications when spending limits are approached

**GraphQL Subscription**:
```graphql
subscription BudgetAlerts {
  budgetAlertAdded {
    id
    alertType
    message
    currentAmount {
      formatted
    }
    limitAmount {
      formatted
    }
    percentageUsed
    timestamp
  }
}
```

**WebSocket Connection**: Connect to `ws://localhost:5000/graphql` with subscription payload

**Expected Result**: Real-time alerts when spending exceeds thresholds (75%, 90%, 100%)

### Scenario 10: Real-time Transaction Notifications
**Goal**: Get notified immediately when new transactions are added

**GraphQL Subscription**:
```graphql
subscription TransactionUpdates($accountId: ID) {
  transactionAdded(accountId: $accountId) {
    id
    amount {
      formatted
    }
    description
    transactionDate
    account {
      name
    }
  }
}
```

**Expected Result**: Live transaction updates when statements are imported or manual entries are added

## Testing the Implementation

### Contract Tests
All GraphQL operations should have corresponding contract tests that verify:
- Schema compliance
- Input validation
- Error handling
- Response structure

### Integration Tests
End-to-end scenarios should be tested:
- Account creation → Transaction import → Budget creation → Progress tracking
- Statement import with duplicate handling
- Category hierarchy and transaction categorization

### Example TUnit Test Structure
```csharp
[ParallelLimiter<DatabaseTestLimit>] // Ensure single database access across test suite
public class AccountManagementTests : IAsyncInitializer
{
    private DistributedApplication _app = null!;
    private HttpClient _httpClient = null!;
    
    public async Task InitializeAsync()
    {
        // TUnit async initialization - runs once per test class
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BasicBudget_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        
        _httpClient = _app.CreateHttpClient("basic-budget-api");
        await _app.WaitForTextAsync("Application started", "basic-budget-api");
    }
    
    [Before(Test)]
    public async Task ResetDatabase()
    {
        // TUnit per-test reset - runs before each test
        var connectionString = _app.GetConnectionString("basic-budget-db");
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var respawn = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
        await respawn.ResetAsync(connection);
    }
    
    [Test]
    public async Task CreateAccount_WithValidInput_ReturnsAccount()
    {
        // This test must fail initially (RED phase of TDD)
        // Arrange
        var mutation = """
            mutation CreateCheckingAccount {
              createAccount(input: {
                accountNumber: "12345678"
                name: "Primary Checking"
                accountType: CHECKING
                initialBalance: { amount: 1000.00, currency: "USD" }
              }) {
                account { id, name, currentBalance { formatted } }
                errors { message, code }
              }
            }
            """;
        
        // Act
        var result = await ExecuteGraphQLMutation(mutation);
        
        // Assert - Using TUnit's native fluent assertions
        await Assert.That(result.Data.CreateAccount.Account).IsNotNull()
            .And.ShouldHaveValidBalance(1000.00m)
            .And.Satisfies(account => account.Name == "Primary Checking");
        await Assert.That(result.Data.CreateAccount.Errors).IsEmpty();
        await Assert.That(result.Data.CreateAccount.Account.CurrentBalance.Formatted).IsEqualTo("$1,000.00");
    }
    
    private async Task<GraphQLResponse> ExecuteGraphQLMutation(string mutation)
    {
        var request = new GraphQLRequest { Query = mutation };
        var response = await _httpClient.PostAsJsonAsync("/graphql", request);
        return await response.Content.ReadFromJsonAsync<GraphQLResponse>();
    }
}

    [Test, MatrixDataSource]
    public async Task CreateAccount_ValidatesInputCombinations(
        [Matrix(AccountType.Checking, AccountType.Savings, AccountType.CreditCard)] AccountType accountType,
        [Matrix(-1000, 0, 100, 50000)] decimal initialBalance
    )
    {
        // This generates 12 test cases (3 account types × 4 balance values)
        // Demonstrates TUnit's powerful matrix testing capabilities
        var mutation = $$"""
            mutation TestAccountCreation {
              createAccount(input: {
                accountNumber: "TEST{{Random.Shared.Next(10000, 99999)}}"
                name: "Test {{accountType}} Account"
                accountType: {{accountType}}
                initialBalance: { amount: {{initialBalance}}, currency: "USD" }
              }) {
                account { id, accountType, currentBalance { amount } }
                errors { message, code }
              }
            }
            """;
        
        var result = await ExecuteGraphQLMutation(mutation);
        
        if (initialBalance < 0)
        {
            // Negative balances should be rejected for checking/savings, allowed for credit cards
            if (accountType == AccountType.CreditCard)
                await Assert.That(result.Data.CreateAccount.Account).IsNotNull();
            else
                await Assert.That(result.Data.CreateAccount.Errors).IsNotEmpty();
        }
        else
        {
            await Assert.That(result.Data.CreateAccount.Account).IsNotNull()
                .And.Satisfies(account => account.AccountType == accountType);
        }
    }
}

// Shared parallel limit for database access across entire test suite
public record DatabaseTestLimit : IParallelLimit
{
    public int Limit => 1; // Only one test can access database at a time
}

// Custom assertions for domain-specific validation
public static class AccountAssertions
{
    [CustomAssertion]
    public static async Task ShouldHaveValidBalance(this AssertionBuilder<Account> builder, decimal expectedAmount)
    {
        var account = await builder.GetActualValue();
        await Assert.That(account.CurrentBalance.Amount)
            .IsEqualTo(expectedAmount)
            .WithMessage($"Account '{account.Name}' should have balance of ${expectedAmount}");
    }
}
```

## Success Criteria
- ✅ All 8 scenarios execute without errors
- ✅ Database correctly stores and retrieves data
- ✅ GraphQL schema validates all operations
- ✅ Budget calculations are accurate
- ✅ Statement import handles CSV and QFX formats
- ✅ Filtering, sorting, and pagination work correctly
- ✅ All tests pass following TDD approach

## Troubleshooting

### Common Issues
1. **Database Connection**: Ensure PostgreSQL is running and connection string is correct
2. **Schema Validation**: Use GraphQL playground to validate queries before implementing
3. **File Upload**: Ensure file upload middleware is configured for statement imports
4. **Currency Formatting**: Verify Money value object handles decimal precision correctly

### Performance Considerations
- Transaction queries should use database indexes on date and account_id
- Large statement imports should be processed in batches
- Budget calculations should be cached for frequently accessed data