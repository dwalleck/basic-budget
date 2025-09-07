# Story: QUERY-001 - List All Accounts

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve all accounts in the system. This is a simple list query without pagination that returns all accounts with their basic properties.

## Acceptance Criteria
- [ ] Query returns all accounts from the database
- [ ] Each account includes: id, accountNumber, name, accountType, currentBalance, createdAt, updatedAt
- [ ] Money type properly formatted with amount, currency, and formatted string
- [ ] Empty array returned when no accounts exist
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Account entity already exists
  - Application Layer: Create GetAccountsQuery and handler via MediatR
  - GraphQL Layer: Add resolver to Query class
- **Error Handling**: Use OneOf<IEnumerable<Account>, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetAccountsQuery.cs`
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Account.cs` (existing)
- **Repository**: `src/BasicBudget.Domain/Repositories/IAccountRepository.cs` (existing)

### Related Files
- Domain entity: `/src/BasicBudget.Domain/Entities/Account.cs`
- Repository interface: `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`
- Repository implementation: `/src/BasicBudget.Infrastructure/Persistence/AccountRepository.cs`
- Existing single account query: `/src/BasicBudget.Application/Queries/GetAccountQuery.cs`

### Schema Reference
```graphql
type Query {
  accounts: [Account!]!
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

type Money {
  amount: Decimal!
  currency: String!
  formatted: String!
}

enum AccountType {
  CHECKING
  SAVINGS
  CREDIT_CARD
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-001-list-all-accounts
```

### 2. Application Layer
Create `/src/BasicBudget.Application/Queries/GetAccountsQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetAccountsQuery() : IRequest<OneOf<IEnumerable<Account>, DomainError>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, OneOf<IEnumerable<Account>, DomainError>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<OneOf<IEnumerable<Account>, DomainError>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _accountRepository.GetAllAsync(cancellationToken);
            return OneOf<IEnumerable<Account>, DomainError>.FromT0(accounts);
        }
        catch (Exception ex)
        {
            return new DomainError("ACCOUNTS_FETCH_FAILED", $"Failed to retrieve accounts: {ex.Message}");
        }
    }
}
```

### 3. Update Repository Interface
Add to `/src/BasicBudget.Domain/Repositories/IAccountRepository.cs`:
```csharp
Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default);
```

### 4. Implement Repository Method
Add to `/src/BasicBudget.Infrastructure/Persistence/AccountRepository.cs`:
```csharp
public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await _context.Accounts
        .Include(a => a.Transactions)
        .ToListAsync(cancellationToken);
}
```

### 5. GraphQL Layer
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve all accounts in the system")]
[UseProjection]
[UseFiltering]
[UseSorting]
public async Task<IQueryable<Account>> GetAccountsAsync(
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetAccountsQuery(), cancellationToken);
    return result.Match(
        accounts => accounts.AsQueryable(),
        error => throw new GraphQLException(error.Message)
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
  accounts {
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
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-001 - Implement list all accounts query"
git push origin story/QUERY-001-list-all-accounts
gh pr create --title "QUERY-001 - List All Accounts" --body "Implements GraphQL query to retrieve all accounts following hexagonal architecture"
```

## Dependencies
- **Blocked By**: None (foundation query)
- **Blocks**: Advanced account queries with filtering

## Notes
- This is a foundational query that establishes the pattern for other list queries
- Consider adding pagination in a future story if the account list grows large
- The Money type serialization should already be configured from MUT-001

## Definition of Done
- [ ] GetAccountsQuery and handler created in Application layer
- [ ] GetAllAsync method added to IAccountRepository and implementation
- [ ] GraphQL resolver added to Query class
- [ ] Solution builds without errors
- [ ] Manual testing confirms query returns accounts correctly
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch