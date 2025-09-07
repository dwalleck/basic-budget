# Story: MUT-004 - Update Transaction

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to update an existing transaction. This mutation allows modification of transaction details including amount, date, description, and category while maintaining account balance consistency.

## Acceptance Criteria
- [ ] Mutation updates transaction details (amount, date, description, category)
- [ ] Account balance is recalculated when amount changes
- [ ] Validates transaction exists before updating
- [ ] Validates account exists and matches transaction
- [ ] Validates category exists if provided
- [ ] Returns updated transaction on success
- [ ] Returns validation errors on failure
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Transaction entity with update methods, balance recalculation
  - Application Layer: Create UpdateTransactionCommand via MediatR
  - GraphQL Layer: Add mutation resolver
- **Error Handling**: Use OneOf<Transaction, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Command**: `src/BasicBudget.Application/Commands/UpdateTransactionCommand.cs` (new)
- **GraphQL Mutation**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Input Type**: `src/BasicBudget.GraphQL/Inputs/UpdateTransactionInput.cs` (new)

### Related Files
- Domain entity: `/src/BasicBudget.Domain/Entities/Transaction.cs`
- Account entity: `/src/BasicBudget.Domain/Entities/Account.cs`
- Repository: `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`
- Account repository: `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`

### Schema Reference
```graphql
type Mutation {
  updateTransaction(input: UpdateTransactionInput!): UpdateTransactionPayload!
}

input UpdateTransactionInput {
  transactionId: ID!
  amount: MoneyInput!
  transactionDate: DateTime!
  description: String!
  categoryId: ID
}

input MoneyInput {
  amount: Decimal!
  currency: String! = "USD"
}

type UpdateTransactionPayload {
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
git checkout -b story/MUT-004-update-transaction
```

### 2. Create Input Type
Create `/src/BasicBudget.GraphQL/Inputs/UpdateTransactionInput.cs`:
```csharp
namespace BasicBudget.GraphQL.Inputs;

public record UpdateTransactionInput(
    Guid TransactionId,
    MoneyInput Amount,
    DateTime TransactionDate,
    string Description,
    Guid? CategoryId);
```

### 3. Create Payload Type
Create `/src/BasicBudget.GraphQL/Payloads/UpdateTransactionPayload.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Payloads;

public class UpdateTransactionPayload
{
    public Transaction? Transaction { get; }
    public IReadOnlyList<UserError> Errors { get; }

    public UpdateTransactionPayload(Transaction transaction)
    {
        Transaction = transaction;
        Errors = Array.Empty<UserError>();
    }

    public UpdateTransactionPayload(IReadOnlyList<UserError> errors)
    {
        Transaction = null;
        Errors = errors;
    }

    public UpdateTransactionPayload(UserError error)
    {
        Transaction = null;
        Errors = new[] { error };
    }
}
```

### 4. Create Application Command
Create `/src/BasicBudget.Application/Commands/UpdateTransactionCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record UpdateTransactionCommand(
    Guid TransactionId,
    Money Amount,
    DateTime TransactionDate,
    string Description,
    Guid? CategoryId
) : IRequest<OneOf<Transaction, DomainError>>;

public class UpdateTransactionCommandHandler 
    : IRequestHandler<UpdateTransactionCommand, OneOf<Transaction, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OneOf<Transaction, DomainError>> Handle(
        UpdateTransactionCommand request,
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

        // Get account for balance adjustment
        var account = await _accountRepository.GetByIdAsync(
            transaction.AccountId, 
            cancellationToken);
            
        if (account == null)
        {
            return new DomainError(
                "ACCOUNT_NOT_FOUND", 
                $"Associated account not found");
        }

        // Validate category exists if provided
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

        // Validate transaction date not in future
        if (request.TransactionDate > DateTime.UtcNow)
        {
            return new DomainError(
                "INVALID_TRANSACTION_DATE", 
                "Transaction date cannot be in the future");
        }

        // Store original amount for balance adjustment
        var originalAmount = transaction.Amount;

        // Update transaction
        var updateResult = transaction.UpdateDetails(
            request.Amount,
            request.TransactionDate,
            request.Description,
            request.CategoryId);

        if (updateResult.IsT1)
        {
            return updateResult.AsT1;
        }

        // Adjust account balance if amount changed
        if (!originalAmount.Equals(request.Amount))
        {
            // Reverse original transaction effect
            var reverseAmount = Money.Negate(originalAmount);
            account.UpdateBalance(reverseAmount);
            
            // Apply new transaction amount
            account.UpdateBalance(request.Amount);
        }

        // Save changes
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
```

### 5. Update Domain Entity
Add to `/src/BasicBudget.Domain/Entities/Transaction.cs`:
```csharp
public OneOf<Success, DomainError> UpdateDetails(
    Money amount, 
    DateTime transactionDate, 
    string description, 
    Guid? categoryId)
{
    if (string.IsNullOrWhiteSpace(description))
    {
        return new DomainError("INVALID_DESCRIPTION", "Transaction description cannot be empty");
    }

    if (transactionDate > DateTime.UtcNow)
    {
        return new DomainError("INVALID_TRANSACTION_DATE", "Transaction date cannot be in the future");
    }

    Amount = amount;
    TransactionDate = transactionDate;
    Description = description;
    CategoryId = categoryId;
    UpdatedAt = DateTime.UtcNow;
    
    return new Success();
}
```

### 6. Update Money Value Object
Add to `/src/BasicBudget.Domain/ValueObjects/Money.cs`:
```csharp
public static Money Negate(Money money)
{
    return new Money(-money.Amount, money.Currency);
}

public bool Equals(Money other)
{
    return Amount == other.Amount && Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase);
}
```

### 7. Update Transaction Repository
Add to `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`:
```csharp
Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
```

### 8. Add GraphQL Mutation
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Update an existing transaction")]
public async Task<UpdateTransactionPayload> UpdateTransactionAsync(
    UpdateTransactionInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(input.Description))
    {
        return new UpdateTransactionPayload(
            new UserError("Transaction description is required", "VALIDATION_ERROR", new[] { "description" }));
    }

    // Convert MoneyInput to Money value object
    var moneyResult = Money.Create(input.Amount.Amount, input.Amount.Currency);
    if (moneyResult.IsT1)
    {
        return new UpdateTransactionPayload(
            new UserError(moneyResult.AsT1.Message, "INVALID_MONEY_INPUT", new[] { "amount" }));
    }

    var command = new UpdateTransactionCommand(
        input.TransactionId,
        moneyResult.AsT0,
        input.TransactionDate,
        input.Description,
        input.CategoryId);

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        transaction => new UpdateTransactionPayload(transaction),
        error => new UpdateTransactionPayload(
            new UserError(error.Message, error.Code, new[] { "input" }))
    );
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
# Update transaction details
mutation {
  updateTransaction(input: {
    transactionId: "existing-transaction-guid"
    amount: { amount: 75.50, currency: "USD" }
    transactionDate: "2025-01-07T14:30:00Z"
    description: "Updated Grocery Store Purchase"
    categoryId: "food-category-guid"
  }) {
    transaction {
      id
      account {
        name
        currentBalance {
          formatted
        }
      }
      amount {
        amount
        currency
        formatted
      }
      transactionDate
      description
      category {
        name
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
  updateTransaction(input: {
    transactionId: "non-existent-guid"
    amount: { amount: 100.00, currency: "USD" }
    transactionDate: "2025-01-07T10:00:00Z"
    description: "Test Transaction"
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

# Test with future date
mutation {
  updateTransaction(input: {
    transactionId: "existing-transaction-guid"
    amount: { amount: 50.00, currency: "USD" }
    transactionDate: "2030-01-01T10:00:00Z"
    description: "Future Transaction"
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

### 10. Create Pull Request
```bash
git add .
git commit -m "feat: MUT-004 - Implement update transaction mutation"
git push origin story/MUT-004-update-transaction
gh pr create --title "MUT-004 - Update Transaction" --body "Implements GraphQL mutation to update transactions with balance recalculation"
```

## Dependencies
- **Blocked By**: MUT-003 (Create Transaction)
- **Blocks**: None

## Notes
- Account balance is automatically adjusted when transaction amount changes
- Original amount is reversed before applying new amount to maintain consistency
- Transaction date validation prevents future dates
- Category assignment is optional and can be cleared by passing null

## Definition of Done
- [ ] UpdateTransactionCommand and handler created in Application layer
- [ ] Input and payload types created for GraphQL
- [ ] Mutation resolver added to Mutation class
- [ ] Transaction existence validation works
- [ ] Account balance recalculation works correctly
- [ ] Category validation works when provided
- [ ] Solution builds without errors
- [ ] Manual testing confirms mutation and balance updates work
- [ ] Error cases properly handled
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch