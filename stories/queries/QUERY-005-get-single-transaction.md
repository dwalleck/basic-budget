# Story: QUERY-005 - Get Single Transaction

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve a single transaction by its ID. This query provides detailed transaction information including related account and category data.

## Acceptance Criteria
- [ ] Query returns transaction when valid ID provided
- [ ] Returns null when transaction not found
- [ ] All transaction properties accessible including nested account and category
- [ ] Money type properly formatted
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Transaction entity already exists
  - Application Layer: Create GetTransactionQuery
  - GraphQL Layer: Add resolver to Query class
- **Error Handling**: Use OneOf<Transaction, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetTransactionQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Transaction.cs` (existing)

### Related Files
- Repository interface: `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`
- Repository implementation: `/src/BasicBudget.Infrastructure/Persistence/TransactionRepository.cs`

### Schema Reference
```graphql
type Query {
  transaction(id: ID!): Transaction
}

type Transaction {
  id: ID!
  amount: Money!
  transactionDate: DateTime!
  description: String
  transactionType: TransactionType!
  account: Account!
  accountId: ID!
  category: Category
  categoryId: ID
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-005-get-single-transaction
```

### 2. Create Application Query
Create `/src/BasicBudget.Application/Queries/GetTransactionQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetTransactionQuery(Guid Id) 
    : IRequest<OneOf<Transaction, DomainError>>;

public class GetTransactionQueryHandler 
    : IRequestHandler<GetTransactionQuery, OneOf<Transaction, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<OneOf<Transaction, DomainError>> Handle(
        GetTransactionQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(
            request.Id, 
            cancellationToken);
            
        if (transaction == null)
        {
            return new DomainError(
                "TRANSACTION_NOT_FOUND", 
                $"Transaction with ID '{request.Id}' not found");
        }

        return transaction;
    }
}
```

### 3. Add Repository Method
Add to `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`:
```csharp
Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
```

### 4. Implement Repository Method
Add to `/src/BasicBudget.Infrastructure/Persistence/TransactionRepository.cs`:
```csharp
public async Task<Transaction?> GetByIdAsync(
    Guid id, 
    CancellationToken cancellationToken = default)
{
    return await _context.Transactions
        .Include(t => t.Account)
        .Include(t => t.Category)
        .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
```

### 5. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve a single transaction by its ID")]
[UseProjection]
public async Task<Transaction?> GetTransactionAsync(
    [GraphQLDescription("The unique identifier of the transaction")]
    [ID] Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetTransactionQuery(id), cancellationToken);
    return result.Match(
        transaction => transaction,
        error => error.Code == "TRANSACTION_NOT_FOUND" 
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
  transaction(id: "valid-guid-here") {
    id
    amount {
      amount
      currency
      formatted
    }
    transactionDate
    description
    transactionType
    account {
      id
      name
      accountNumber
    }
    category {
      id
      name
    }
    createdAt
    updatedAt
  }
}

# Test with non-existent ID
query {
  transaction(id: "00000000-0000-0000-0000-000000000000") {
    id
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-005 - Implement get single transaction query"
git push origin story/QUERY-005-get-single-transaction
gh pr create --title "QUERY-005 - Get Single Transaction" --body "Implements query to retrieve a single transaction by ID with related data"
```

## Dependencies
- **Blocked By**: QUERY-004 (for consistent transaction patterns)
- **Blocks**: None directly

## Notes
- Consider adding DataLoader for N+1 query prevention
- The [UseProjection] attribute enables efficient field selection
- Account and Category are eagerly loaded to avoid lazy loading issues

## Definition of Done
- [ ] GetTransactionQuery and handler created in Application layer
- [ ] GetByIdAsync method added to ITransactionRepository and implementation
- [ ] GraphQL resolver added with proper descriptions
- [ ] Error handling distinguishes between not found and errors
- [ ] Solution builds without errors
- [ ] Manual testing confirms query works correctly
- [ ] Returns null for non-existent transactions
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch