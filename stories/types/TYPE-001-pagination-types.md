# Story: TYPE-001 - PageInfo and Connection Types

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Implement the GraphQL connection types for cursor-based pagination following the Relay specification. These types will be used for paginating large result sets, particularly transactions.

## Acceptance Criteria
- [ ] PageInfo type implemented with all required fields
- [ ] Generic Connection and Edge types implemented
- [ ] TransactionConnection specifically implemented
- [ ] Cursor encoding/decoding logic implemented
- [ ] Types registered with HotChocolate schema
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: These are GraphQL-specific types that belong in the GraphQL layer
- **Error Handling**: Not applicable for type definitions
- **Pattern**: Follow Relay Connection specification for consistency

### Implementation Location
- **GraphQL Types**: `src/BasicBudget.GraphQL/Types/PaginationTypes.cs` (new)
- **GraphQL Configuration**: `src/BasicBudget.GraphQL/Program.cs`

### Schema Reference
```graphql
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
git checkout -b story/TYPE-001-pagination-types
```

### 2. Create Pagination Types
Create `/src/BasicBudget.GraphQL/Types/PaginationTypes.cs`:
```csharp
using HotChocolate;
using HotChocolate.Types;
using System.Text;
using BasicBudget.Domain.Entities;

namespace BasicBudget.GraphQL.Types;

/// <summary>
/// Information about pagination in a connection
/// </summary>
public class PageInfo
{
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
}

/// <summary>
/// Generic edge type for connections
/// </summary>
public class Edge<T>
{
    public Edge(T node, string cursor)
    {
        Node = node;
        Cursor = cursor;
    }

    public T Node { get; }
    public string Cursor { get; }
}

/// <summary>
/// Generic connection type for pagination
/// </summary>
public class Connection<T>
{
    public Connection(
        IReadOnlyList<T> nodes,
        IReadOnlyList<Edge<T>> edges,
        PageInfo pageInfo,
        int totalCount)
    {
        Nodes = nodes;
        Edges = edges;
        PageInfo = pageInfo;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Nodes { get; }
    public IReadOnlyList<Edge<T>> Edges { get; }
    public PageInfo PageInfo { get; }
    public int TotalCount { get; }
}

/// <summary>
/// Specific implementation for Transaction connections
/// </summary>
public class TransactionConnection : Connection<Transaction>
{
    public TransactionConnection(
        IReadOnlyList<Transaction> nodes,
        IReadOnlyList<Edge<Transaction>> edges,
        PageInfo pageInfo,
        int totalCount)
        : base(nodes, edges, pageInfo, totalCount)
    {
    }
}

/// <summary>
/// Specific implementation for Transaction edges
/// </summary>
public class TransactionEdge : Edge<Transaction>
{
    public TransactionEdge(Transaction node, string cursor)
        : base(node, cursor)
    {
    }
}

/// <summary>
/// Helper class for cursor encoding/decoding
/// </summary>
public static class CursorHelper
{
    public static string ToCursor(object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public static T? FromCursor<T>(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }
}

/// <summary>
/// Extension methods for creating connections
/// </summary>
public static class ConnectionExtensions
{
    public static Connection<T> ToConnection<T>(
        this IQueryable<T> source,
        int? first,
        string? after,
        int? last,
        string? before,
        Func<T, string> getCursor)
    {
        var totalCount = source.Count();
        
        // Apply cursor-based filtering
        if (!string.IsNullOrEmpty(after))
        {
            // Decode cursor and filter
            // Implementation depends on cursor strategy
        }
        
        if (!string.IsNullOrEmpty(before))
        {
            // Decode cursor and filter
            // Implementation depends on cursor strategy
        }
        
        // Apply limits
        IQueryable<T> query = source;
        if (first.HasValue)
        {
            query = query.Take(first.Value + 1); // Take one extra to determine hasNextPage
        }
        else if (last.HasValue)
        {
            // For last, we need to reverse, take, then reverse again
            // This is simplified - real implementation would be more complex
            query = query.Take(last.Value + 1);
        }
        
        var items = query.ToList();
        var hasNextPage = first.HasValue && items.Count > first.Value;
        var hasPreviousPage = !string.IsNullOrEmpty(after) || (last.HasValue && items.Count > last.Value);
        
        if (hasNextPage)
        {
            items = items.Take(first!.Value).ToList();
        }
        
        var edges = items.Select(item => new Edge<T>(item, getCursor(item))).ToList();
        
        var pageInfo = new PageInfo
        {
            HasNextPage = hasNextPage,
            HasPreviousPage = hasPreviousPage,
            StartCursor = edges.FirstOrDefault()?.Cursor,
            EndCursor = edges.LastOrDefault()?.Cursor
        };
        
        return new Connection<T>(items, edges, pageInfo, totalCount);
    }
}
```

### 3. Register Types with GraphQL Schema
Update `/src/BasicBudget.GraphQL/Program.cs`:
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<PageInfo>()
    .AddType<TransactionConnection>()
    .AddType<TransactionEdge>()
    // ... other type registrations
```

### 4. Create Type Configuration (Optional)
Create `/src/BasicBudget.GraphQL/Types/PaginationTypeConfigurations.cs`:
```csharp
using HotChocolate.Types;

namespace BasicBudget.GraphQL.Types;

public class PageInfoType : ObjectType<PageInfo>
{
    protected override void Configure(IObjectTypeDescriptor<PageInfo> descriptor)
    {
        descriptor.Name("PageInfo");
        descriptor.Description("Information about pagination in a connection.");
        
        descriptor.Field(f => f.HasNextPage)
            .Type<NonNullType<BooleanType>>()
            .Description("Whether there are more pages after this one.");
            
        descriptor.Field(f => f.HasPreviousPage)
            .Type<NonNullType<BooleanType>>()
            .Description("Whether there are pages before this one.");
            
        descriptor.Field(f => f.StartCursor)
            .Type<StringType>()
            .Description("Cursor for the first edge in this page.");
            
        descriptor.Field(f => f.EndCursor)
            .Type<StringType>()
            .Description("Cursor for the last edge in this page.");
    }
}

public class TransactionConnectionType : ObjectType<TransactionConnection>
{
    protected override void Configure(IObjectTypeDescriptor<TransactionConnection> descriptor)
    {
        descriptor.Name("TransactionConnection");
        descriptor.Description("A connection to a list of transactions.");
        
        descriptor.Field(f => f.Nodes)
            .Type<NonNullType<ListType<NonNullType<ObjectType<Transaction>>>>>()
            .Description("The transactions in this page.");
            
        descriptor.Field(f => f.Edges)
            .Type<NonNullType<ListType<NonNullType<TransactionEdgeType>>>>()
            .Description("Edges containing cursors for pagination.");
            
        descriptor.Field(f => f.PageInfo)
            .Type<NonNullType<PageInfoType>>()
            .Description("Pagination information.");
            
        descriptor.Field(f => f.TotalCount)
            .Type<NonNullType<IntType>>()
            .Description("Total number of transactions matching the query.");
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

Verify types appear in schema:
```graphql
{
  __schema {
    types {
      name
      kind
      fields {
        name
        type {
          name
        }
      }
    }
  }
}
```

### 6. Create Pull Request
```bash
git add .
git commit -m "feat: TYPE-001 - Implement pagination types for connections"
git push origin story/TYPE-001-pagination-types
gh pr create --title "TYPE-001 - Pagination Types" --body "Implements PageInfo, Connection, and Edge types for cursor-based pagination"
```

## Dependencies
- **Blocked By**: None (foundational types)
- **Blocks**: QUERY-004 (List Transactions with Pagination)

## Notes
- The cursor implementation can be enhanced to include sorting field values
- Consider using HotChocolate's built-in pagination if it meets requirements
- The generic Connection<T> allows reuse for other entity types

## Definition of Done
- [ ] PageInfo type created with all fields
- [ ] Generic Connection and Edge types implemented
- [ ] TransactionConnection and TransactionEdge implemented
- [ ] Cursor helper methods for encoding/decoding
- [ ] Types registered with GraphQL schema
- [ ] Solution builds without errors
- [ ] Types visible in GraphQL schema introspection
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch