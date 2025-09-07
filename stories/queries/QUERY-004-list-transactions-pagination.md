# Story: QUERY-004 - List Transactions with Pagination

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve transactions with cursor-based pagination. This is the core transaction listing functionality that supports efficient browsing of large transaction sets.

## Acceptance Criteria
- [ ] Query returns paginated transactions
- [ ] Cursor-based pagination works correctly
- [ ] First/after parameters work for forward pagination
- [ ] PageInfo correctly indicates hasNextPage/hasPreviousPage
- [ ] Total count is accurate
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Transaction entity exists
  - Application Layer: Create GetTransactionsQuery with pagination
  - GraphQL Layer: Use TransactionConnection type from TYPE-001
- **Error Handling**: Use OneOf<TransactionConnection, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/GetTransactionsQuery.cs` (update existing)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Connection Types**: `src/BasicBudget.GraphQL/Types/PaginationTypes.cs` (from TYPE-001)

### Related Files
- Pagination types: `/src/BasicBudget.GraphQL/Types/PaginationTypes.cs`
- Repository: `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`

### Schema Reference
```graphql
type Query {
  transactions(
    accountId: ID
    categoryId: ID
    startDate: DateTime
    endDate: DateTime
    first: Int
    after: String
    where: TransactionFilterInput
    order: [TransactionSortInput!]
  ): TransactionConnection!
}

type TransactionConnection {
  nodes: [Transaction!]!
  edges: [TransactionEdge!]!
  pageInfo: PageInfo!
  totalCount: Int!
}

type TransactionEdge {
  node: Transaction!
  cursor: String!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-004-list-transactions-pagination
```

### 2. Application Layer (Optional)
With HotChocolate's built-in attributes, the application layer query becomes optional. HotChocolate will handle pagination, filtering, and sorting directly. However, if complex business logic is needed, you can still create:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.GraphQL.Types;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetTransactionsQuery(
    Guid? AccountId,
    Guid? CategoryId,
    DateTime? StartDate,
    DateTime? EndDate,
    int? First,
    string? After,
    TransactionFilterInput? Where,
    IList<TransactionSortInput>? Order
) : IRequest<OneOf<TransactionConnection, DomainError>>;

public class GetTransactionsQueryHandler 
    : IRequestHandler<GetTransactionsQuery, OneOf<TransactionConnection, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<OneOf<TransactionConnection, DomainError>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build query
            var query = await _transactionRepository.GetQueryableAsync();
            
            // Apply filters
            if (request.AccountId.HasValue)
            {
                query = query.Where(t => t.AccountId == request.AccountId.Value);
            }
            
            if (request.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == request.CategoryId.Value);
            }
            
            if (request.StartDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= request.StartDate.Value);
            }
            
            if (request.EndDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= request.EndDate.Value);
            }
            
            // Apply additional filters from Where input
            if (request.Where != null)
            {
                query = ApplyFilters(query, request.Where);
            }
            
            // Apply sorting
            query = ApplySorting(query, request.Order);
            
            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);
            
            // Apply cursor pagination
            if (!string.IsNullOrEmpty(request.After))
            {
                var cursor = CursorHelper.FromCursor<DateTime>(request.After);
                if (cursor.HasValue)
                {
                    query = query.Where(t => t.TransactionDate > cursor.Value);
                }
            }
            
            // Apply limit
            var limit = request.First ?? 10;
            var items = await query
                .Take(limit + 1) // Take one extra to check hasNextPage
                .ToListAsync(cancellationToken);
            
            var hasNextPage = items.Count > limit;
            if (hasNextPage)
            {
                items = items.Take(limit).ToList();
            }
            
            // Create edges
            var edges = items.Select(t => new TransactionEdge(
                t,
                CursorHelper.ToCursor(t.TransactionDate)
            )).ToList();
            
            // Create page info
            var pageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                HasPreviousPage = !string.IsNullOrEmpty(request.After),
                StartCursor = edges.FirstOrDefault()?.Cursor,
                EndCursor = edges.LastOrDefault()?.Cursor
            };
            
            return new TransactionConnection(items, edges, pageInfo, totalCount);
        }
        catch (Exception ex)
        {
            return new DomainError("TRANSACTIONS_FETCH_FAILED", 
                $"Failed to retrieve transactions: {ex.Message}");
        }
    }
    
    private IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query, 
        TransactionFilterInput filter)
    {
        // Implementation depends on filter structure
        // This will be completed in QUERY-006
        return query;
    }
    
    private IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        IList<TransactionSortInput>? sorting)
    {
        if (sorting == null || !sorting.Any())
        {
            return query.OrderByDescending(t => t.TransactionDate);
        }
        
        // Implementation will be completed in QUERY-007
        return query;
    }
}
```

### 3. Add Repository Method
Add to `/src/BasicBudget.Domain/Repositories/ITransactionRepository.cs`:
```csharp
IQueryable<Transaction> GetQueryable();
```

### 4. Implement Repository Method
Add to `/src/BasicBudget.Infrastructure/Persistence/TransactionRepository.cs`:
```csharp
public IQueryable<Transaction> GetQueryable()
{
    return _context.Transactions
        .Include(t => t.Account)
        .Include(t => t.Category)
        .AsQueryable();
}
```

### 5. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve transactions with pagination and filtering")]
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Transaction> GetTransactionsAsync(
    [Service] ITransactionRepository transactionRepository)
{
    // HotChocolate will automatically handle pagination, filtering, sorting, and projection
    // The repository should return IQueryable for HotChocolate to apply operations efficiently
    return transactionRepository.GetQueryable();
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
# First page
query {
  transactions(first: 10) {
    totalCount
    pageInfo {
      hasNextPage
      hasPreviousPage
      endCursor
    }
    edges {
      cursor
      node {
        id
        amount {
          amount
          currency
        }
        transactionDate
        description
      }
    }
  }
}

# Next page using cursor
query {
  transactions(first: 10, after: "cursor-from-previous-query") {
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
    nodes {
      id
      description
    }
  }
}

# Filtered by account and date range
query {
  transactions(
    accountId: "account-guid"
    startDate: "2025-01-01T00:00:00Z"
    endDate: "2025-01-31T23:59:59Z"
    first: 20
  ) {
    totalCount
    nodes {
      id
      amount {
        formatted
      }
      transactionDate
    }
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-004 - Implement transactions query with pagination"
git push origin story/QUERY-004-list-transactions-pagination
gh pr create --title "QUERY-004 - List Transactions with Pagination" --body "Implements cursor-based pagination for transactions query"
```

## Dependencies
- **Blocked By**: TYPE-001 (Pagination Types)
- **Blocks**: QUERY-006 (Transaction Filtering), QUERY-007 (Transaction Sorting)

## Notes
- Cursor encodes the transaction date for stable pagination
- Default sort is by transaction date descending
- Consider implementing DataLoader to prevent N+1 queries
- The where and order parameters will be fully implemented in QUERY-006 and QUERY-007

## Definition of Done
- [ ] GetTransactionsQuery handles pagination parameters
- [ ] Cursor-based pagination working correctly
- [ ] PageInfo accurately reflects pagination state
- [ ] Total count calculated correctly
- [ ] GraphQL resolver properly documented
- [ ] Solution builds without errors
- [ ] Manual testing confirms pagination works
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch