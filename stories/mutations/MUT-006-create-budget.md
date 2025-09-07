# Story: MUT-006 - Create Budget

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to create a new budget. This mutation creates a budget with a specified amount, time period, and associated categories for tracking spending against the budget.

## Acceptance Criteria
- [ ] Mutation creates new budget with all required fields
- [ ] Validates budget name is unique within the time period
- [ ] Validates start date is not after end date
- [ ] Validates budget amount is positive
- [ ] Validates all provided category IDs exist
- [ ] Returns created budget on success
- [ ] Returns validation errors on failure
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Budget entity with factory methods and validation
  - Application Layer: Create CreateBudgetCommand via MediatR
  - GraphQL Layer: Add mutation resolver
- **Error Handling**: Use OneOf<Budget, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Budget.cs` (new)
- **Application Command**: `src/BasicBudget.Application/Commands/CreateBudgetCommand.cs` (new)
- **GraphQL Mutation**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Input Type**: `src/BasicBudget.GraphQL/Inputs/CreateBudgetInput.cs` (new)
- **Repository**: `src/BasicBudget.Domain/Repositories/IBudgetRepository.cs` (new)

### Related Files
- Category entity: `/src/BasicBudget.Domain/Entities/Category.cs`
- Money value object: `/src/BasicBudget.Domain/ValueObjects/Money.cs`
- Category repository: `/src/BasicBudget.Domain/Repositories/ICategoryRepository.cs`

### Schema Reference
```graphql
type Mutation {
  createBudget(input: CreateBudgetInput!): CreateBudgetPayload!
}

input CreateBudgetInput {
  name: String!
  budgetAmount: MoneyInput!
  startDate: DateTime!
  endDate: DateTime!
  categoryIds: [ID!]!
}

input MoneyInput {
  amount: Decimal!
  currency: String! = "USD"
}

type CreateBudgetPayload {
  budget: Budget
  errors: [UserError!]
}

type Budget {
  id: ID!
  name: String!
  budgetAmount: Money!
  startDate: DateTime!
  endDate: DateTime!
  categories: [Category!]!
  spent: Money!
  remaining: Money!
  percentUsed: Float!
  createdAt: DateTime!
  updatedAt: DateTime!
}

type UserError {
  message: String!
  code: String!
  path: [String!]
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/MUT-006-create-budget
```

### 2. Create Budget Domain Entity
Create `/src/BasicBudget.Domain/Entities/Budget.cs`:
```csharp
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.ValueObjects;
using OneOf;

namespace BasicBudget.Domain.Entities;

public class Budget
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money BudgetAmount { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public List<Guid> CategoryIds { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Constructor
    private Budget() { }

    private Budget(
        string name,
        Money budgetAmount,
        DateTime startDate,
        DateTime endDate,
        List<Guid> categoryIds)
    {
        Id = Guid.NewGuid();
        Name = name;
        BudgetAmount = budgetAmount;
        StartDate = startDate;
        EndDate = endDate;
        CategoryIds = categoryIds;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static OneOf<Budget, DomainError> Create(
        string name,
        Money budgetAmount,
        DateTime startDate,
        DateTime endDate,
        List<Guid> categoryIds)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return new DomainError("INVALID_BUDGET_NAME", "Budget name cannot be empty");
        }

        // Validate budget amount
        if (budgetAmount.Amount <= 0)
        {
            return new DomainError("INVALID_BUDGET_AMOUNT", "Budget amount must be positive");
        }

        // Validate date range
        if (startDate >= endDate)
        {
            return new DomainError("INVALID_DATE_RANGE", "Start date must be before end date");
        }

        // Validate categories provided
        if (categoryIds == null || categoryIds.Count == 0)
        {
            return new DomainError("NO_CATEGORIES", "Budget must include at least one category");
        }

        return new Budget(name, budgetAmount, startDate, endDate, categoryIds);
    }

    public OneOf<Success, DomainError> UpdateDetails(
        string name,
        Money budgetAmount,
        DateTime startDate,
        DateTime endDate,
        List<Guid> categoryIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new DomainError("INVALID_BUDGET_NAME", "Budget name cannot be empty");
        }

        if (budgetAmount.Amount <= 0)
        {
            return new DomainError("INVALID_BUDGET_AMOUNT", "Budget amount must be positive");
        }

        if (startDate >= endDate)
        {
            return new DomainError("INVALID_DATE_RANGE", "Start date must be before end date");
        }

        if (categoryIds == null || categoryIds.Count == 0)
        {
            return new DomainError("NO_CATEGORIES", "Budget must include at least one category");
        }

        Name = name;
        BudgetAmount = budgetAmount;
        StartDate = startDate;
        EndDate = endDate;
        CategoryIds = categoryIds;
        UpdatedAt = DateTime.UtcNow;

        return new Success();
    }
}
```

### 3. Create Budget Repository Interface
Create `/src/BasicBudget.Domain/Repositories/IBudgetRepository.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.Domain.Repositories;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Budget?> GetByNameAndPeriodAsync(string name, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task AddAsync(Budget budget, CancellationToken cancellationToken = default);
    Task UpdateAsync(Budget budget, CancellationToken cancellationToken = default);
    Task DeleteAsync(Budget budget, CancellationToken cancellationToken = default);
}
```

### 4. Create Input Type
Create `/src/BasicBudget.GraphQL/Inputs/CreateBudgetInput.cs`:
```csharp
namespace BasicBudget.GraphQL.Inputs;

public record CreateBudgetInput(
    string Name,
    MoneyInput BudgetAmount,
    DateTime StartDate,
    DateTime EndDate,
    List<Guid> CategoryIds);
```

### 5. Create Payload Type
Create `/src/BasicBudget.GraphQL/Payloads/CreateBudgetPayload.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Payloads;

public class CreateBudgetPayload
{
    public Budget? Budget { get; }
    public IReadOnlyList<UserError> Errors { get; }

    public CreateBudgetPayload(Budget budget)
    {
        Budget = budget;
        Errors = Array.Empty<UserError>();
    }

    public CreateBudgetPayload(IReadOnlyList<UserError> errors)
    {
        Budget = null;
        Errors = errors;
    }

    public CreateBudgetPayload(UserError error)
    {
        Budget = null;
        Errors = new[] { error };
    }
}
```

### 6. Create Application Command
Create `/src/BasicBudget.Application/Commands/CreateBudgetCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record CreateBudgetCommand(
    string Name,
    Money BudgetAmount,
    DateTime StartDate,
    DateTime EndDate,
    List<Guid> CategoryIds
) : IRequest<OneOf<Budget, DomainError>>;

public class CreateBudgetCommandHandler 
    : IRequestHandler<CreateBudgetCommand, OneOf<Budget, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OneOf<Budget, DomainError>> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        // Check for duplicate budget name in the same period
        var existingBudget = await _budgetRepository.GetByNameAndPeriodAsync(
            request.Name, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);
            
        if (existingBudget != null)
        {
            return new DomainError(
                "DUPLICATE_BUDGET_NAME", 
                $"Budget with name '{request.Name}' already exists for the specified period");
        }

        // Validate all categories exist
        foreach (var categoryId in request.CategoryIds)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                return new DomainError(
                    "CATEGORY_NOT_FOUND", 
                    $"Category with ID '{categoryId}' not found");
            }
        }

        // Create budget
        var budgetResult = Budget.Create(
            request.Name,
            request.BudgetAmount,
            request.StartDate,
            request.EndDate,
            request.CategoryIds);

        if (budgetResult.IsT1)
        {
            return budgetResult.AsT1;
        }

        var budget = budgetResult.AsT0;

        // Save to repository
        await _budgetRepository.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return budget;
    }
}
```

### 7. Add GraphQL Mutation
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Create a new budget")]
public async Task<CreateBudgetPayload> CreateBudgetAsync(
    CreateBudgetInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(input.Name))
    {
        return new CreateBudgetPayload(
            new UserError("Budget name is required", "VALIDATION_ERROR", new[] { "name" }));
    }

    if (input.CategoryIds == null || input.CategoryIds.Count == 0)
    {
        return new CreateBudgetPayload(
            new UserError("At least one category is required", "VALIDATION_ERROR", new[] { "categoryIds" }));
    }

    // Convert MoneyInput to Money value object
    var moneyResult = Money.Create(input.BudgetAmount.Amount, input.BudgetAmount.Currency);
    if (moneyResult.IsT1)
    {
        return new CreateBudgetPayload(
            new UserError(moneyResult.AsT1.Message, "INVALID_MONEY_INPUT", new[] { "budgetAmount" }));
    }

    var command = new CreateBudgetCommand(
        input.Name,
        moneyResult.AsT0,
        input.StartDate,
        input.EndDate,
        input.CategoryIds);

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        budget => new CreateBudgetPayload(budget),
        error => new CreateBudgetPayload(
            new UserError(error.Message, error.Code, new[] { "input" }))
    );
}
```

### 8. Add GraphQL Budget Type
Create `/src/BasicBudget.GraphQL/Types/BudgetType.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Types;

[ObjectType<Budget>]
public static partial class BudgetType
{
    static partial void Configure(IObjectTypeDescriptor<Budget> descriptor)
    {
        descriptor
            .Field(b => b.Id)
            .Type<NonNullType<IdType>>();

        descriptor
            .Field(b => b.Name)
            .Type<NonNullType<StringType>>();

        descriptor
            .Field(b => b.BudgetAmount)
            .Type<NonNullType<MoneyType>>();

        descriptor
            .Field(b => b.StartDate)
            .Type<NonNullType<DateTimeType>>();

        descriptor
            .Field(b => b.EndDate)
            .Type<NonNullType<DateTimeType>>();

        descriptor
            .Field(b => b.CreatedAt)
            .Type<NonNullType<DateTimeType>>();

        descriptor
            .Field(b => b.UpdatedAt)
            .Type<NonNullType<DateTimeType>>();

        // Computed fields would be added here
        // e.g., spent, remaining, percentUsed
    }
}
```

### 9. Verify & Test
```bash
# Build entire solution
dotnet build

# Run existing tests
dotnet test

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

Test mutations:
```graphql
# Create monthly budget
mutation {
  createBudget(input: {
    name: "January 2025 Budget"
    budgetAmount: { amount: 2500.00, currency: "USD" }
    startDate: "2025-01-01T00:00:00Z"
    endDate: "2025-01-31T23:59:59Z"
    categoryIds: ["food-category-guid", "entertainment-category-guid", "transport-category-guid"]
  }) {
    budget {
      id
      name
      budgetAmount {
        amount
        currency
        formatted
      }
      startDate
      endDate
      categories {
        name
        color
      }
      createdAt
    }
    errors {
      message
      code
      path
    }
  }
}

# Test duplicate name validation
mutation {
  createBudget(input: {
    name: "January 2025 Budget"  # Same name and period
    budgetAmount: { amount: 3000.00, currency: "USD" }
    startDate: "2025-01-01T00:00:00Z"
    endDate: "2025-01-31T23:59:59Z"
    categoryIds: ["food-category-guid"]
  }) {
    budget {
      id
    }
    errors {
      message
      code
      path
    }
  }
}

# Test invalid date range
mutation {
  createBudget(input: {
    name: "Invalid Budget"
    budgetAmount: { amount: 1000.00, currency: "USD" }
    startDate: "2025-02-01T00:00:00Z"
    endDate: "2025-01-31T23:59:59Z"  # End before start
    categoryIds: ["food-category-guid"]
  }) {
    budget {
      id
    }
    errors {
      message
      code
      path
    }
  }
}

# Test zero amount validation
mutation {
  createBudget(input: {
    name: "Zero Budget"
    budgetAmount: { amount: 0.00, currency: "USD" }
    startDate: "2025-03-01T00:00:00Z"
    endDate: "2025-03-31T23:59:59Z"
    categoryIds: ["food-category-guid"]
  }) {
    budget {
      id
    }
    errors {
      message
      code
      path
    }
  }
}
```

### 10. Create Pull Request
```bash
git add .
git commit -m "feat: MUT-006 - Implement create budget mutation"
git push origin story/MUT-006-create-budget
gh pr create --title "MUT-006 - Create Budget" --body "Implements GraphQL mutation to create budgets with category assignments and validation"
```

## Dependencies
- **Blocked By**: CAT-001 (Create Category) - need categories to assign to budget
- **Blocks**: QUERY-005 (Budget queries), MUT-007 (Update Budget)

## Notes
- Budget names must be unique within the same time period
- At least one category must be assigned to each budget
- Budget amount must be positive
- Date validation ensures logical time periods
- Future enhancements: budget templates, recurring budgets

## Definition of Done
- [ ] Budget domain entity created with validation
- [ ] CreateBudgetCommand and handler created in Application layer
- [ ] Input and payload types created for GraphQL
- [ ] IBudgetRepository interface created
- [ ] Budget GraphQL type created
- [ ] Mutation resolver added to Mutation class
- [ ] Budget name uniqueness validation works
- [ ] Category existence validation works
- [ ] Date range validation works
- [ ] Solution builds without errors
- [ ] Manual testing confirms budget creation works
- [ ] Error cases properly handled
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch