# Story: MUT-005 - Categorize Transaction

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to assign or change the category of an existing transaction. This is a focused operation that only updates the transaction's category without affecting other properties.

## Acceptance Criteria
- [ ] Mutation assigns category to an uncategorized transaction
- [ ] Mutation changes category of an already categorized transaction
- [ ] Mutation can remove category (set to null) from a transaction
- [ ] Validates transaction exists before categorizing
- [ ] Validates category exists if provided (when not null)
- [ ] Returns updated transaction with category information
- [ ] Returns validation errors on failure
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Transaction entity with category assignment methods
  - Application Layer: Create CategorizeTransactionCommand via MediatR
  - GraphQL Layer: Add mutation resolver
- **Error Handling**: Use OneOf<Transaction, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Command**: `src/BasicBudget.Application/Commands/CategorizeTransactionCommand.cs` (new)
- **GraphQL Mutation**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Input Type**: `src/BasicBudget.GraphQL/Inputs/CategorizeTransactionInput.cs` (new)

### Related Files
- Domain entity: `/src/BasicBudget.Domain/Entities/Transaction.cs`
- Category entity: `/src/BasicBudget.Domain/Entities/Category.cs`
- Repository: `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`
- Category repository: `/src/BasicBudget.Domain/Repositories/ICategoryRepository.cs`

### Schema Reference
```graphql
type Mutation {
  categorizeTransaction(input: CategorizeTransactionInput!): CategorizeTransactionPayload!
}

input CategorizeTransactionInput {
  transactionId: ID!
  categoryId: ID  # nullable - null removes category
}

type CategorizeTransactionPayload {
  transaction: Transaction
  errors: [UserError!]
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
git checkout -b story/MUT-005-categorize-transaction
```

### 2. Create Input Type
Create `/src/BasicBudget.GraphQL/Inputs/CategorizeTransactionInput.cs`:
```csharp
namespace BasicBudget.GraphQL.Inputs;

public record CategorizeTransactionInput(
    Guid TransactionId,
    Guid? CategoryId);  // null to remove category
```

### 3. Create Payload Type
Create `/src/BasicBudget.GraphQL/Payloads/CategorizeTransactionPayload.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Payloads;

public class CategorizeTransactionPayload
{
    public Transaction? Transaction { get; }
    public IReadOnlyList<UserError> Errors { get; }

    public CategorizeTransactionPayload(Transaction transaction)
    {
        Transaction = transaction;
        Errors = Array.Empty<UserError>();
    }

    public CategorizeTransactionPayload(IReadOnlyList<UserError> errors)
    {
        Transaction = null;
        Errors = errors;
    }

    public CategorizeTransactionPayload(UserError error)
    {
        Transaction = null;
        Errors = new[] { error };
    }
}
```

### 4. Create Application Command
Create `/src/BasicBudget.Application/Commands/CategorizeTransactionCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record CategorizeTransactionCommand(
    Guid TransactionId,
    Guid? CategoryId
) : IRequest<OneOf<Transaction, DomainError>>;

public class CategorizeTransactionCommandHandler 
    : IRequestHandler<CategorizeTransactionCommand, OneOf<Transaction, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategorizeTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OneOf<Transaction, DomainError>> Handle(
        CategorizeTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // Validate transaction exists
        var transaction = await _transactionRepository.GetByIdAsync(
            request.TransactionId, 
            cancellationToken);
            
        if (transaction == null)
        {
            return new DomainError(
                "TRANSACTION_NOT_FOUND", 
                $"Transaction with ID '{request.TransactionId}' not found");
        }

        // Validate category exists if provided (null means remove category)
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId.Value, 
                cancellationToken);
                
            if (category == null)
            {
                return new DomainError(
                    "CATEGORY_NOT_FOUND", 
                    $"Category with ID '{request.CategoryId}' not found");
            }
        }

        // Update transaction category
        var categorizeResult = transaction.AssignCategory(request.CategoryId);
        if (categorizeResult.IsT1)
        {
            return categorizeResult.AsT1;
        }

        // Save changes
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
```

### 5. Update Domain Entity
Add to `/src/BasicBudget.Domain/Entities/Transaction.cs`:
```csharp
public OneOf<Success, DomainError> AssignCategory(Guid? categoryId)
{
    // Allow null to remove category
    CategoryId = categoryId;
    UpdatedAt = DateTime.UtcNow;
    
    return new Success();
}

public OneOf<Success, DomainError> RemoveCategory()
{
    CategoryId = null;
    UpdatedAt = DateTime.UtcNow;
    
    return new Success();
}
```

### 6. Add GraphQL Mutation
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Assign or remove category from a transaction")]
public async Task<CategorizeTransactionPayload> CategorizeTransactionAsync(
    CategorizeTransactionInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var command = new CategorizeTransactionCommand(
        input.TransactionId,
        input.CategoryId);

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        transaction => new CategorizeTransactionPayload(transaction),
        error => new CategorizeTransactionPayload(
            new UserError(error.Message, error.Code, new[] { "input" }))
    );
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

Test mutations:
```graphql
# Assign category to transaction
mutation {
  categorizeTransaction(input: {
    transactionId: "existing-transaction-guid"
    categoryId: "food-category-guid"
  }) {
    transaction {
      id
      description
      amount {
        formatted
      }
      category {
        id
        name
        color
      }
      updatedAt
    }
    errors {
      message
      code
      path
    }
  }
}

# Remove category from transaction
mutation {
  categorizeTransaction(input: {
    transactionId: "existing-transaction-guid"
    categoryId: null
  }) {
    transaction {
      id
      description
      category {
        name  # Should be null
      }
      updatedAt
    }
    errors {
      message
      code
      path
    }
  }
}

# Change category of already categorized transaction
mutation {
  categorizeTransaction(input: {
    transactionId: "existing-transaction-guid"
    categoryId: "entertainment-category-guid"
  }) {
    transaction {
      id
      description
      category {
        name
        color
      }
      updatedAt
    }
    errors {
      message
      code
      path
    }
  }
}

# Test with non-existent transaction
mutation {
  categorizeTransaction(input: {
    transactionId: "non-existent-guid"
    categoryId: "food-category-guid"
  }) {
    transaction {
      id
    }
    errors {
      message
      code
      path
    }
  }
}

# Test with non-existent category
mutation {
  categorizeTransaction(input: {
    transactionId: "existing-transaction-guid"
    categoryId: "non-existent-category-guid"
  }) {
    transaction {
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

### 8. Create Pull Request
```bash
git add .
git commit -m "feat: MUT-005 - Implement categorize transaction mutation"
git push origin story/MUT-005-categorize-transaction
gh pr create --title "MUT-005 - Categorize Transaction" --body "Implements GraphQL mutation to assign or remove categories from transactions"
```

## Dependencies
- **Blocked By**: MUT-003 (Create Transaction), CAT-001 (Create Category)
- **Blocks**: None

## Notes
- This is a focused operation that only affects the transaction's category
- Passing null for categoryId removes the category from the transaction
- No balance adjustments needed since only category is being modified
- Operation updates the transaction's UpdatedAt timestamp
- Useful for bulk categorization workflows

## Definition of Done
- [ ] CategorizeTransactionCommand and handler created in Application layer
- [ ] Input and payload types created for GraphQL
- [ ] Mutation resolver added to Mutation class
- [ ] Transaction existence validation works
- [ ] Category existence validation works when provided
- [ ] Category removal (null assignment) works
- [ ] Solution builds without errors
- [ ] Manual testing confirms categorization works
- [ ] Error cases properly handled
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch