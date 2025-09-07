# Story: QUERY-011 - Get Budget Progress

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve detailed progress information for a specific budget. This query provides comprehensive analytics including spending trends, category breakdowns, and projections based on current spending patterns.

## Acceptance Criteria
- [ ] Query returns detailed progress for a specific budget by ID
- [ ] Includes category-level progress breakdown with variance analysis
- [ ] Calculates spending trends and projections
- [ ] Returns null when budget not found
- [ ] Provides alert indicators for overspending categories
- [ ] All calculations are accurate and performant
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Budget entity and progress calculation value objects
  - Application Layer: Create GetBudgetProgressQuery and handler via MediatR
  - GraphQL Layer: Add resolver with progress-specific data
- **Error Handling**: Use OneOf<BudgetProgress, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Domain Value Object**: `src/BasicBudget.Domain/ValueObjects/BudgetProgress.cs` (new)
- **Application Query**: `src/BasicBudget.Application/Queries/GetBudgetProgressQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Repository**: `src/BasicBudget.Domain/Repositories/IBudgetRepository.cs` (existing)

### Related Files
- Budget entity: `/src/BasicBudget.Domain/Entities/Budget.cs`
- Transaction entity: `/src/BasicBudget.Domain/Entities/Transaction.cs`
- Repository interfaces for budget and transaction data

### Schema Reference
```graphql
type Query {
  budgetProgress(id: ID!): BudgetProgress
}

type BudgetProgress {
  budget: Budget!
  overallProgress: ProgressMetrics!
  categoryProgress: [CategoryProgress!]!
  spendingTrend: SpendingTrend!
  projections: BudgetProjections!
  alerts: [BudgetAlert!]!
  lastUpdated: DateTime!
}

type ProgressMetrics {
  totalBudgeted: Money!
  totalSpent: Money!
  totalRemaining: Money!
  percentageUsed: Float!
  percentageRemaining: Float!
  isOverBudget: Boolean!
  variance: Money!
}

type CategoryProgress {
  category: Category!
  budgeted: Money!
  spent: Money!
  remaining: Money!
  percentageUsed: Float!
  variance: Money!
  isOverBudget: Boolean!
  alert: AlertLevel!
}

type SpendingTrend {
  dailyAverage: Money!
  weeklyTrend: Float!
  monthlyTrend: Float!
  peakSpendingDay: DayOfWeek!
  consistencyScore: Float!
}

type BudgetProjections {
  projectedEndBalance: Money!
  projectedOverrun: Money!
  recommendedDailyLimit: Money!
  daysUntilOverBudget: Int
  completionDate: DateTime
}

type BudgetAlert {
  category: Category!
  level: AlertLevel!
  message: String!
  threshold: Float!
  current: Float!
}

enum AlertLevel {
  INFO
  WARNING
  CRITICAL
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-011-get-budget-progress
```

### 2. Create Domain Value Objects
Create `/src/BasicBudget.Domain/ValueObjects/BudgetProgress.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.Domain.ValueObjects;

public record BudgetProgress(
    Budget Budget,
    ProgressMetrics OverallProgress,
    IEnumerable<CategoryProgress> CategoryProgress,
    SpendingTrend SpendingTrend,
    BudgetProjections Projections,
    IEnumerable<BudgetAlert> Alerts,
    DateTime LastUpdated
);

public record ProgressMetrics(
    Money TotalBudgeted,
    Money TotalSpent,
    Money TotalRemaining,
    float PercentageUsed,
    float PercentageRemaining,
    bool IsOverBudget,
    Money Variance
);

public record CategoryProgress(
    Category Category,
    Money Budgeted,
    Money Spent,
    Money Remaining,
    float PercentageUsed,
    Money Variance,
    bool IsOverBudget,
    AlertLevel Alert
);

public record SpendingTrend(
    Money DailyAverage,
    float WeeklyTrend,
    float MonthlyTrend,
    DayOfWeek PeakSpendingDay,
    float ConsistencyScore
);

public record BudgetProjections(
    Money ProjectedEndBalance,
    Money ProjectedOverrun,
    Money RecommendedDailyLimit,
    int? DaysUntilOverBudget,
    DateTime? CompletionDate
);

public record BudgetAlert(
    Category Category,
    AlertLevel Level,
    string Message,
    float Threshold,
    float Current
);

public enum AlertLevel
{
    Info,
    Warning,
    Critical
}
```

### 3. Create Progress Calculation Service
Create `/src/BasicBudget.Domain/Services/IBudgetProgressService.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Services;

public interface IBudgetProgressService
{
    Task<BudgetProgress> CalculateProgressAsync(Budget budget, CancellationToken cancellationToken = default);
}
```

Create implementation in `/src/BasicBudget.Infrastructure/Services/BudgetProgressService.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Services;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Infrastructure.Services;

public class BudgetProgressService : IBudgetProgressService
{
    private readonly ITransactionRepository _transactionRepository;

    public BudgetProgressService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<BudgetProgress> CalculateProgressAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        // Get transactions for this budget period
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            budget.StartDate, budget.EndDate, cancellationToken);

        var overallProgress = CalculateOverallProgress(budget);
        var categoryProgress = CalculateCategoryProgress(budget, transactions);
        var spendingTrend = CalculateSpendingTrend(transactions, budget);
        var projections = CalculateProjections(budget, spendingTrend);
        var alerts = GenerateAlerts(categoryProgress);

        return new BudgetProgress(
            budget,
            overallProgress,
            categoryProgress,
            spendingTrend,
            projections,
            alerts,
            DateTime.UtcNow
        );
    }

    private ProgressMetrics CalculateOverallProgress(Budget budget)
    {
        var totalBudgeted = budget.Amount;
        var totalSpent = Money.Create(
            budget.Categories.Sum(c => c.SpentAmount.Amount),
            budget.Amount.Currency
        );
        var totalRemaining = Money.Create(
            totalBudgeted.Amount - totalSpent.Amount,
            budget.Amount.Currency
        );

        var percentageUsed = totalBudgeted.Amount > 0 
            ? (float)(totalSpent.Amount / totalBudgeted.Amount * 100) 
            : 0;
        var percentageRemaining = 100 - percentageUsed;
        var isOverBudget = totalSpent.Amount > totalBudgeted.Amount;
        var variance = Money.Create(
            totalSpent.Amount - totalBudgeted.Amount,
            budget.Amount.Currency
        );

        return new ProgressMetrics(
            totalBudgeted,
            totalSpent,
            totalRemaining,
            percentageUsed,
            percentageRemaining,
            isOverBudget,
            variance
        );
    }

    // Additional private methods for calculations...
    private IEnumerable<CategoryProgress> CalculateCategoryProgress(Budget budget, IEnumerable<Transaction> transactions)
    {
        return budget.Categories.Select(bc =>
        {
            var spent = bc.SpentAmount;
            var budgeted = bc.AllocatedAmount;
            var remaining = bc.RemainingAmount;
            var percentageUsed = budgeted.Amount > 0 ? (float)(spent.Amount / budgeted.Amount * 100) : 0;
            var variance = Money.Create(spent.Amount - budgeted.Amount, budgeted.Currency);
            var isOverBudget = spent.Amount > budgeted.Amount;
            
            var alert = percentageUsed switch
            {
                >= 100 => AlertLevel.Critical,
                >= 80 => AlertLevel.Warning,
                >= 60 => AlertLevel.Info,
                _ => AlertLevel.Info
            };

            return new CategoryProgress(
                bc.Category,
                budgeted,
                spent,
                remaining,
                percentageUsed,
                variance,
                isOverBudget,
                alert
            );
        });
    }

    // More calculation methods would be implemented here...
}
```

### 4. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetBudgetProgressQuery.cs`:
```csharp
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Services;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetBudgetProgressQuery(Guid BudgetId) : IRequest<OneOf<BudgetProgress, DomainError>>;

public class GetBudgetProgressQueryHandler : IRequestHandler<GetBudgetProgressQuery, OneOf<BudgetProgress, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IBudgetProgressService _progressService;

    public GetBudgetProgressQueryHandler(
        IBudgetRepository budgetRepository,
        IBudgetProgressService progressService)
    {
        _budgetRepository = budgetRepository;
        _progressService = progressService;
    }

    public async Task<OneOf<BudgetProgress, DomainError>> Handle(
        GetBudgetProgressQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await _budgetRepository.GetByIdAsync(request.BudgetId, cancellationToken);
            
            if (budget == null)
            {
                return new DomainError("BUDGET_NOT_FOUND", $"Budget with ID {request.BudgetId} was not found");
            }

            var progress = await _progressService.CalculateProgressAsync(budget, cancellationToken);
            return OneOf<BudgetProgress, DomainError>.FromT0(progress);
        }
        catch (Exception ex)
        {
            return new DomainError("BUDGET_PROGRESS_CALCULATION_FAILED", 
                $"Failed to calculate budget progress: {ex.Message}");
        }
    }
}
```

### 5. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Get detailed progress information for a specific budget")]
public async Task<BudgetProgress?> GetBudgetProgressAsync(
    [GraphQLDescription("The unique identifier of the budget")]
    [ID] Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetBudgetProgressQuery(id), cancellationToken);
    return result.Match(
        progress => progress,
        error => error.Code == "BUDGET_NOT_FOUND" 
            ? null 
            : throw new GraphQLException(error.Message)
    );
}
```

### 6. Verify & Test
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
  budgetProgress(id: "valid-budget-id") {
    budget {
      name
      period
    }
    overallProgress {
      totalBudgeted {
        formatted
      }
      totalSpent {
        formatted
      }
      percentageUsed
      isOverBudget
      variance {
        formatted
      }
    }
    categoryProgress {
      category {
        name
      }
      budgeted {
        formatted
      }
      spent {
        formatted
      }
      percentageUsed
      alert
      isOverBudget
    }
    spendingTrend {
      dailyAverage {
        formatted
      }
      peakSpendingDay
      consistencyScore
    }
    projections {
      projectedEndBalance {
        formatted
      }
      recommendedDailyLimit {
        formatted
      }
      daysUntilOverBudget
    }
    alerts {
      category {
        name
      }
      level
      message
      threshold
      current
    }
    lastUpdated
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-011 - Implement budget progress analytics query"
git push origin story/QUERY-011-get-budget-progress
gh pr create --title "QUERY-011 - Get Budget Progress" --body "Implements comprehensive budget progress analytics with trends and projections"
```

## Dependencies
- **Blocked By**: QUERY-009 (Get Single Budget), Transaction queries for spending data
- **Blocks**: Advanced dashboard and reporting features

## Notes
- Complex calculations require efficient transaction querying
- Consider caching progress calculations for frequently accessed budgets
- Spending trends require sufficient transaction history for accuracy
- Alert thresholds should be configurable per user/organization

## Definition of Done
- [ ] BudgetProgress value object and related types created in Domain layer
- [ ] IBudgetProgressService interface and implementation created
- [ ] GetBudgetProgressQuery and handler created in Application layer
- [ ] GraphQL resolver added with comprehensive progress data
- [ ] Solution builds without errors
- [ ] Manual testing confirms accurate progress calculations
- [ ] Performance is acceptable for complex calculations
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch