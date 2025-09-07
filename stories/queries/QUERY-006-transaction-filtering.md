# Story: QUERY-006 - Transaction Filtering

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement advanced transaction filtering capabilities using HotChocolate's built-in filtering features. This story enhances the existing transactions query with comprehensive filter options.

## Acceptance Criteria
- [ ] Filter by amount range (min/max)
- [ ] Filter by transaction type (DEBIT/CREDIT)
- [ ] Filter by description (contains, starts with, ends with)
- [ ] Filter by date range
- [ ] Filter by category (including null categories)
- [ ] Complex filter combinations work correctly
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **HotChocolate Filtering**: Use [UseFiltering] attribute for automatic filter generation
- **Domain Layer**: Transaction entity with all filterable properties
- **GraphQL Layer**: Configure filtering conventions
- **No Application Layer Changes**: HotChocolate handles filtering at GraphQL layer

### Implementation Location
- **GraphQL Configuration**: `src/BasicBudget.GraphQL/Program.cs`
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs` (already has [UseFiltering])
- **Filter Configuration**: `src/BasicBudget.GraphQL/Filters/TransactionFilterType.cs` (new)

### Schema Reference
```graphql
type Query {
  transactions(
    where: TransactionFilterInput
  ): [Transaction!]!
}

input TransactionFilterInput {
  and: [TransactionFilterInput!]
  or: [TransactionFilterInput!]
  id: UuidOperationFilterInput
  amount: MoneyFilterInput
  transactionDate: DateTimeOperationFilterInput
  description: StringOperationFilterInput
  transactionType: TransactionTypeOperationFilterInput
  accountId: UuidOperationFilterInput
  categoryId: UuidOperationFilterInput
  account: AccountFilterInput
  category: CategoryFilterInput
}

input MoneyFilterInput {
  amount: DecimalOperationFilterInput
  currency: StringOperationFilterInput
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String
  neq: String
  contains: String
  ncontains: String
  in: [String!]
  nin: [String!]
  startsWith: String
  nstartsWith: String
  endsWith: String
  nendsWith: String
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-006-transaction-filtering
```

### 2. Configure Custom Filter Type (Optional)
Create `/src/BasicBudget.GraphQL/Filters/TransactionFilterType.cs` for custom filter logic:
```csharp
using BasicBudget.Domain.Entities;
using HotChocolate.Data.Filters;

namespace BasicBudget.GraphQL.Filters;

public class TransactionFilterType : FilterInputType<Transaction>
{
    protected override void Configure(IFilterInputTypeDescriptor<Transaction> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        // Configure filterable fields
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Amount);
        descriptor.Field(t => t.TransactionDate);
        descriptor.Field(t => t.Description);
        descriptor.Field(t => t.TransactionType);
        descriptor.Field(t => t.AccountId);
        descriptor.Field(t => t.CategoryId);
        
        // Configure nested filtering
        descriptor.Field(t => t.Account);
        descriptor.Field(t => t.Category);
        
        // Add custom filter for amount range
        descriptor.Field("amountRange")
            .Type<AmountRangeFilterType>()
            .Description("Filter by amount range");
    }
}

public class AmountRangeFilterType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Field("min")
            .Type<DecimalType>()
            .Description("Minimum amount");
            
        descriptor.Field("max")
            .Type<DecimalType>()
            .Description("Maximum amount");
    }
}
```

### 3. Configure Money Filter Type
Create `/src/BasicBudget.GraphQL/Filters/MoneyFilterType.cs`:
```csharp
using BasicBudget.Domain.ValueObjects;
using HotChocolate.Data.Filters;

namespace BasicBudget.GraphQL.Filters;

public class MoneyFilterType : FilterInputType<Money>
{
    protected override void Configure(IFilterInputTypeDescriptor<Money> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(m => m.Amount)
            .Description("Filter by amount value");
            
        descriptor.Field(m => m.Currency)
            .Description("Filter by currency code");
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
    // Configure custom filter types
    .AddType<TransactionFilterType>()
    .AddType<MoneyFilterType>()
    // Configure filter conventions
    .AddConvention<IFilterConvention>(
        new FilterConventionExtension(x => x
            .AddDefaults()
            .BindRuntimeType<decimal, DecimalOperationFilterInputType>()
            .BindRuntimeType<Money, MoneyFilterType>()))
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());
```

### 5. Verify Existing Query
Confirm `/src/BasicBudget.GraphQL/Query.cs` has the filtering attribute:
```csharp
[GraphQLDescription("Retrieve transactions with pagination and filtering")]
[UsePaging]
[UseProjection]
[UseFiltering]  // This enables filtering
[UseSorting]
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
# Filter by amount range
query {
  transactions(
    where: {
      amount: {
        amount: { gte: 100, lte: 500 }
      }
    }
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

# Filter by transaction type and date range
query {
  transactions(
    where: {
      and: [
        { transactionType: { eq: DEBIT } }
        { transactionDate: { gte: "2025-01-01T00:00:00Z" } }
        { transactionDate: { lte: "2025-01-31T23:59:59Z" } }
      ]
    }
  ) {
    nodes {
      id
      transactionType
      transactionDate
      amount {
        formatted
      }
    }
  }
}

# Filter by description contains
query {
  transactions(
    where: {
      description: { contains: "grocery" }
    }
  ) {
    nodes {
      id
      description
    }
  }
}

# Complex filter with OR conditions
query {
  transactions(
    where: {
      or: [
        { categoryId: { eq: null } }
        { amount: { amount: { gt: 1000 } } }
        { description: { startsWith: "REFUND" } }
      ]
    }
  ) {
    nodes {
      id
      description
      categoryId
      amount {
        formatted
      }
    }
  }
}
```

### 7. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-006 - Implement transaction filtering with HotChocolate"
git push origin story/QUERY-006-transaction-filtering
gh pr create --title "QUERY-006 - Transaction Filtering" --body "Implements comprehensive transaction filtering using HotChocolate's filtering features"
```

## Dependencies
- **Blocked By**: QUERY-004 (Transaction list query must exist)
- **Blocks**: None directly

## Notes
- HotChocolate automatically generates filter input types based on entity properties
- Custom filter types can be created for complex scenarios
- The [UseFiltering] attribute must be present on the resolver
- Filtering is applied at the database level for optimal performance

## Definition of Done
- [ ] Custom filter types created if needed
- [ ] GraphQL configuration updated with filter conventions
- [ ] Filtering works for all transaction properties
- [ ] Complex AND/OR filter combinations work
- [ ] Nested filtering on Account and Category works
- [ ] Solution builds without errors
- [ ] Manual testing confirms all filter scenarios work
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch