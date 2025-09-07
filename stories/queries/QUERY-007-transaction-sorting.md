# Story: QUERY-007 - Transaction Sorting

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement transaction sorting capabilities using HotChocolate's built-in sorting features. This enables users to sort transactions by various fields in ascending or descending order.

## Acceptance Criteria
- [ ] Sort by transaction date (ascending/descending)
- [ ] Sort by amount (ascending/descending)
- [ ] Sort by description alphabetically
- [ ] Sort by transaction type
- [ ] Multi-field sorting works correctly
- [ ] Default sort order is transaction date descending
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **HotChocolate Sorting**: Use [UseSorting] attribute for automatic sort generation
- **Domain Layer**: Transaction entity with all sortable properties
- **GraphQL Layer**: Configure sorting conventions
- **No Application Layer Changes**: HotChocolate handles sorting at GraphQL layer

### Implementation Location
- **GraphQL Configuration**: `src/BasicBudget.GraphQL/Program.cs`
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs` (already has [UseSorting])
- **Sort Configuration**: `src/BasicBudget.GraphQL/Sorting/TransactionSortType.cs` (optional)

### Schema Reference
```graphql
type Query {
  transactions(
    order: [TransactionSortInput!]
  ): [Transaction!]!
}

input TransactionSortInput {
  id: SortEnumType
  amount: MoneySortInput
  transactionDate: SortEnumType
  description: SortEnumType
  transactionType: SortEnumType
  createdAt: SortEnumType
  updatedAt: SortEnumType
  account: AccountSortInput
  category: CategorySortInput
}

input MoneySortInput {
  amount: SortEnumType
  currency: SortEnumType
}

enum SortEnumType {
  ASC
  DESC
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-007-transaction-sorting
```

### 2. Configure Custom Sort Type (Optional)
Create `/src/BasicBudget.GraphQL/Sorting/TransactionSortType.cs` for custom sort configuration:
```csharp
using BasicBudget.Domain.Entities;
using HotChocolate.Data.Sorting;

namespace BasicBudget.GraphQL.Sorting;

public class TransactionSortType : SortInputType<Transaction>
{
    protected override void Configure(ISortInputTypeDescriptor<Transaction> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        // Configure sortable fields
        descriptor.Field(t => t.Id)
            .Description("Sort by transaction ID");
            
        descriptor.Field(t => t.TransactionDate)
            .Description("Sort by transaction date");
            
        descriptor.Field(t => t.Amount)
            .Type<MoneySortType>()
            .Description("Sort by amount");
            
        descriptor.Field(t => t.Description)
            .Description("Sort by description");
            
        descriptor.Field(t => t.TransactionType)
            .Description("Sort by transaction type");
            
        descriptor.Field(t => t.CreatedAt)
            .Description("Sort by creation date");
            
        descriptor.Field(t => t.UpdatedAt)
            .Description("Sort by last update date");
            
        // Configure nested sorting
        descriptor.Field(t => t.Account)
            .Description("Sort by account properties");
            
        descriptor.Field(t => t.Category)
            .Description("Sort by category properties");
    }
}

public class MoneySortType : SortInputType<Money>
{
    protected override void Configure(ISortInputTypeDescriptor<Money> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(m => m.Amount)
            .Description("Sort by amount value");
            
        descriptor.Field(m => m.Currency)
            .Description("Sort by currency code");
    }
}
```

### 3. Configure Default Sorting
Create `/src/BasicBudget.GraphQL/Sorting/TransactionSortingMiddleware.cs`:
```csharp
using HotChocolate.Resolvers;

namespace BasicBudget.GraphQL.Sorting;

public class DefaultTransactionSortingMiddleware
{
    private readonly FieldDelegate _next;

    public DefaultTransactionSortingMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        // Check if sorting is already applied
        if (!context.ArgumentValue<object?>("order").HasValue)
        {
            // Apply default sorting by transaction date descending
            context.Arguments["order"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["transactionDate"] = "DESC"
                }
            };
        }

        await _next(context);
    }
}
```

### 4. Update GraphQL Configuration
Update `/src/BasicBudget.GraphQL/Program.cs`:
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    // Configure custom sort types if needed
    .AddType<TransactionSortType>()
    .AddType<MoneySortType>()
    // Configure sorting conventions
    .AddConvention<ISortConvention>(
        new SortConventionExtension(x => x
            .AddDefaults()
            .BindRuntimeType<Money, MoneySortType>()))
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());
```

### 5. Verify Existing Query
Confirm `/src/BasicBudget.GraphQL/Query.cs` has the sorting attribute:
```csharp
[GraphQLDescription("Retrieve transactions with pagination and filtering")]
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]  // This enables sorting
public IQueryable<Transaction> GetTransactionsAsync(
    [Service] ITransactionRepository transactionRepository)
{
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

Test queries:
```graphql
# Sort by transaction date descending (newest first)
query {
  transactions(
    order: [{ transactionDate: DESC }]
  ) {
    nodes {
      id
      transactionDate
      description
    }
  }
}

# Sort by amount ascending (smallest first)
query {
  transactions(
    order: [{ amount: { amount: ASC } }]
  ) {
    nodes {
      id
      amount {
        amount
        formatted
      }
      description
    }
  }
}

# Multi-field sorting: by type, then by date
query {
  transactions(
    order: [
      { transactionType: ASC }
      { transactionDate: DESC }
    ]
  ) {
    nodes {
      id
      transactionType
      transactionDate
      description
    }
  }
}

# Sort by nested field (account name)
query {
  transactions(
    order: [{ account: { name: ASC } }]
  ) {
    nodes {
      id
      account {
        name
      }
      description
    }
  }
}

# Combined with filtering and pagination
query {
  transactions(
    where: { transactionType: { eq: DEBIT } }
    order: [{ amount: { amount: DESC } }]
    first: 10
  ) {
    pageInfo {
      hasNextPage
      endCursor
    }
    nodes {
      id
      amount {
        formatted
      }
      transactionType
      description
    }
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-007 - Implement transaction sorting with HotChocolate"
git push origin story/QUERY-007-transaction-sorting
gh pr create --title "QUERY-007 - Transaction Sorting" --body "Implements comprehensive transaction sorting using HotChocolate's sorting features"
```

## Dependencies
- **Blocked By**: QUERY-004 (Transaction list query must exist)
- **Blocks**: None directly

## Notes
- HotChocolate automatically generates sort input types based on entity properties
- Multiple sort fields are applied in the order specified
- Sorting is applied at the database level for optimal performance
- The [UseSorting] attribute must be present on the resolver
- Consider implementing default sort order for better UX

## Definition of Done
- [ ] Custom sort types created if needed
- [ ] GraphQL configuration updated with sort conventions
- [ ] Sorting works for all transaction properties
- [ ] Multi-field sorting works correctly
- [ ] Nested sorting on Account and Category works
- [ ] Default sort order implemented (date descending)
- [ ] Solution builds without errors
- [ ] Manual testing confirms all sort scenarios work
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch