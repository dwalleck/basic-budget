# Story: TYPE-002 - Filter Input Types

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Configure and customize HotChocolate filter input types for the application. This story establishes the filtering conventions and custom filter types needed for complex filtering scenarios across all entities.

## Acceptance Criteria
- [ ] Configure filter conventions for all domain types
- [ ] Create custom filter types for value objects (Money, AccountNumber)
- [ ] Configure date range filtering helpers
- [ ] Configure text search filtering options
- [ ] Enum filtering works correctly
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **HotChocolate Filtering**: Configure filtering conventions and custom types
- **Domain Layer**: No changes needed
- **GraphQL Layer**: Configure filter types and conventions
- **Type Safety**: Ensure all filter types are strongly typed

### Implementation Location
- **Filter Types**: `src/BasicBudget.GraphQL/Filters/` directory
- **Configuration**: `src/BasicBudget.GraphQL/Program.cs`
- **Convention**: `src/BasicBudget.GraphQL/Filters/CustomFilteringConvention.cs`

### Schema Reference
```graphql
# Auto-generated filter types
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

input DecimalOperationFilterInput {
  eq: Decimal
  neq: Decimal
  in: [Decimal!]
  nin: [Decimal!]
  gt: Decimal
  ngt: Decimal
  gte: Decimal
  ngte: Decimal
  lt: Decimal
  nlt: Decimal
  lte: Decimal
  nlte: Decimal
}

input DateTimeOperationFilterInput {
  eq: DateTime
  neq: DateTime
  in: [DateTime!]
  nin: [DateTime!]
  gt: DateTime
  ngt: DateTime
  gte: DateTime
  ngte: DateTime
  lt: DateTime
  nlt: DateTime
  lte: DateTime
  nlte: DateTime
}

# Custom filter types
input MoneyFilterInput {
  amount: DecimalOperationFilterInput
  currency: StringOperationFilterInput
  between: MoneyRangeInput
}

input MoneyRangeInput {
  min: Decimal!
  max: Decimal!
  currency: String
}

input DateRangeFilterInput {
  start: DateTime!
  end: DateTime!
  inclusive: Boolean
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/TYPE-002-filter-input-types
```

### 2. Create Custom Filtering Convention
Create `/src/BasicBudget.GraphQL/Filters/CustomFilteringConvention.cs`:
```csharp
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace BasicBudget.GraphQL.Filters;

public class CustomFilteringConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        
        // Configure naming
        descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
        descriptor.Operation(DefaultFilterOperations.NotEquals).Name("neq");
        descriptor.Operation(DefaultFilterOperations.Contains).Name("contains");
        descriptor.Operation(DefaultFilterOperations.NotContains).Name("ncontains");
        descriptor.Operation(DefaultFilterOperations.In).Name("in");
        descriptor.Operation(DefaultFilterOperations.NotIn).Name("nin");
        descriptor.Operation(DefaultFilterOperations.StartsWith).Name("startsWith");
        descriptor.Operation(DefaultFilterOperations.NotStartsWith).Name("nstartsWith");
        descriptor.Operation(DefaultFilterOperations.EndsWith).Name("endsWith");
        descriptor.Operation(DefaultFilterOperations.NotEndsWith).Name("nendsWith");
        descriptor.Operation(DefaultFilterOperations.GreaterThan).Name("gt");
        descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Name("ngt");
        descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Name("gte");
        descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Name("ngte");
        descriptor.Operation(DefaultFilterOperations.LowerThan).Name("lt");
        descriptor.Operation(DefaultFilterOperations.NotLowerThan).Name("nlt");
        descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Name("lte");
        descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Name("nlte");
        
        // Add custom operations
        descriptor.Operation(CustomFilterOperations.Between).Name("between");
        descriptor.Operation(CustomFilterOperations.DateRange).Name("dateRange");
        descriptor.Operation(CustomFilterOperations.Search).Name("search");
        
        // Configure default behavior
        descriptor.ArgumentName("where");
        descriptor.Provider<QueryableFilterProvider>();
    }
}

public static class CustomFilterOperations
{
    public const int Between = 1000;
    public const int DateRange = 1001;
    public const int Search = 1002;
}
```

### 3. Create Money Filter Type
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
            .Type<DecimalOperationFilterInputType>()
            .Description("Filter by amount value");
            
        descriptor.Field(m => m.Currency)
            .Type<StringOperationFilterInputType>()
            .Description("Filter by currency code");
            
        descriptor.Field("between")
            .Type<MoneyRangeFilterType>()
            .Description("Filter by amount range");
    }
}

public class MoneyRangeFilterType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("MoneyRangeInput");
        
        descriptor.Field("min")
            .Type<NonNullType<DecimalType>>()
            .Description("Minimum amount (inclusive)");
            
        descriptor.Field("max")
            .Type<NonNullType<DecimalType>>()
            .Description("Maximum amount (inclusive)");
            
        descriptor.Field("currency")
            .Type<StringType>()
            .Description("Currency code (default: USD)");
    }
}
```

### 4. Create Date Range Filter Type
Create `/src/BasicBudget.GraphQL/Filters/DateRangeFilterType.cs`:
```csharp
using HotChocolate.Data.Filters;

namespace BasicBudget.GraphQL.Filters;

public class DateRangeFilterType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("DateRangeFilterInput");
        
        descriptor.Field("start")
            .Type<NonNullType<DateTimeType>>()
            .Description("Start date of the range");
            
        descriptor.Field("end")
            .Type<NonNullType<DateTimeType>>()
            .Description("End date of the range");
            
        descriptor.Field("inclusive")
            .Type<BooleanType>()
            .DefaultValue(true)
            .Description("Whether the range is inclusive (default: true)");
    }
}

public static class DateRangeFilterExtensions
{
    public static IQueryable<T> ApplyDateRange<T>(
        this IQueryable<T> query,
        DateTime start,
        DateTime end,
        Func<T, DateTime> dateSelector,
        bool inclusive = true)
    {
        if (inclusive)
        {
            return query.Where(x => dateSelector(x) >= start && dateSelector(x) <= end);
        }
        else
        {
            return query.Where(x => dateSelector(x) > start && dateSelector(x) < end);
        }
    }
}
```

### 5. Create Account Number Filter Type
Create `/src/BasicBudget.GraphQL/Filters/AccountNumberFilterType.cs`:
```csharp
using BasicBudget.Domain.ValueObjects;
using HotChocolate.Data.Filters;

namespace BasicBudget.GraphQL.Filters;

public class AccountNumberFilterType : FilterInputType<AccountNumber>
{
    protected override void Configure(IFilterInputTypeDescriptor<AccountNumber> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(a => a.Value)
            .Name("value")
            .Type<StringOperationFilterInputType>()
            .Description("Filter by account number value");
            
        descriptor.Field("matches")
            .Type<StringType>()
            .Description("Pattern match for account number (supports wildcards)");
    }
}
```

### 6. Create Search Filter Handler
Create `/src/BasicBudget.GraphQL/Filters/SearchFilterHandler.cs`:
```csharp
using HotChocolate.Data.Filters;
using System.Linq.Expressions;

namespace BasicBudget.GraphQL.Filters;

public class SearchFilterHandler : FilterOperationHandler
{
    public override bool CanHandle(
        IFilterOperationField field,
        IFilterInputTypeDescriptor descriptor)
    {
        return field.Id == CustomFilterOperations.Search;
    }

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object parsedValue)
    {
        if (parsedValue is string searchTerm && !string.IsNullOrWhiteSpace(searchTerm))
        {
            // Implement full-text search logic
            var parameter = Expression.Parameter(context.RuntimeType);
            
            // Search multiple string fields
            var searchableProperties = context.RuntimeType
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string));
            
            Expression? combinedExpression = null;
            
            foreach (var property in searchableProperties)
            {
                var propertyAccess = Expression.Property(parameter, property);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var searchExpression = Expression.Call(
                    propertyAccess,
                    containsMethod!,
                    Expression.Constant(searchTerm));
                
                combinedExpression = combinedExpression == null
                    ? searchExpression
                    : Expression.OrElse(combinedExpression, searchExpression);
            }
            
            return combinedExpression ?? Expression.Constant(false);
        }
        
        return Expression.Constant(true);
    }
}
```

### 7. Update GraphQL Configuration
Update `/src/BasicBudget.GraphQL/Program.cs`:
```csharp
using BasicBudget.GraphQL.Filters;

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    // Configure filtering with custom convention
    .AddFiltering<CustomFilteringConvention>()
    .AddSorting()
    .AddProjections()
    // Register custom filter types
    .AddType<MoneyFilterType>()
    .AddType<AccountNumberFilterType>()
    .AddType<DateRangeFilterType>()
    .AddType<MoneyRangeFilterType>()
    // Register custom filter handlers
    .AddSingleton<SearchFilterHandler>()
    // Configure type bindings
    .BindRuntimeType<Money, MoneyFilterType>()
    .BindRuntimeType<AccountNumber, AccountNumberFilterType>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());
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

Test queries:
```graphql
# Filter with money range
query {
  transactions(
    where: {
      amount: {
        between: { min: 100, max: 500, currency: "USD" }
      }
    }
  ) {
    nodes {
      amount {
        formatted
      }
    }
  }
}

# Filter with date range
query {
  transactions(
    where: {
      transactionDate: {
        gte: "2025-01-01T00:00:00Z"
        lte: "2025-01-31T23:59:59Z"
      }
    }
  ) {
    nodes {
      transactionDate
    }
  }
}

# Complex filter with AND/OR
query {
  accounts(
    where: {
      or: [
        { accountType: { eq: CHECKING } }
        { 
          currentBalance: { 
            amount: { gt: 10000 } 
          }
        }
      ]
    }
  ) {
    accountNumber
    accountType
    currentBalance {
      formatted
    }
  }
}

# Search filter
query {
  transactions(
    where: {
      search: "grocery"
    }
  ) {
    nodes {
      description
    }
  }
}
```

### 9. Create Pull Request
```bash
git add .
git commit -m "feat: TYPE-002 - Configure filter input types"
git push origin story/TYPE-002-filter-input-types
gh pr create --title "TYPE-002 - Filter Input Types" --body "Configures HotChocolate filtering with custom types and conventions"
```

## Dependencies
- **Blocked By**: TYPE-001 (Pagination Types)
- **Blocks**: QUERY-006 (Transaction Filtering)

## Notes
- Custom filter types enable domain-specific filtering logic
- The convention ensures consistent naming across all filter operations
- Search functionality can be extended to use full-text search if needed
- Consider adding caching for frequently used filter combinations

## Definition of Done
- [ ] Custom filtering convention created and configured
- [ ] Money filter type with range support implemented
- [ ] Date range filter type implemented
- [ ] Account number filter type implemented
- [ ] Search filter handler implemented
- [ ] GraphQL configuration updated
- [ ] All filter types properly registered
- [ ] Solution builds without errors
- [ ] Manual testing confirms all filter types work
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch