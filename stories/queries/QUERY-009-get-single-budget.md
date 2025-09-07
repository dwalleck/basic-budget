# Story: QUERY-009 - Get Single Budget

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve a single budget by its ID. This query returns a specific budget with all its associated categories, spending limits, and calculated progress metrics.

## Acceptance Criteria
- [ ] Query returns budget when valid ID provided
- [ ] Returns null when budget not found
- [ ] All budget properties accessible including nested categories
- [ ] Budget progress calculations included (spent/remaining amounts)
- [ ] Money type properly formatted
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Budget entity from QUERY-008
  - Application Layer: Create GetBudgetQuery and handler via MediatR
  - GraphQL Layer: Add resolver to Query class
- **Error Handling**: Use OneOf<Budget, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetBudgetQuery.cs` (new)
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
  budget(id: ID!): Budget
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
  createdAt: DateTime!
  updatedAt: DateTime!
}

type BudgetCategory {
  id: ID!
  budget: Budget!
  category: Category!
  allocatedAmount: Money!
  spentAmount: Money!
  remainingAmount: Money!
  progressPercentage: Float!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-009-get-single-budget
```

### 2. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetBudgetQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetBudgetQuery(Guid Id) : IRequest<OneOf<Budget, DomainError>>;

public class GetBudgetQueryHandler : IRequestHandler<GetBudgetQuery, OneOf<Budget, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<OneOf<Budget, DomainError>> Handle(
        GetBudgetQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budget = await _budgetRepository.GetByIdAsync(request.Id, cancellationToken);
            
            if (budget == null)
            {
                return new DomainError("BUDGET_NOT_FOUND", $"Budget with ID {request.Id} was not found");
            }

            return OneOf<Budget, DomainError>.FromT0(budget);
        }
        catch (Exception ex)
        {
            return new DomainError("BUDGET_FETCH_FAILED", $"Failed to retrieve budget: {ex.Message}");
        }
    }
}
```

### 3. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve a single budget by its ID")]
[UseProjection]
public async Task<Budget?> GetBudgetAsync(
    [GraphQLDescription("The unique identifier of the budget")]
    [ID] Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetBudgetQuery(id), cancellationToken);
    return result.Match(
        budget => budget,
        error => error.Code == "BUDGET_NOT_FOUND" 
            ? null 
            : throw new GraphQLException(error.Message)
    );
}
```

### 4. Add Budget Extensions
Create `/src/BasicBudget.GraphQL/Types/BudgetExtensions.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using HotChocolate.Types;

namespace BasicBudget.GraphQL.Types;

[ExtendObjectType(typeof(Budget))]
public class BudgetExtensions
{
    [GraphQLDescription("Total amount spent across all categories in this budget")]
    public Money GetTotalSpent([Parent] Budget budget)
    {
        var totalSpent = budget.Categories.Sum(c => c.SpentAmount.Amount);
        return Money.Create(totalSpent, budget.Amount.Currency);
    }

    [GraphQLDescription("Total amount remaining across all categories in this budget")]
    public Money GetTotalRemaining([Parent] Budget budget)
    {
        var totalRemaining = budget.Categories.Sum(c => c.RemainingAmount.Amount);
        return Money.Create(totalRemaining, budget.Amount.Currency);
    }

    [GraphQLDescription("Overall progress percentage for this budget (0-100)")]
    public float GetProgressPercentage([Parent] Budget budget)
    {
        if (budget.Amount.Amount == 0) return 0;
        
        var totalSpent = budget.Categories.Sum(c => c.SpentAmount.Amount);
        return (float)((totalSpent / budget.Amount.Amount) * 100);
    }
}
```

### 5. Add BudgetCategory Extensions
Create `/src/BasicBudget.GraphQL/Types/BudgetCategoryExtensions.cs`:
```csharp
using BasicBudget.Domain.Entities;
using HotChocolate.Types;

namespace BasicBudget.GraphQL.Types;

[ExtendObjectType(typeof(BudgetCategory))]
public class BudgetCategoryExtensions
{
    [GraphQLDescription("Progress percentage for this budget category (0-100)")]
    public float GetProgressPercentage([Parent] BudgetCategory budgetCategory)
    {
        if (budgetCategory.AllocatedAmount.Amount == 0) return 0;
        
        return (float)((budgetCategory.SpentAmount.Amount / budgetCategory.AllocatedAmount.Amount) * 100);
    }
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
  budget(id: "valid-guid-here") {
    id
    name
    description
    amount {
      amount
      currency
      formatted
    }
    period
    startDate
    endDate
    isActive
    categories {
      id
      category {
        name
      }
      allocatedAmount {
        formatted
      }
      spentAmount {
        formatted
      }
      remainingAmount {
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
    createdAt
    updatedAt
  }
}

# Test with non-existent ID
query {
  budget(id: "00000000-0000-0000-0000-000000000000") {
    id
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-009 - Implement get single budget query"
git push origin story/QUERY-009-get-single-budget
gh pr create --title "QUERY-009 - Get Single Budget" --body "Implements GraphQL query to retrieve a single budget with progress calculations"
```

## Dependencies
- **Blocked By**: QUERY-008 (List All Budgets - requires Budget entity)
- **Blocks**: Budget-related mutations and advanced progress queries

## Notes
- Includes calculated fields for budget progress and spending totals
- Progress percentage is calculated as (spent / allocated) * 100
- Uses field extensions to add computed properties
- Consider caching progress calculations for performance

## Definition of Done
- [ ] GetBudgetQuery and handler created in Application layer
- [ ] GraphQL resolver with proper error handling added to Query class
- [ ] Budget and BudgetCategory field extensions created
- [ ] Solution builds without errors
- [ ] Manual testing confirms query works correctly
- [ ] Returns null for non-existent budgets
- [ ] Progress calculations are accurate
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch