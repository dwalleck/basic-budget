# Story: MUT-001 - Create Account

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to create a new account. This mutation creates an account with validation for account number uniqueness and proper initial balance handling.

## Acceptance Criteria
- [ ] Mutation creates new account with all required fields
- [ ] Account number uniqueness is validated
- [ ] Initial balance defaults to 0 if not provided
- [ ] Returns created account on success
- [ ] Returns validation errors on failure
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Account entity with factory methods
  - Application Layer: Create CreateAccountCommand via MediatR
  - GraphQL Layer: Add mutation resolver
- **Error Handling**: Use OneOf<Account, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Command**: `src/BasicBudget.Application/Commands/CreateAccountCommand.cs` (new)
- **GraphQL Mutation**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Input Type**: `src/BasicBudget.GraphQL/Inputs/CreateAccountInput.cs` (new)

### Schema Reference
```graphql
type Mutation {
  createAccount(input: CreateAccountInput!): CreateAccountPayload!
}

input CreateAccountInput {
  accountNumber: String!
  name: String!
  accountType: AccountType!
  initialBalance: MoneyInput
}

input MoneyInput {
  amount: Decimal!
  currency: String!
}

type CreateAccountPayload {
  account: Account
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
git checkout -b story/MUT-001-create-account
```

### 2. Create Input Type
Create `/src/BasicBudget.GraphQL/Inputs/CreateAccountInput.cs`:
```csharp
using BasicBudget.Domain.Enums;

namespace BasicBudget.GraphQL.Inputs;

public record CreateAccountInput(
    string AccountNumber,
    string Name,
    AccountType AccountType,
    MoneyInput? InitialBalance);

public record MoneyInput(
    decimal Amount,
    string Currency = "USD");
```

### 3. Create Payload Type
Create `/src/BasicBudget.GraphQL/Payloads/CreateAccountPayload.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Payloads;

public class CreateAccountPayload
{
    public Account? Account { get; }
    public IReadOnlyList<UserError> Errors { get; }

    public CreateAccountPayload(Account account)
    {
        Account = account;
        Errors = Array.Empty<UserError>();
    }

    public CreateAccountPayload(IReadOnlyList<UserError> errors)
    {
        Account = null;
        Errors = errors;
    }

    public CreateAccountPayload(UserError error)
    {
        Account = null;
        Errors = new[] { error };
    }
}

public record UserError(string Message, string Code, string[]? Path = null);
```

### 4. Create Application Command
Create `/src/BasicBudget.Application/Commands/CreateAccountCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Enums;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record CreateAccountCommand(
    string AccountNumber,
    string Name,
    AccountType AccountType,
    decimal InitialBalance,
    string Currency) : IRequest<OneOf<Account, DomainError>>;

public class CreateAccountCommandHandler 
    : IRequestHandler<CreateAccountCommand, OneOf<Account, DomainError>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OneOf<Account, DomainError>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Validate account number format
        var accountNumberResult = AccountNumber.Create(request.AccountNumber);
        if (accountNumberResult.IsT1)
        {
            return accountNumberResult.AsT1;
        }

        // Check for duplicate account number
        var existingAccount = await _accountRepository.GetByAccountNumberAsync(
            accountNumberResult.AsT0, 
            cancellationToken);
            
        if (existingAccount != null)
        {
            return new DomainError(
                "ACCOUNT_NUMBER_EXISTS", 
                $"Account number '{request.AccountNumber}' already exists");
        }

        // Create money value object
        var initialBalance = Money.Create(request.InitialBalance, request.Currency);

        // Create account
        var account = new Account(
            accountNumberResult.AsT0,
            request.Name,
            request.AccountType,
            initialBalance);

        // Save to repository
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account;
    }
}
```

### 5. Create Unit of Work Interface
Add to `/src/BasicBudget.Domain/Repositories/IUnitOfWork.cs`:
```csharp
namespace BasicBudget.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 6. Update Account Repository
Add to `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`:
```csharp
Task AddAsync(Account account, CancellationToken cancellationToken = default);
```

### 7. Add GraphQL Mutation
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Create a new account")]
public async Task<CreateAccountPayload> CreateAccountAsync(
    CreateAccountInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(input.AccountNumber))
    {
        return new CreateAccountPayload(
            new UserError("Account number is required", "VALIDATION_ERROR", new[] { "accountNumber" }));
    }

    if (string.IsNullOrWhiteSpace(input.Name))
    {
        return new CreateAccountPayload(
            new UserError("Account name is required", "VALIDATION_ERROR", new[] { "name" }));
    }

    var initialBalance = input.InitialBalance?.Amount ?? 0;
    var currency = input.InitialBalance?.Currency ?? "USD";

    var command = new CreateAccountCommand(
        input.AccountNumber,
        input.Name,
        input.AccountType,
        initialBalance,
        currency);

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        account => new CreateAccountPayload(account),
        error => new CreateAccountPayload(
            new UserError(error.Message, error.Code, new[] { "input" }))
    );
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

Test mutations:
```graphql
# Create account with initial balance
mutation {
  createAccount(input: {
    accountNumber: "1234567890"
    name: "Primary Checking"
    accountType: CHECKING
    initialBalance: {
      amount: 1000.00
      currency: "USD"
    }
  }) {
    account {
      id
      accountNumber
      name
      accountType
      currentBalance {
        amount
        currency
        formatted
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

# Create account without initial balance
mutation {
  createAccount(input: {
    accountNumber: "0987654321"
    name: "Savings Account"
    accountType: SAVINGS
  }) {
    account {
      id
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

# Test duplicate account number
mutation {
  createAccount(input: {
    accountNumber: "1234567890"  # Already exists
    name: "Duplicate Account"
    accountType: CHECKING
  }) {
    account {
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

### 9. Create Pull Request
```bash
git add .
git commit -m "feat: MUT-001 - Implement create account mutation"
git push origin story/MUT-001-create-account
gh pr create --title "MUT-001 - Create Account" --body "Implements GraphQL mutation to create new accounts with validation"
```

## Dependencies
- **Blocked By**: FOUND-002 (Error Handling)
- **Blocks**: MUT-002 (Update Account)

## Notes
- Account number uniqueness is enforced at the domain level
- Initial balance defaults to 0 if not provided
- Consider adding transaction to track initial deposit if balance > 0
- The payload pattern allows for both success and error responses

## Definition of Done
- [ ] CreateAccountCommand and handler created in Application layer
- [ ] Input and payload types created for GraphQL
- [ ] Mutation resolver added to Mutation class
- [ ] Account number uniqueness validation works
- [ ] Initial balance handling works correctly
- [ ] Solution builds without errors
- [ ] Manual testing confirms mutation works
- [ ] Error cases properly handled
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch