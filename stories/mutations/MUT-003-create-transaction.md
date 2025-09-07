# Story: MUT-003 - Create Transaction

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to create a new transaction. This mutation will add a transaction to an account, optionally categorize it, and update the account balance.

## Acceptance Criteria
- [ ] Mutation creates a new transaction in the database
- [ ] Account balance is updated after transaction creation
- [ ] Optional category can be assigned during creation
- [ ] Validates account exists before creating transaction
- [ ] Validates category exists if provided
- [ ] Returns created transaction with all properties
- [ ] Returns appropriate errors for validation failures
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Transaction entity exists, may need balance update logic
  - Application Layer: Create/update CreateTransactionCommand and handler
  - GraphQL Layer: Add mutation resolver with input/payload types
- **Error Handling**: Use OneOf<Transaction, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Command**: `src/BasicBudget.Application/Commands/CreateTransactionCommand.cs`
- **GraphQL Types**: `src/BasicBudget.GraphQL/Types/TransactionTypes.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Transaction.cs` (existing)

### Related Files
- Domain entity: `/src/BasicBudget.Domain/Entities/Transaction.cs`
- Account entity: `/src/BasicBudget.Domain/Entities/Account.cs`
- Repository: `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`
- Account repository: `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`

### Schema Reference
```graphql
type Mutation {
  createTransaction(input: CreateTransactionInput!): CreateTransactionPayload!
}

input CreateTransactionInput {
  accountId: ID!
  amount: MoneyInput!
  transactionDate: DateTime!
  description: String!
  categoryId: ID
}

input MoneyInput {
  amount: Decimal!
  currency: String! = "USD"
}

type CreateTransactionPayload {
  transaction: Transaction
  errors: [Error!]!
}

type Transaction {
  id: ID!
  account: Account!
  amount: Money!
  transactionDate: DateTime!
  description: String!
  category: Category
  isReconciled: Boolean!
  importedAt: DateTime
}

type Error {
  message: String!
  code: String!
  path: [String!]!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/MUT-003-create-transaction
```

### 2. GraphQL Input/Payload Types
Create `/src/BasicBudget.GraphQL/Types/TransactionTypes.cs`:
```csharp
namespace BasicBudget.GraphQL.Types;

public record CreateTransactionInput(
    Guid AccountId,
    MoneyInput Amount,
    DateTime TransactionDate,
    string Description,
    Guid? CategoryId
);

public record CreateTransactionPayload(
    Transaction? Transaction,
    IReadOnlyList<ApiError>? Errors
);
```

### 3. Update Application Command
Update `/src/BasicBudget.Application/Commands/CreateTransactionCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record CreateTransactionCommand(
    Guid AccountId,
    Money Amount,
    DateTime TransactionDate,
    string Description,
    Guid? CategoryId
) : IRequest<OneOf<Transaction, DomainError>>;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, OneOf<Transaction, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<OneOf<Transaction, DomainError>> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // Validate account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            return new DomainError("ACCOUNT_NOT_FOUND", $"Account with ID {request.AccountId} not found");
        }

        // Validate category exists if provided
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken);
            if (category == null)
            {
                return new DomainError("CATEGORY_NOT_FOUND", $"Category with ID {request.CategoryId} not found");
            }
        }

        // Validate transaction date not in future
        if (request.TransactionDate > DateTime.UtcNow)
        {
            return new DomainError("INVALID_TRANSACTION_DATE", "Transaction date cannot be in the future");
        }

        // Create transaction
        var transactionResult = Transaction.Create(
            request.AccountId,
            request.Amount,
            request.TransactionDate,
            request.Description,
            request.CategoryId
        );

        if (transactionResult.IsT1)
        {
            return transactionResult.AsT1;
        }

        var transaction = transactionResult.AsT0;
        
        // Save transaction
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        
        // Update account balance
        account.UpdateBalance(request.Amount);
        await _accountRepository.UpdateAsync(account, cancellationToken);
        
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
```

### 4. Update Domain Entity
Add to `/src/BasicBudget.Domain/Entities/Account.cs`:
```csharp
public void UpdateBalance(Money transactionAmount)
{
    CurrentBalance = Money.Add(CurrentBalance, transactionAmount);
    UpdatedAt = DateTime.UtcNow;
}
```

### 5. GraphQL Mutation Resolver
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Create a new transaction")]
public async Task<CreateTransactionPayload> CreateTransactionAsync(
    CreateTransactionInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    // Convert MoneyInput to Money value object
    var moneyResult = Money.Create(input.Amount.Amount, input.Amount.Currency);
    if (moneyResult.IsT1)
    {
        var error = new ApiError(moneyResult.AsT1.Message, "INVALID_MONEY_INPUT");
        return new CreateTransactionPayload(null, new[] { error });
    }

    var command = new CreateTransactionCommand(
        input.AccountId,
        moneyResult.AsT0,
        input.TransactionDate,
        input.Description,
        input.CategoryId
    );

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        transaction => new CreateTransactionPayload(transaction, null),
        error => new CreateTransactionPayload(null, new[] { new ApiError(error.Message, error.Code) })
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

Test mutation:
```graphql
mutation {
  createTransaction(input: {
    accountId: "existing-account-guid"
    amount: { amount: 50.00, currency: "USD" }
    transactionDate: "2025-01-06T10:00:00Z"
    description: "Grocery Store Purchase"
    categoryId: null
  }) {
    transaction {
      id
      account {
        name
        currentBalance {
          amount
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
    }
    errors {
      message
      code
    }
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: MUT-003 - Implement create transaction mutation"
git push origin story/MUT-003-create-transaction
gh pr create --title "MUT-003 - Create Transaction" --body "Implements GraphQL mutation to create transactions with account balance updates"
```

## Dependencies
- **Blocked By**: TYPE-004 (Money Input Type) - though basic version exists
- **Blocks**: MUT-004 (Update Transaction), MUT-005 (Categorize Transaction)

## Notes
- Account balance update should be atomic with transaction creation
- Consider adding transaction validation rules (e.g., minimum/maximum amounts)
- Future enhancement: batch transaction creation for imports

## Definition of Done
- [ ] CreateTransactionCommand and handler implemented with validation
- [ ] Account balance update logic added
- [ ] GraphQL input and payload types created
- [ ] Mutation resolver added to Mutation class
- [ ] Solution builds without errors
- [ ] Manual testing confirms transaction creation and balance update
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch