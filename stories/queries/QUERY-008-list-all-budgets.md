# Story: QUERY-008 - List All Budgets

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve all budgets in the system. This query returns budgets with their associated categories and spending limits, using HotChocolate's built-in features for filtering and sorting.

## Acceptance Criteria
- [ ] Query returns all budgets from the database
- [ ] Each budget includes: id, name, description, amount, period, startDate, endDate, isActive
- [ ] Budget categories with spending limits are accessible
- [ ] Money type properly formatted for budget amounts
- [ ] Empty array returned when no budgets exist
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Budget entity needs to be created
  - Application Layer: Create GetBudgetsQuery and handler via MediatR
  - GraphQL Layer: Add resolver to Query class with HotChocolate attributes
- **Error Handling**: Use OneOf<IEnumerable<Budget>, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Budget.cs` (new)
- **Application Query**: `src/BasicBudget.Application/Queries/GetBudgetsQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Repository**: `src/BasicBudget.Domain/Repositories/IBudgetRepository.cs` (new)

### Schema Reference
```graphql
type Query {
  budgets: [Budget!]!
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
}

enum BudgetPeriod {
  MONTHLY
  QUARTERLY
  YEARLY
  CUSTOM
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-008-list-all-budgets
```

### 2. Create Domain Entity
Create `/src/BasicBudget.Domain/Entities/Budget.cs`:
```csharp
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Entities;

public class Budget : BaseEntity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money Amount { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }
    
    // Navigation properties
    public ICollection<BudgetCategory> Categories { get; private set; }
    
    private Budget() 
    { 
        Categories = new List<BudgetCategory>();
    }
    
    public Budget(
        string name,
        Money amount,
        BudgetPeriod period,
        DateTime startDate,
        DateTime endDate,
        string? description = null)
    {
        Name = name;
        Amount = amount;
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
        Description = description;
        IsActive = DateTime.UtcNow >= startDate && DateTime.UtcNow <= endDate;
        Categories = new List<BudgetCategory>();
    }
}

public enum BudgetPeriod
{
    Monthly,
    Quarterly,
    Yearly,
    Custom
}
```

### 3. Create BudgetCategory Entity
Create `/src/BasicBudget.Domain/Entities/BudgetCategory.cs`:
```csharp
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Entities;

public class BudgetCategory : BaseEntity
{
    public Guid BudgetId { get; private set; }
    public Budget Budget { get; private set; }
    
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; }
    
    public Money AllocatedAmount { get; private set; }
    public Money SpentAmount { get; private set; }
    
    public Money RemainingAmount => 
        Money.Create(AllocatedAmount.Amount - SpentAmount.Amount, AllocatedAmount.Currency);
    
    private BudgetCategory() { }
    
    public BudgetCategory(
        Budget budget,
        Category category,
        Money allocatedAmount)
    {
        Budget = budget;
        BudgetId = budget.Id;
        Category = category;
        CategoryId = category.Id;
        AllocatedAmount = allocatedAmount;
        SpentAmount = Money.Create(0, allocatedAmount.Currency);
    }
}
```

### 4. Create Repository Interface
Create `/src/BasicBudget.Domain/Repositories/IBudgetRepository.cs`:
```csharp
namespace BasicBudget.Domain.Repositories;

public interface IBudgetRepository
{
    IQueryable<Budget> GetQueryable();
    Task<IEnumerable<Budget>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 5. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetBudgetsQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetBudgetsQuery() : IRequest<OneOf<IEnumerable<Budget>, DomainError>>;

public class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, OneOf<IEnumerable<Budget>, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetsQueryHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<OneOf<IEnumerable<Budget>, DomainError>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var budgets = await _budgetRepository.GetAllAsync(cancellationToken);
            return OneOf<IEnumerable<Budget>, DomainError>.FromT0(budgets);
        }
        catch (Exception ex)
        {
            return new DomainError("BUDGETS_FETCH_FAILED", $"Failed to retrieve budgets: {ex.Message}");
        }
    }
}
```

### 6. Implement Repository
Create `/src/BasicBudget.Infrastructure/Persistence/BudgetRepository.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BasicBudget.Infrastructure.Persistence;

public class BudgetRepository : IBudgetRepository
{
    private readonly ApplicationDbContext _context;

    public BudgetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<Budget> GetQueryable()
    {
        return _context.Budgets
            .Include(b => b.Categories)
                .ThenInclude(bc => bc.Category)
            .AsQueryable();
    }

    public async Task<IEnumerable<Budget>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetQueryable().ToListAsync(cancellationToken);
    }

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetQueryable()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
```

### 7. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve all budgets in the system")]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Budget> GetBudgetsAsync(
    [Service] IBudgetRepository budgetRepository)
{
    // HotChocolate will automatically handle filtering, sorting, and projection
    return budgetRepository.GetQueryable();
}
```

### 8. Verify & Test
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
  budgets {
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
    }
    createdAt
    updatedAt
  }
}

# With filtering for active budgets only
query {
  budgets(where: { isActive: { eq: true } }) {
    name
    period
    amount {
      formatted
    }
  }
}

# With sorting by start date
query {
  budgets(order: [{ startDate: DESC }]) {
    name
    startDate
    endDate
  }
}
```

### 9. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-008 - Implement list all budgets query"
git push origin story/QUERY-008-list-all-budgets
gh pr create --title "QUERY-008 - List All Budgets" --body "Implements GraphQL query to retrieve all budgets with HotChocolate features"
```

## Dependencies
- **Blocked By**: TYPE-001 (Pagination types)
- **Blocks**: QUERY-009 (Get Single Budget), QUERY-010 (Get Active Budgets), QUERY-011 (Get Budget Progress)

## Notes
- Budget entity includes navigation to BudgetCategory for category allocations
- The IsActive property is calculated based on current date vs start/end dates
- HotChocolate attributes enable automatic filtering and sorting capabilities
- Consider implementing DataLoader for N+1 query prevention with categories

## Definition of Done
- [ ] Budget and BudgetCategory entities created in Domain layer
- [ ] GetBudgetsQuery and handler created in Application layer
- [ ] IBudgetRepository interface and implementation created
- [ ] GraphQL resolver added with HotChocolate attributes
- [ ] Solution builds without errors
- [ ] Manual testing confirms query returns budgets correctly
- [ ] Filtering and sorting work as expected
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch