# Story: QUERY-002 - Get Account by ID

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve a single account by its ID. This query already partially exists but needs to be completed with proper error handling and full property resolution.

## Acceptance Criteria
- [ ] Query returns account when valid ID provided
- [ ] Returns null when account not found
- [ ] All account properties accessible including nested transactions
- [ ] Money type properly formatted
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Account entity already exists
  - Application Layer: GetAccountQuery already exists, may need updates
  - GraphQL Layer: Resolver exists in Query class, needs completion
- **Error Handling**: Use OneOf<Account, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetAccountQuery.cs` (existing)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs` (existing)
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Account.cs` (existing)

### Related Files
- Existing implementation: `/src/BasicBudget.GraphQL/Query.cs` (line 7-17)
- Application query: `/src/BasicBudget.Application/Queries/GetAccountQuery.cs`

### Schema Reference
```graphql
type Query {
  account(id: ID!): Account
}

type Account {
  id: ID!
  accountNumber: String!
  name: String!
  accountType: AccountType!
  currentBalance: Money!
  transactions(
    first: Int
    after: String
    where: TransactionFilterInput
    order: [TransactionSortInput!]
  ): TransactionConnection!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-002-get-account-by-id
```

### 2. Review Existing Implementation
Check `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
public async Task<Account?> GetAccountAsync(
    Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetAccountQuery(id), cancellationToken);
    return result.Match(
        account => account,
        error => null // Or throw a GraphQLException
    );
}
```

### 3. Update GraphQL Resolver
Enhance with proper field descriptions and error handling:
```csharp
[GraphQLDescription("Retrieve a single account by its ID")]
[UseProjection]
public async Task<Account?> GetAccountAsync(
    [GraphQLDescription("The unique identifier of the account")]
    [ID] Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetAccountQuery(id), cancellationToken);
    return result.Match(
        account => account,
        error => error.Code == "ACCOUNT_NOT_FOUND" 
            ? null 
            : throw new GraphQLException(error.Message)
    );
}
```

### 4. Add Transactions Field Resolver
Add to `/src/BasicBudget.GraphQL/Types/AccountType.cs` (create if needed):
```csharp
[ExtendObjectType(typeof(Account))]
public class AccountExtensions
{
    [GraphQLDescription("Get transactions for this account with optional filtering and pagination")]
    public async Task<TransactionConnection> GetTransactionsAsync(
        [Parent] Account account,
        int? first,
        string? after,
        TransactionFilterInput? where,
        IList<TransactionSortInput>? order,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Implementation will come in QUERY-004
        // For now, return empty connection
        return new TransactionConnection(
            new List<Transaction>(),
            new List<TransactionEdge>(),
            new PageInfo { HasNextPage = false, HasPreviousPage = false },
            0
        );
    }
}
```

### 5. Verify & Test
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
  account(id: "valid-guid-here") {
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

# Test with non-existent ID
query {
  account(id: "00000000-0000-0000-0000-000000000000") {
    id
  }
}
```

### 6. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-002 - Complete get account by ID query"
git push origin story/QUERY-002-get-account-by-id
gh pr create --title "QUERY-002 - Get Account by ID" --body "Completes the single account query with proper error handling and field resolution"
```

## Dependencies
- **Blocked By**: QUERY-001 (List All Accounts - for consistent patterns)
- **Blocks**: Transaction-related queries on accounts

## Notes
- This query partially exists but needs completion
- Consider adding DataLoader for N+1 query prevention in future
- The transactions field will be fully implemented in QUERY-004

## Definition of Done
- [ ] GetAccountAsync resolver has proper descriptions
- [ ] Error handling distinguishes between not found and errors
- [ ] Account type can resolve all fields
- [ ] Solution builds without errors
- [ ] Manual testing confirms query works correctly
- [ ] Returns null for non-existent accounts
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch