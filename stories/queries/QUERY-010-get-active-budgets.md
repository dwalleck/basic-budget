# Story: QUERY-010 - Get Active Budgets

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve all currently active budgets. This query filters budgets based on their date range and isActive status, returning only budgets that are currently in effect with their progress metrics.

## Acceptance Criteria
- [ ] Query returns only active budgets (where current date is within start/end range)
- [ ] Each budget includes progress calculations and category details
- [ ] Empty array returned when no active budgets exist
- [ ] Supports filtering, sorting, and projection via HotChocolate attributes
- [ ] Money type properly formatted for all budget amounts
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Budget entity from QUERY-008
  - Application Layer: Create GetActiveBudgetsQuery and handler via MediatR
  - GraphQL Layer: Add resolver to Query class with HotChocolate attributes
- **Error Handling**: Use OneOf<IEnumerable<Budget>, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetActiveBudgetsQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Budget.cs` (existing)
- **Repository**: `src/BasicBudget.Domain/Repositories/IBudgetRepository.cs` (existing)

### Related Files
- Budget entity: `/src/BasicBudget.Domain/Entities/Budget.cs`
- Repository interface: `/src/BasicBudget.Domain/Repositories/IBudgetRepository.cs`
- Repository implementation: `/src/BasicBudget.Infrastructure/Persistence/BudgetRepository.cs`

### Schema Reference
```graphql
type Query {
  activeBudgets: [Budget!]!
}

type Budget {
  id: ID!
  name: String!
  description: String
  amount: Money!
  period: BudgetPeriod!
  startDate: DateTime!
  endDate: DateTime!
  isActive: Boolean!
  categories: [BudgetCategory!]!
  totalSpent: Money!
  totalRemaining: Money!
  progressPercentage: Float!
  daysRemaining: Int!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-010-get-active-budgets
```

### 2. Update Repository Interface
Add to `/src/BasicBudget.Domain/Repositories/IBudgetRepository.cs`:
```csharp
Task<IEnumerable<Budget>> GetActiveBudgetsAsync(CancellationToken cancellationToken = default);
IQueryable<Budget> GetActiveBudgetsQueryable();
```

### 3. Implement Repository Method
Add to `/src/BasicBudget.Infrastructure/Persistence/BudgetRepository.cs`:
```csharp
public async Task<IEnumerable<Budget>> GetActiveBudgetsAsync(CancellationToken cancellationToken = default)
{
    var currentDate = DateTime.UtcNow;
    return await GetQueryable()
        .Where(b => b.StartDate <= currentDate && b.EndDate >= currentDate)
        .ToListAsync(cancellationToken);
}

public IQueryable<Budget> GetActiveBudgetsQueryable()
{
    var currentDate = DateTime.UtcNow;
    return GetQueryable()
        .Where(b => b.StartDate <= currentDate && b.EndDate >= currentDate);
}
```

### 4. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetActiveBudgetsQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetActiveBudgetsQuery() : IRequest<OneOf<IEnumerable<Budget>, DomainError>>;

public class GetActiveBudgetsQueryHandler : IRequestHandler<GetActiveBudgetsQuery, OneOf<IEnumerable<Budget>, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetActiveBudgetsQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<OneOf<IEnumerable<Budget>, DomainError>> Handle(
        GetActiveBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var activeBudgets = await _budgetRepository.GetActiveBudgetsAsync(cancellationToken);
            return OneOf<IEnumerable<Budget>, DomainError>.FromT0(activeBudgets);
        }
        catch (Exception ex)
        {
            return new DomainError("ACTIVE_BUDGETS_FETCH_FAILED", $"Failed to retrieve active budgets: {ex.Message}");
        }
    }
}
```

### 5. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve all currently active budgets")]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Budget> GetActiveBudgetsAsync(
    [Service] IBudgetRepository budgetRepository)
{
    // HotChocolate will automatically handle filtering, sorting, and projection
    return budgetRepository.GetActiveBudgetsQueryable();
}
```

### 6. Add Budget Extensions for Active Budget Calculations
Update `/src/BasicBudget.GraphQL/Types/BudgetExtensions.cs`:
```csharp
[GraphQLDescription("Number of days remaining in this budget period")]
public int GetDaysRemaining([Parent] Budget budget)
{
    var currentDate = DateTime.UtcNow;
    var daysRemaining = (budget.EndDate - currentDate).Days;
    return Math.Max(0, daysRemaining);
}

[GraphQLDescription("Daily spending rate needed to stay within budget")]
public Money GetDailyBudgetRate([Parent] Budget budget)
{
    var daysRemaining = GetDaysRemaining(budget);
    if (daysRemaining == 0) return Money.Create(0, budget.Amount.Currency);
    
    var totalRemaining = GetTotalRemaining(budget);
    var dailyRate = totalRemaining.Amount / daysRemaining;
    return Money.Create(dailyRate, budget.Amount.Currency);
}

[GraphQLDescription("Whether this budget is on track (under spending rate)")]
public bool IsOnTrack([Parent] Budget budget)
{
    var totalDays = (budget.EndDate - budget.StartDate).Days;
    var elapsedDays = (DateTime.UtcNow - budget.StartDate).Days;
    
    if (totalDays == 0 || elapsedDays <= 0) return true;
    
    var expectedProgressPercentage = (float)(elapsedDays * 100.0 / totalDays);
    var actualProgressPercentage = GetProgressPercentage(budget);
    
    return actualProgressPercentage <= expectedProgressPercentage;
}
```

### 7. Verify & Test
```bash
# Build entire solution
dotnet build

# Run existing tests
dotnet test

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

Test query:
```graphql
query {
  activeBudgets {
    id
    name
    description
    amount {
      formatted
    }
    period
    startDate
    endDate
    isActive
    categories {
      category {
        name
      }
      allocatedAmount {
        formatted
      }
      spentAmount {
        formatted
      }
      progressPercentage
    }
    totalSpent {
      formatted
    }
    totalRemaining {
      formatted
    }
    progressPercentage
    daysRemaining
    dailyBudgetRate {
      formatted
    }
    isOnTrack
  }
}

# With filtering for specific period
query {
  activeBudgets(where: { period: { eq: MONTHLY } }) {
    name
    period
    progressPercentage
    isOnTrack
  }
}

# With sorting by progress percentage
query {
  activeBudgets(order: [{ progressPercentage: DESC }]) {
    name
    progressPercentage
    daysRemaining
  }
}
```

### 8. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-010 - Implement get active budgets query"
git push origin story/QUERY-010-get-active-budgets
gh pr create --title "QUERY-010 - Get Active Budgets" --body "Implements GraphQL query to retrieve currently active budgets with progress tracking"
```

## Dependencies
- **Blocked By**: QUERY-008 (List All Budgets), QUERY-009 (Get Single Budget)
- **Blocks**: Dashboard queries that rely on active budget data

## Notes
- Active budgets are determined by current date being within start/end date range
- Includes advanced calculations like daily budget rate and on-track status
- Uses IQueryable for optimal performance with HotChocolate features
- Consider adding budget alerts/notifications based on progress thresholds

## Definition of Done
- [ ] GetActiveBudgetsQuery and handler created in Application layer
- [ ] GetActiveBudgetsAsync and GetActiveBudgetsQueryable methods added to repository
- [ ] GraphQL resolver added with HotChocolate attributes
- [ ] Budget extensions updated with active budget calculations
- [ ] Solution builds without errors
- [ ] Manual testing confirms query returns only active budgets
- [ ] Progress calculations and tracking metrics are accurate
- [ ] Filtering and sorting work as expected
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch