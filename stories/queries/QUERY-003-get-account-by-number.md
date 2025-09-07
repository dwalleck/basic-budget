# Story: QUERY-003 - Get Account by Number

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve an account by its account number. This provides an alternative lookup method using the business-meaningful account number instead of the technical ID.

## Acceptance Criteria
- [ ] Query returns account when valid account number provided
- [ ] Returns null when account not found
- [ ] Account number search is case-insensitive
- [ ] All account properties accessible
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: AccountNumber value object exists
  - Application Layer: Create GetAccountByNumberQuery
  - GraphQL Layer: Add resolver to Query class
- **Error Handling**: Use OneOf<Account, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetAccountByNumberQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Repository Method**: `src/BasicBudget.Domain/Repositories/IAccountRepository.cs`

### Related Files
- Value object: `/src/BasicBudget.Domain/ValueObjects/AccountNumber.cs`
- Repository: `/src/BasicBudget.Infrastructure/Persistence/AccountRepository.cs`

### Schema Reference
```graphql
type Query {
  accountByNumber(accountNumber: String!): Account
}

type Account {
  id: ID!
  accountNumber: String!
  name: String!
  accountType: AccountType!
  currentBalance: Money!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-003-get-account-by-number
```

### 2. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetAccountByNumberQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetAccountByNumberQuery(string AccountNumber) 
    : IRequest<OneOf<Account, DomainError>>;

public class GetAccountByNumberQueryHandler 
    : IRequestHandler<GetAccountByNumberQuery, OneOf<Account, DomainError>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountByNumberQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<OneOf<Account, DomainError>> Handle(
        GetAccountByNumberQuery request,
        CancellationToken cancellationToken)
    {
        // Validate account number format
        var accountNumberResult = AccountNumber.Create(request.AccountNumber);
        if (accountNumberResult.IsT1)
        {
            return accountNumberResult.AsT1;
        }

        var account = await _accountRepository.GetByAccountNumberAsync(
            accountNumberResult.AsT0, 
            cancellationToken);
            
        if (account == null)
        {
            return new DomainError(
                "ACCOUNT_NOT_FOUND", 
                $"Account with number '{request.AccountNumber}' not found");
        }

        return account;
    }
}
```

### 3. Add Repository Method
Add to `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`:
```csharp
Task<Account?> GetByAccountNumberAsync(
    AccountNumber accountNumber, 
    CancellationToken cancellationToken = default);
```

### 4. Implement Repository Method
Add to `/src/BasicBudget.Infrastructure/Persistence/AccountRepository.cs`:
```csharp
public async Task<Account?> GetByAccountNumberAsync(
    AccountNumber accountNumber, 
    CancellationToken cancellationToken = default)
{
    return await _context.Accounts
        .Include(a => a.Transactions)
        .FirstOrDefaultAsync(
            a => a.AccountNumber.Value.ToLower() == accountNumber.Value.ToLower(),
            cancellationToken);
}
```

### 5. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve an account by its account number")]
[UseProjection]
public async Task<Account?> GetAccountByNumberAsync(
    [GraphQLDescription("The account number to search for")]
    string accountNumber,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(accountNumber))
    {
        throw new GraphQLException("Account number is required");
    }

    var result = await mediator.Send(
        new GetAccountByNumberQuery(accountNumber), 
        cancellationToken);
        
    return result.Match(
        account => account,
        error => error.Code == "ACCOUNT_NOT_FOUND" 
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
  accountByNumber(accountNumber: "12345678") {
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
    updatedAt
  }
}

# Test with non-existent account number
query {
  accountByNumber(accountNumber: "INVALID") {
    id
  }
}

# Test with invalid format
query {
  accountByNumber(accountNumber: "") {
    id
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-003 - Implement get account by number query"
git push origin story/QUERY-003-get-account-by-number
gh pr create --title "QUERY-003 - Get Account by Number" --body "Implements account lookup by account number with validation"
```

## Dependencies
- **Blocked By**: QUERY-001, QUERY-002 (for consistent patterns)
- **Blocks**: None directly

## Notes
- Account number lookup should be case-insensitive
- Consider adding caching for frequently accessed accounts
- Ensure account number format validation matches business rules

## Definition of Done
- [ ] GetAccountByNumberQuery and handler created
- [ ] Repository method for account number lookup implemented
- [ ] GraphQL resolver added with proper validation
- [ ] Case-insensitive search working
- [ ] Solution builds without errors
- [ ] Manual testing confirms query works correctly
- [ ] Returns null for non-existent account numbers
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch