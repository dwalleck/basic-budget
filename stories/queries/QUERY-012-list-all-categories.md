# Story: QUERY-012 - List All Categories

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve all categories in the system with support for filtering, sorting, and hierarchical display. This query provides access to the complete category tree including parent-child relationships, metadata, and usage statistics.

## Acceptance Criteria
- [ ] Query returns all categories in the system
- [ ] Supports hierarchical display with parent-child relationships
- [ ] Includes category metadata (color, description, system-generated flag)
- [ ] Shows usage statistics (transaction count, budget usage)
- [ ] Filtering automatically available through HotChocolate attributes
- [ ] Sorting automatically available through HotChocolate attributes
- [ ] Proper error handling for repository failures
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Category entity (existing)
  - Application Layer: Create ListAllCategoriesQuery and handler via MediatR
  - GraphQL Layer: Add resolver with HotChocolate attributes for filtering/sorting
- **Error Handling**: Use OneOf<IEnumerable<Category>, DomainError> pattern
- **Dependency Direction**: GraphQL → Application → Domain

### Implementation Location
- **Application Query**: `src/BasicBudget.Application/Queries/ListAllCategoriesQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Category.cs` (existing)
- **Repository**: `src/BasicBudget.Domain/Repositories/ICategoryRepository.cs` (existing)

### Related Files
- Category entity: `/src/BasicBudget.Domain/Entities/Category.cs`
- Repository interface: `/src/BasicBudget.Domain/Repositories/ICategoryRepository.cs`
- Repository implementation: `/src/BasicBudget.Infrastructure/Persistence/CategoryRepository.cs`

### Schema Reference
```graphql
type Query {
  categories: [Category!]!
}

type Category {
  id: ID!
  name: String!
  description: String
  color: String
  isSystemGenerated: Boolean!
  isRoot: Boolean!
  isLeaf: Boolean!
  depth: Int!
  fullPath: String!
  transactionCount: Int!
  parentCategoryId: ID
  parentCategory: Category
  children: [Category!]!
  transactions: [Transaction!]!
  budgetCategories: [BudgetCategory!]!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

**Note: Filtering and sorting are automatically available through HotChocolate's [UseFiltering] and [UseSorting] attributes. Users can filter by any property (e.g., isSystemGenerated, isRoot, name, color, etc.) and sort by any field without additional code.**

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-012-list-all-categories
```

### 2. Create Application Query
Create `/src/BasicBudget.Application/Queries/ListAllCategoriesQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record ListAllCategoriesQuery() : IRequest<OneOf<IEnumerable<Category>, DomainError>>;

public class ListAllCategoriesQueryHandler : IRequestHandler<ListAllCategoriesQuery, OneOf<IEnumerable<Category>, DomainError>>
{
    private readonly ICategoryRepository _categoryRepository;

    public ListAllCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<OneOf<IEnumerable<Category>, DomainError>> Handle(
        ListAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _categoryRepository.GetHierarchyAsync(cancellationToken);
            return OneOf<IEnumerable<Category>, DomainError>.FromT0(categories);
        }
        catch (Exception ex)
        {
            return new DomainError("CATEGORIES_FETCH_FAILED", $"Failed to retrieve categories: {ex.Message}");
        }
    }
}
```

### 3. Add GraphQL Resolver
Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve all categories with hierarchical structure")]
[UseProjection]
[UseFiltering]
[UseSorting]
public async Task<IEnumerable<Category>> GetCategoriesAsync(
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new ListAllCategoriesQuery(), cancellationToken);
    return result.Match(
        categories => categories,
        error => throw new GraphQLException(error.Message)
    );
}
```

### 4. Add Category Extensions for Computed Properties
Create `/src/BasicBudget.GraphQL/Types/CategoryExtensions.cs`:
```csharp
using BasicBudget.Domain.Entities;
using HotChocolate.Types;

namespace BasicBudget.GraphQL.Types;

[ExtendObjectType(typeof(Category))]
public class CategoryExtensions
{
    [GraphQLDescription("Whether this category is a root category (has no parent)")]
    public bool GetIsRoot([Parent] Category category) => category.IsRoot;

    [GraphQLDescription("Whether this category is a leaf category (has no children)")]
    public bool GetIsLeaf([Parent] Category category) => category.IsLeaf;

    [GraphQLDescription("The depth of this category in the hierarchy (1 for root categories)")]
    public int GetDepth([Parent] Category category) => category.GetDepth();

    [GraphQLDescription("The full path of this category from root (e.g., 'Food > Restaurants > Fast Food')")]
    public string GetFullPath([Parent] Category category) => category.GetFullPath();

    [GraphQLDescription("The number of transactions associated with this category")]
    public int GetTransactionCount([Parent] Category category) => category.TransactionCount;

    [GraphQLDescription("All ancestor categories from root to parent")]
    public IEnumerable<Category> GetAncestors([Parent] Category category) => category.GetAllAncestors();

    [GraphQLDescription("All descendant categories (children and their children)")]
    public IEnumerable<Category> GetDescendants([Parent] Category category) => category.GetAllDescendants();
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

Test queries:
```graphql
# Basic query - all categories
query {
  categories {
    id
    name
    description
    color
    isSystemGenerated
    isRoot
    isLeaf
    depth
    fullPath
    transactionCount
    parentCategory {
      name
    }
    children {
      name
    }
    createdAt
    updatedAt
  }
}

# Filtered query - only root categories
query {
  categories(where: { isRoot: { eq: true } }) {
    id
    name
    description
    children {
      name
      children {
        name
      }
    }
  }
}

# Filtered query - only system-generated categories
query {
  categories(where: { isSystemGenerated: { eq: true } }) {
    id
    name
    description
    color
  }
}

# Filtered query - categories with transactions
query {
  categories(where: { transactionCount: { gt: 0 } }) {
    id
    name
    transactionCount
    fullPath
  }
}

# Filtered query - categories by color
query {
  categories(where: { color: { eq: "#FF0000" } }) {
    id
    name
    color
  }
}

# Filtered query - categories by name pattern
query {
  categories(where: { name: { contains: "Food" } }) {
    id
    name
    fullPath
  }
}

# Sorted query - by name ascending
query {
  categories(order: [{ name: ASC }]) {
    id
    name
    fullPath
  }
}

# Sorted query - by transaction count descending
query {
  categories(order: [{ transactionCount: DESC }]) {
    id
    name
    transactionCount
    fullPath
  }
}

# Sorted query - by creation date
query {
  categories(order: [{ createdAt: DESC }]) {
    id
    name
    createdAt
    updatedAt
  }
}

# Complex filtering and sorting
query {
  categories(
    where: { 
      AND: [
        { isSystemGenerated: { eq: false } }
        { transactionCount: { gte: 1 } }
        { name: { ncontains: "test" } }
      ]
    }
    order: [{ transactionCount: DESC }, { name: ASC }]
  ) {
    id
    name
    transactionCount
    fullPath
    color
  }
}

# Hierarchical display - root categories with nested children
query {
  categories(where: { isRoot: { eq: true } }) {
    id
    name
    description
    color
    children {
      id
      name
      description
      children {
        id
        name
        description
        transactionCount
      }
    }
  }
}
```

### 6. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-012 - Implement list all categories query with filtering and sorting"
git push origin story/QUERY-012-list-all-categories
gh pr create --title "QUERY-012 - List All Categories" --body "Implements GraphQL query to list all categories with hierarchical structure, filtering, and sorting support"
```

## Dependencies
- **Blocked By**: Category entity and repository implementation (existing)
- **Blocks**: Category-related mutations and advanced category analytics

## Notes
- Uses `GetHierarchyAsync()` to return categories with proper parent-child relationships loaded
- HotChocolate filtering supports complex queries including nested property filtering
- HotChocolate sorting allows multi-field sorting with different directions
- Computed properties (isRoot, isLeaf, depth, fullPath) are exposed via field extensions
- Consider performance implications for large category trees - may need pagination for very large datasets
- Filtering examples include string operations (contains, ncontains), comparison operations (eq, gt, gte), and logical operations (AND, OR)

## Definition of Done
- [ ] ListAllCategoriesQuery and handler created in Application layer
- [ ] GraphQL resolver with [UseProjection], [UseFiltering], [UseSorting] attributes added
- [ ] CategoryExtensions class created with computed properties
- [ ] Solution builds without errors
- [ ] Manual testing confirms filtering works (by name, color, isSystemGenerated, transactionCount, etc.)
- [ ] Manual testing confirms sorting works (by name, createdAt, transactionCount, etc.)
- [ ] Complex queries with multiple filters and sorts work correctly
- [ ] Hierarchical relationships properly loaded and accessible
- [ ] Pull request opened with comprehensive test queries
- [ ] Code review approved
- [ ] Merged to main branch