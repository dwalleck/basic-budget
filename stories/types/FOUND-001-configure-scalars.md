# Story: FOUND-001 - Configure GraphQL Scalar Types

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Configure custom scalar types for the GraphQL schema including DateTime, Decimal, and Upload. These scalars are fundamental for all other operations and must be configured before other stories can be implemented.

## Acceptance Criteria
- [ ] DateTime scalar configured for date/time fields
- [ ] Decimal scalar configured for precise monetary values
- [ ] Upload scalar configured for file uploads
- [ ] Scalars properly serialize/deserialize
- [ ] Schema documentation updated for each scalar
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Scalar configuration is GraphQL-specific infrastructure
- **Error Handling**: Implement proper serialization error handling
- **Pattern**: Use HotChocolate's scalar type system

### Implementation Location
- **GraphQL Configuration**: `src/BasicBudget.GraphQL/Program.cs`
- **Custom Scalars**: `src/BasicBudget.GraphQL/Types/ScalarTypes.cs` (new)

### Schema Reference
```graphql
# Scalar Types
scalar DateTime  # ISO 8601 date-time string
scalar Decimal   # Precise decimal number for money
scalar Upload    # File upload via multipart/form-data
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/FOUND-001-configure-scalars
```

### 2. Install Required NuGet Packages
Update `/src/BasicBudget.GraphQL/BasicBudget.GraphQL.csproj`:
```xml
<PackageReference Include="HotChocolate.Types.Scalars" Version="13.9.0" />
<PackageReference Include="HotChocolate.Types.Scalars.Upload" Version="13.9.0" />
```

### 3. Create Custom Decimal Scalar
Create `/src/BasicBudget.GraphQL/Types/ScalarTypes.cs`:
```csharp
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Globalization;

namespace BasicBudget.GraphQL.Types;

/// <summary>
/// Custom scalar type for precise decimal values (money)
/// </summary>
public class DecimalType : ScalarType<decimal, StringValueNode>
{
    public DecimalType() : base("Decimal")
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            decimal d => new StringValueNode(d.ToString(CultureInfo.InvariantCulture)),
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
                => new StringValueNode(d.ToString(CultureInfo.InvariantCulture)),
            null => NullValueNode.Default,
            _ => throw new SerializationException(
                ErrorBuilder.New()
                    .SetMessage("Cannot serialize the given value to Decimal.")
                    .Build(),
                this)
        };
    }

    public override object? ParseLiteral(IValueNode valueSyntax)
    {
        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        if (valueSyntax is StringValueNode stringValue &&
            decimal.TryParse(stringValue.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        if (valueSyntax is FloatValueNode floatValue &&
            decimal.TryParse(floatValue.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fd))
        {
            return fd;
        }

        if (valueSyntax is IntValueNode intValue)
        {
            return decimal.Parse(intValue.Value, CultureInfo.InvariantCulture);
        }

        throw new SerializationException(
            ErrorBuilder.New()
                .SetMessage("Cannot parse the given literal to Decimal.")
                .Build(),
            this);
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        switch (runtimeValue)
        {
            case decimal d:
                resultValue = d.ToString(CultureInfo.InvariantCulture);
                return true;
            case string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d):
                resultValue = d.ToString(CultureInfo.InvariantCulture);
                return true;
            case null:
                resultValue = null;
                return true;
            default:
                resultValue = null;
                return false;
        }
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        switch (resultValue)
        {
            case string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d):
                runtimeValue = d;
                return true;
            case decimal d:
                runtimeValue = d;
                return true;
            case double dbl:
                runtimeValue = (decimal)dbl;
                return true;
            case float f:
                runtimeValue = (decimal)f;
                return true;
            case int i:
                runtimeValue = (decimal)i;
                return true;
            case long l:
                runtimeValue = (decimal)l;
                return true;
            case null:
                runtimeValue = null;
                return true;
            default:
                runtimeValue = null;
                return false;
        }
    }
}

/// <summary>
/// Configuration for DateTime scalar with proper formatting
/// </summary>
public static class DateTimeTypeConfiguration
{
    public static string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffK"; // ISO 8601
}
```

### 4. Configure Scalars in Program.cs
Update `/src/BasicBudget.GraphQL/Program.cs`:
```csharp
using BasicBudget.GraphQL.Types;
using HotChocolate.Types;

var builder = WebApplication.CreateBuilder(args);

// Add GraphQL services
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    // Configure scalar types
    .AddType<DecimalType>()
    .AddType<DateTimeType>() // Built-in from HotChocolate.Types.Scalars
    .AddType<UploadType>()   // Built-in from HotChocolate.Types.Scalars.Upload
    .BindRuntimeType<decimal, DecimalType>()
    .BindRuntimeType<DateTime, DateTimeType>()
    // Configure DateTime format
    .AddTypeConverter<DateTime, string>(from => from.ToString(DateTimeTypeConfiguration.DateTimeFormat))
    .AddTypeConverter<string, DateTime>(from => DateTime.Parse(from))
    // Enable multipart request for file uploads
    .AddHttpRequestInterceptor<UploadRequestInterceptor>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// Configure Kestrel for file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB max file size
});

var app = builder.Build();

// Configure middleware for file uploads
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});

app.Run();
```

### 5. Create Upload Interceptor
Create `/src/BasicBudget.GraphQL/Infrastructure/UploadRequestInterceptor.cs`:
```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace BasicBudget.GraphQL.Infrastructure;

public class UploadRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.HasFormContentType)
        {
            // Handle multipart/form-data for file uploads
            requestBuilder.SetGlobalState("httpContext", context);
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

### 6. Add Scalar Documentation
Create `/src/BasicBudget.GraphQL/schema.graphql`:
```graphql
"""
Represents a date and time value in ISO 8601 format
Example: 2025-01-06T10:30:00.000Z
"""
scalar DateTime

"""
Represents a precise decimal number, typically used for monetary values
Example: "123.45"
Note: Transmitted as string to preserve precision
"""
scalar Decimal

"""
Represents a file upload via multipart/form-data
Used in mutations that accept file uploads
"""
scalar Upload
```

### 7. Verify & Test
```bash
# Build entire solution
dotnet build

# Run existing tests
dotnet test

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

Test scalar serialization:
```graphql
# Test DateTime in existing query
query {
  account(id: "some-id") {
    createdAt  # Should return ISO 8601 formatted string
    updatedAt
  }
}

# Test Decimal in Money type
query {
  account(id: "some-id") {
    currentBalance {
      amount  # Should handle decimal precision
    }
  }
}
```

### 8. Create Pull Request
```bash
git add .
git commit -m "feat: FOUND-001 - Configure GraphQL scalar types"
git push origin story/FOUND-001-configure-scalars
gh pr create --title "FOUND-001 - Configure Scalars" --body "Configures DateTime, Decimal, and Upload scalar types for GraphQL schema"
```

## Dependencies
- **Blocked By**: None (foundation configuration)
- **Blocks**: All other stories that use these scalar types

## Notes
- Decimal is transmitted as string to preserve precision across JavaScript clients
- DateTime uses ISO 8601 format for universal compatibility
- Upload scalar requires multipart/form-data support in HTTP pipeline
- Consider adding validation for DateTime (no future dates for certain fields)

## Definition of Done
- [ ] DecimalType custom scalar implemented
- [ ] DateTime scalar configured with ISO 8601 format
- [ ] Upload scalar configured with multipart support
- [ ] Scalars registered in GraphQL schema
- [ ] Type converters configured for serialization
- [ ] Solution builds without errors
- [ ] Manual testing confirms scalars work correctly
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch