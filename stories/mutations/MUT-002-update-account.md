# Story: MUT-002 - Update Account

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL mutation to update an existing account. This mutation allows modification of account name and account type while preserving the account number and balance history.

## Acceptance Criteria
- [ ] Mutation updates account name and type
- [ ] Account number cannot be modified (immutable)
- [ ] Account balance is preserved during update
- [ ] Validates account exists before updating
- [ ] Returns updated account on success
- [ ] Returns validation errors on failure
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Account entity with update methods
  - Application Layer: Create UpdateAccountCommand via MediatR
  - GraphQL Layer: Add mutation resolver
- **Error Handling**: Use OneOf<Account, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Command**: `src/BasicBudget.Application/Commands/UpdateAccountCommand.cs` (new)
- **GraphQL Mutation**: `src/BasicBudget.GraphQL/Mutation.cs`
- **Input Type**: `src/BasicBudget.GraphQL/Inputs/UpdateAccountInput.cs` (new)

### Schema Reference
```graphql
type Mutation {
  updateAccount(input: UpdateAccountInput!): UpdateAccountPayload!
}

input UpdateAccountInput {
  accountId: ID!
  name: String!
  accountType: AccountType!
}

type UpdateAccountPayload {
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
git checkout -b story/MUT-002-update-account
```

### 2. Create Input Type
Create `/src/BasicBudget.GraphQL/Inputs/UpdateAccountInput.cs`:
```csharp
using BasicBudget.Domain.Enums;

namespace BasicBudget.GraphQL.Inputs;

public record UpdateAccountInput(
    Guid AccountId,
    string Name,
    AccountType AccountType);
```

### 3. Create Payload Type
Create `/src/BasicBudget.GraphQL/Payloads/UpdateAccountPayload.cs`:
```csharp
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Payloads;

public class UpdateAccountPayload
{
    public Account? Account { get; }
    public IReadOnlyList<UserError> Errors { get; }

    public UpdateAccountPayload(Account account)
    {
        Account = account;
        Errors = Array.Empty<UserError>();
    }

    public UpdateAccountPayload(IReadOnlyList<UserError> errors)
    {
        Account = null;
        Errors = errors;
    }

    public UpdateAccountPayload(UserError error)
    {
        Account = null;
        Errors = new[] { error };
    }
}
```

### 4. Create Application Command
Create `/src/BasicBudget.Application/Commands/UpdateAccountCommand.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Enums;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Commands;

public record UpdateAccountCommand(
    Guid AccountId,
    string Name,
    AccountType AccountType) : IRequest<OneOf<Account, DomainError>>;

public class UpdateAccountCommandHandler 
    : IRequestHandler<UpdateAccountCommand, OneOf<Account, DomainError>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OneOf<Account, DomainError>> Handle(
        UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Validate account exists
        var account = await _accountRepository.GetByIdAsync(
            request.AccountId, 
            cancellationToken);
            
        if (account == null)
        {
            return new DomainError(
                "ACCOUNT_NOT_FOUND", 
                $"Account with ID '{request.AccountId}' not found");
        }

        // Validate name is not empty
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new DomainError(
                "INVALID_NAME", 
                "Account name cannot be empty");
        }

        // Update account properties
        var updateResult = account.UpdateDetails(request.Name, request.AccountType);
        if (updateResult.IsT1)
        {
            return updateResult.AsT1;
        }

        // Save changes
        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account;
    }
}
```

### 5. Update Domain Entity
Add to `/src/BasicBudget.Domain/Entities/Account.cs`:
```csharp
public OneOf<Success, DomainError> UpdateDetails(string name, AccountType accountType)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return new DomainError("INVALID_NAME", "Account name cannot be empty");
    }

    Name = name;
    AccountType = accountType;
    UpdatedAt = DateTime.UtcNow;
    
    return new Success();
}
```

### 6. Update Account Repository
Add to `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`:
```csharp
Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
```

### 7. Add GraphQL Mutation
Add to `/src/BasicBudget.GraphQL/Mutation.cs`:
```csharp
[GraphQLDescription("Update an existing account")]
public async Task<UpdateAccountPayload> UpdateAccountAsync(
    UpdateAccountInput input,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(input.Name))
    {
        return new UpdateAccountPayload(
            new UserError("Account name is required", "VALIDATION_ERROR", new[] { "name" }));
    }

    var command = new UpdateAccountCommand(
        input.AccountId,
        input.Name,
        input.AccountType);

    var result = await mediator.Send(command, cancellationToken);

    return result.Match(
        account => new UpdateAccountPayload(account),
        error => new UpdateAccountPayload(
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
# Update account details
mutation {
  updateAccount(input: {
    accountId: "existing-account-guid"
    name: "Updated Checking Account"
    accountType: CHECKING
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
      updatedAt
    }
    errors {
      message
      code
      path
    }
  }
}

# Test with invalid account ID
mutation {
  updateAccount(input: {
    accountId: "non-existent-guid"
    name: "Test Account"
    accountType: SAVINGS
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

# Test with empty name
mutation {
  updateAccount(input: {
    accountId: "existing-account-guid"
    name: ""
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
git commit -m "feat: MUT-002 - Implement update account mutation"
git push origin story/MUT-002-update-account
gh pr create --title "MUT-002 - Update Account" --body "Implements GraphQL mutation to update existing accounts with validation"
```

## Dependencies
- **Blocked By**: MUT-001 (Create Account)
- **Blocks**: None

## Notes
- Account number is immutable and cannot be changed
- Account balance is preserved during updates
- Only name and account type can be modified
- UpdatedAt timestamp is automatically set

## Definition of Done
- [ ] UpdateAccountCommand and handler created in Application layer
- [ ] Input and payload types created for GraphQL
- [ ] Mutation resolver added to Mutation class
- [ ] Account existence validation works
- [ ] Name validation prevents empty values
- [ ] Solution builds without errors
- [ ] Manual testing confirms mutation works
- [ ] Error cases properly handled
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch