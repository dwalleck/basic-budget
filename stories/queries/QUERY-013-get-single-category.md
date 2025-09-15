# Story: QUERY-013 - Get Single Category by ID

## Status
- [x] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL query to retrieve a single category by its ID. This query returns a specific category with its hierarchical relationships (parent/children), usage statistics, and associated budget allocations.

## Acceptance Criteria
- [ ] Query returns category when valid ID provided
- [ ] Returns null when category not found
- [ ] Category hierarchy relationships accessible (parent and children)
- [ ] Usage statistics included (transaction counts, spending totals)
- [ ] Associated budget allocations visible
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Category entity with hierarchical relationships
  - Application Layer: Create GetCategoryQuery and handler via MediatR
  - GraphQL Layer: Add resolver to Query class with [UseProjection]
- **Error Handling**: Use OneOf<Category, DomainError> pattern for all operations
- **Dependency Direction**: Must flow inward (GraphQL → Application → Domain)

### Implementation Location
- **Domain Entity**: `src/BasicBudget.Domain/Entities/Category.cs` (existing)
- **Application Query**: `src/BasicBudget.Application/Queries/GetCategoryQuery.cs` (new)
- **GraphQL Resolver**: `src/BasicBudget.GraphQL/Query.cs`
- **Repository**: `src/BasicBudget.Domain/Repositories/ICategoryRepository.cs` (existing)

### Related Files
- Category entity: `/src/BasicBudget.Domain/Entities/Category.cs`
- Repository interface: `/src/BasicBudget.Domain/Repositories/ICategoryRepository.cs`
- Repository implementation: `/src/BasicBudget.Infrastructure/Persistence/CategoryRepository.cs`

### Schema Reference
```graphql
type Query {
  category(id: ID!): Category
}

type Category {
  id: ID!
  name: String!
  description: String
  color: String
  icon: String
  isActive: Boolean!
  parent: Category
  children: [Category!]!
  level: Int!
  path: String!
  budgetAllocations: [BudgetCategory!]!
  usageStatistics: CategoryUsageStatistics!
  createdAt: DateTime!
  updatedAt: DateTime!
}

type CategoryUsageStatistics {
  transactionCount: Int!
  totalSpent: Money!
  averageTransactionAmount: Money!
  lastTransactionDate: DateTime
  monthlySpending: [MonthlySpending!]!
}

type MonthlySpending {
  year: Int!
  month: Int!
  totalAmount: Money!
  transactionCount: Int!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/QUERY-013-get-single-category
```

### 2. Domain Layer (if needed)
- [ ] Verify Category entity exists with proper relationships
- [ ] Add CategoryUsageStatistics value object if needed
- [ ] Ensure ICategoryRepository has GetByIdAsync method

Create `/src/BasicBudget.Domain/ValueObjects/CategoryUsageStatistics.cs`:
```csharp
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.ValueObjects;

public record CategoryUsageStatistics(
    int TransactionCount,
    Money TotalSpent,
    Money AverageTransactionAmount,
    DateTime? LastTransactionDate,
    IEnumerable<MonthlySpending> MonthlySpending
);

public record MonthlySpending(
    int Year,
    int Month,
    Money TotalAmount,
    int TransactionCount
);
```

### 3. Application Layer
- [ ] Create GetCategoryQuery record
- [ ] Implement MediatR handler with OneOf<Category, DomainError> return type
- [ ] Inject ICategoryRepository and ITransactionRepository via constructor

Create `/src/BasicBudget.Application/Queries/GetCategoryQuery.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using MediatR;
using OneOf;

namespace BasicBudget.Application.Queries;

public record GetCategoryQuery(Guid Id) : IRequest<OneOf<Category, DomainError>>;

public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, OneOf<Category, DomainError>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetCategoryQueryHandler(
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository)
    {
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<OneOf<Category, DomainError>> Handle(
        GetCategoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            
            if (category == null)
            {
                return new DomainError("CATEGORY_NOT_FOUND", $"Category with ID {request.Id} was not found");
            }

            return OneOf<Category, DomainError>.FromT0(category);
        }
        catch (Exception ex)
        {
            return new DomainError("CATEGORY_FETCH_FAILED", $"Failed to retrieve category: {ex.Message}");
        }
    }
}
```

### 4. Infrastructure Layer (if needed)
- [ ] Verify CategoryRepository implements GetByIdAsync with proper includes
- [ ] Ensure EF Core configuration includes hierarchical relationships
- [ ] Add navigation properties for parent/children relationships

### 5. GraphQL Layer
- [ ] Add resolver method to Query class with [UseProjection] attribute
- [ ] Map between GraphQL types and domain models
- [ ] Handle errors appropriately (return null for not found, throw for other errors)

Add to `/src/BasicBudget.GraphQL/Query.cs`:
```csharp
[GraphQLDescription("Retrieve a single category by its ID")]
[UseProjection]
public async Task<Category?> GetCategoryAsync(
    [GraphQLDescription("The unique identifier of the category")]
    [ID] Guid id,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetCategoryQuery(id), cancellationToken);
    return result.Match(
        category => category,
        error => error.Code == "CATEGORY_NOT_FOUND" 
            ? null 
            : throw new GraphQLException(error.Message)
    );
}
```

### 6. Add Category Extensions
Create `/src/BasicBudget.GraphQL/Types/CategoryExtensions.cs`:
```csharp
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using HotChocolate.Types;

namespace BasicBudget.GraphQL.Types;

[ExtendObjectType(typeof(Category))]
public class CategoryExtensions
{
    [GraphQLDescription("The hierarchical level of this category (0 for root categories)")]
    public int GetLevel([Parent] Category category)
    {
        var level = 0;
        var current = category.Parent;
        while (current != null)
        {
            level++;
            current = current.Parent;
        }
        return level;
    }

    [GraphQLDescription("The full hierarchical path of this category")]
    public string GetPath([Parent] Category category)
    {
        var path = new List<string>();
        var current = category;
        
        while (current != null)
        {
            path.Insert(0, current.Name);
            current = current.Parent;
        }
        
        return string.Join(" > ", path);
    }

    [GraphQLDescription("Usage statistics for this category")]
    public async Task<CategoryUsageStatistics> GetUsageStatisticsAsync(
        [Parent] Category category,
        [Service] ITransactionRepository transactionRepository,
        CancellationToken cancellationToken)
    {
        var transactions = await transactionRepository.GetByCategoryAsync(category.Id, cancellationToken);
        
        var transactionCount = transactions.Count();
        var totalSpent = transactions.Any() 
            ? Money.Create(transactions.Sum(t => t.Amount.Amount), transactions.First().Amount.Currency)
            : Money.Create(0, "USD"); // Default currency
        
        var averageAmount = transactionCount > 0 
            ? Money.Create(totalSpent.Amount / transactionCount, totalSpent.Currency)
            : Money.Create(0, totalSpent.Currency);
        
        var lastTransactionDate = transactions.Any() 
            ? transactions.Max(t => t.TransactionDate) 
            : (DateTime?)null;

        var monthlySpending = transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new MonthlySpending(
                g.Key.Year,
                g.Key.Month,
                Money.Create(g.Sum(t => t.Amount.Amount), totalSpent.Currency),
                g.Count()))
            .OrderBy(ms => ms.Year)
            .ThenBy(ms => ms.Month);

        return new CategoryUsageStatistics(
            transactionCount,
            totalSpent,
            averageAmount,
            lastTransactionDate,
            monthlySpending
        );
    }
}
```

### 7. Write Automated Tests
- [ ] Unit tests for domain logic (if applicable)
- [ ] Unit tests for application handlers
- [ ] Integration tests for repository implementations (if applicable)
- [ ] GraphQL integration tests for new queries/mutations
- [ ] Test both success and error scenarios
- [ ] Ensure all edge cases are covered

**TESTING Section**:

#### Unit Tests
Create `/tests/BasicBudget.Application.Tests/Queries/GetCategoryQueryHandlerTests.cs`:
```csharp
using BasicBudget.Application.Queries;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using TUnit.Core;

namespace BasicBudget.Application.Tests.Queries;

public class GetCategoryQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly GetCategoryQueryHandler _handler;

    public GetCategoryQueryHandlerTests()
    {
        _categoryRepository = Substitute.For<ICategoryRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _handler = new GetCategoryQueryHandler(_categoryRepository, _transactionRepository);
    }

    [Test]
    public async Task Handle_ValidCategoryId_ReturnsCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var expectedCategory = new Category("Test Category");
        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(expectedCategory);

        var query = new GetCategoryQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.Should().BeTrue();
        result.AsT0.Should().Be(expectedCategory);
    }

    [Test]
    public async Task Handle_InvalidCategoryId_ReturnsNotFoundError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var query = new GetCategoryQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsT1.Should().BeTrue();
        result.AsT1.Code.Should().Be("CATEGORY_NOT_FOUND");
    }

    [Test]
    public async Task Handle_RepositoryThrowsException_ReturnsFetchFailedError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        var query = new GetCategoryQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsT1.Should().BeTrue();
        result.AsT1.Code.Should().Be("CATEGORY_FETCH_FAILED");
    }
}
```

#### Integration Tests
Create `/tests/BasicBudget.GraphQL.Tests/Queries/CategoryQueryTests.cs`:
```csharp
using BasicBudget.GraphQL.Tests.Infrastructure;
using FluentAssertions;
using TUnit.Core;

namespace BasicBudget.GraphQL.Tests.Queries;

public class CategoryQueryTests : GraphQLTestBase
{
    [Test]
    public async Task Category_ValidId_ReturnsCategory()
    {
        // Arrange
        var category = await SeedCategoryAsync("Test Category");

        var query = """
            query($id: ID!) {
                category(id: $id) {
                    id
                    name
                    description
                    level
                    path
                    usageStatistics {
                        transactionCount
                        totalSpent {
                            formatted
                        }
                    }
                }
            }
            """;

        // Act
        var result = await ExecuteQueryAsync(query, new { id = category.Id });

        // Assert
        result.Should().NotBeNull();
        result.MatchSnapshot();
    }

    [Test]
    public async Task Category_InvalidId_ReturnsNull()
    {
        // Arrange
        var query = """
            query($id: ID!) {
                category(id: $id) {
                    id
                    name
                }
            }
            """;

        // Act
        var result = await ExecuteQueryAsync(query, new { id = Guid.NewGuid() });

        // Assert
        var categoryData = result["data"]?["category"];
        categoryData.Should().BeNull();
    }

    [Test]
    public async Task Category_WithHierarchy_ReturnsParentAndChildren()
    {
        // Arrange
        var parentCategory = await SeedCategoryAsync("Parent Category");
        var childCategory = await SeedCategoryAsync("Child Category", parentCategory.Id);

        var query = """
            query($id: ID!) {
                category(id: $id) {
                    id
                    name
                    level
                    path
                    parent {
                        id
                        name
                    }
                    children {
                        id
                        name
                    }
                }
            }
            """;

        // Act
        var result = await ExecuteQueryAsync(query, new { id = childCategory.Id });

        // Assert
        result.Should().NotBeNull();
        result.MatchSnapshot();
    }
}
```

#### Coverage Requirements
- **Unit Test Coverage**: Minimum 90% for new handler classes
- **Integration Test Coverage**: All new GraphQL resolvers must have tests
- **Test Execution**: Run with `dotnet test --coverage --coverage-output-format cobertura`

### 8. Verify & Test
```bash
# Build entire solution
dotnet build

# Run all tests (including new ones)
dotnet test

# Run tests with coverage (outputs to console)
dotnet test --coverage --coverage-output-format cobertura

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

Test queries:
```graphql
# Basic category query
query {
  category(id: "valid-guid-here") {
    id
    name
    description
    color
    icon
    isActive
    level
    path
    parent {
      id
      name
    }
    children {
      id
      name
    }
    usageStatistics {
      transactionCount
      totalSpent {
        formatted
      }
      averageTransactionAmount {
        formatted
      }
      lastTransactionDate
      monthlySpending {
        year
        month
        totalAmount {
          formatted
        }
        transactionCount
      }
    }
    createdAt
    updatedAt
  }
}

# Test with non-existent ID
query {
  category(id: "00000000-0000-0000-0000-000000000000") {
    id
  }
}
```

### 9. Create Pull Request
```bash
git add .
git commit -m "feat: QUERY-013 - Implement get single category query with usage statistics"
git push origin story/QUERY-013-get-single-category
gh pr create --title "QUERY-013 - Get Single Category by ID" --body "Implements GraphQL query to retrieve a single category with hierarchical relationships and usage statistics"
```

## Dependencies
- **Blocked By**: Category entity and repository implementations
- **Blocks**: Category hierarchy queries and category-based reporting

## Notes
- Includes hierarchical path calculation for breadcrumb navigation
- Usage statistics calculated from transaction history
- Level calculation determines depth in category tree
- Consider performance impact of usage statistics calculation
- Monthly spending data useful for trend analysis

## Definition of Done
- [ ] All acceptance criteria met
- [ ] Code follows hexagonal architecture
- [ ] Errors handled with OneOf pattern
- [ ] **Unit tests written for new domain logic**
- [ ] **Unit tests written for application handlers**
- [ ] **Integration tests written for GraphQL endpoints**
- [ ] **All tests pass (new and existing)**
- [ ] **Code coverage maintained or improved**
- [ ] GetCategoryQuery and handler created in Application layer
- [ ] GraphQL resolver with [UseProjection] added to Query class
- [ ] Category extensions for computed fields created
- [ ] Usage statistics calculations implemented
- [ ] Solution builds without errors
- [ ] Manual testing confirms query works correctly with hierarchy
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch