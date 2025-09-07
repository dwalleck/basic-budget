# Story: FOUND-002 - Setup Error Handling Infrastructure

## Status
- [ ] Not Started
- [ ] In Progress  
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview
Establish consistent error handling infrastructure across all GraphQL operations. This includes creating the Error type, configuring GraphQL error formatting, and establishing patterns for converting domain errors to GraphQL errors.

## Acceptance Criteria
- [ ] Error type defined matching schema specification
- [ ] Global error filter configured for GraphQL
- [ ] Domain error to GraphQL error mapping established
- [ ] Consistent error codes defined
- [ ] Error logging configured
- [ ] All code builds successfully
- [ ] Pull request opened when complete

## Technical Context

### Architecture Requirements
- **Hexagonal Architecture**: Error handling spans all layers but maintains separation
- **Error Handling**: Establish OneOf<TSuccess, DomainError> to GraphQL error conversion
- **Pattern**: Use error filters and formatters in HotChocolate

### Implementation Location
- **Error Types**: `src/BasicBudget.GraphQL/Types/ErrorTypes.cs` (new)
- **Error Filter**: `src/BasicBudget.GraphQL/Infrastructure/ErrorFilter.cs` (new)
- **Configuration**: `src/BasicBudget.GraphQL/Program.cs`

### Schema Reference
```graphql
type Error {
  message: String!
  code: String!
  path: [String!]!
}

# Used in all mutation payloads
type XxxPayload {
  xxx: Xxx
  errors: [Error!]!
}
```

## Implementation Steps

### 1. Create Feature Branch
```bash
git checkout -b story/FOUND-002-error-handling
```

### 2. Define Error Types
Create `/src/BasicBudget.GraphQL/Types/ErrorTypes.cs`:
```csharp
using HotChocolate;

namespace BasicBudget.GraphQL.Types;

/// <summary>
/// Standard error type for GraphQL responses
/// </summary>
public record ApiError(
    [property: GraphQLDescription("Human-readable error message")]
    string Message,
    
    [property: GraphQLDescription("Machine-readable error code")]
    string Code,
    
    [property: GraphQLDescription("Path to the field that caused the error")]
    IReadOnlyList<string>? Path = null
)
{
    public static ApiError FromDomainError(DomainError domainError, IReadOnlyList<string>? path = null)
    {
        return new ApiError(domainError.Message, domainError.Code, path);
    }
    
    public static ApiError ValidationError(string field, string message)
    {
        return new ApiError(message, "VALIDATION_ERROR", new[] { field });
    }
    
    public static ApiError NotFound(string entityType, string id)
    {
        return new ApiError(
            $"{entityType} with ID {id} not found",
            $"{entityType.ToUpper()}_NOT_FOUND",
            new[] { entityType.ToLower(), id }
        );
    }
}

/// <summary>
/// Standard error codes used across the application
/// </summary>
public static class ErrorCodes
{
    // Validation errors
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidInput = "INVALID_INPUT";
    public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";
    
    // Entity errors
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string TransactionNotFound = "TRANSACTION_NOT_FOUND";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string BudgetNotFound = "BUDGET_NOT_FOUND";
    
    // Business rule errors
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    public const string DuplicateAccountNumber = "DUPLICATE_ACCOUNT_NUMBER";
    public const string InvalidDateRange = "INVALID_DATE_RANGE";
    public const string BudgetExceeded = "BUDGET_EXCEEDED";
    
    // System errors
    public const string InternalError = "INTERNAL_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ExternalServiceError = "EXTERNAL_SERVICE_ERROR";
}
```

### 3. Create Error Filter
Create `/src/BasicBudget.GraphQL/Infrastructure/ErrorFilter.cs`:
```csharp
using HotChocolate;
using BasicBudget.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace BasicBudget.GraphQL.Infrastructure;

/// <summary>
/// Global error filter for GraphQL operations
/// </summary>
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;
    
    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }
    
    public IError OnError(IError error)
    {
        // Log the error
        _logger.LogError(error.Exception, "GraphQL error occurred: {Message}", error.Message);
        
        // Handle different exception types
        var exception = error.Exception;
        
        return exception switch
        {
            DomainException domainEx => CreateError(domainEx.Error, error),
            ValidationException validationEx => CreateValidationError(validationEx, error),
            ArgumentNullException argNullEx => CreateArgumentError(argNullEx, error),
            UnauthorizedAccessException => CreateUnauthorizedError(error),
            _ => CreateInternalError(error)
        };
    }
    
    private IError CreateError(DomainError domainError, IError error)
    {
        return ErrorBuilder.New()
            .SetMessage(domainError.Message)
            .SetCode(domainError.Code)
            .SetPath(error.Path)
            .AddLocation(error.Locations?.FirstOrDefault())
            .Build();
    }
    
    private IError CreateValidationError(ValidationException ex, IError error)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ex.Message)
            .SetCode(ErrorCodes.ValidationError)
            .SetPath(error.Path);
            
        // Add validation details as extensions
        if (ex.Errors?.Any() == true)
        {
            builder.SetExtension("validationErrors", ex.Errors);
        }
        
        return builder.Build();
    }
    
    private IError CreateArgumentError(ArgumentNullException ex, IError error)
    {
        return ErrorBuilder.New()
            .SetMessage($"Required field '{ex.ParamName}' was not provided")
            .SetCode(ErrorCodes.MissingRequiredField)
            .SetPath(error.Path?.Append(ex.ParamName).ToArray())
            .Build();
    }
    
    private IError CreateUnauthorizedError(IError error)
    {
        return ErrorBuilder.New()
            .SetMessage("You are not authorized to perform this action")
            .SetCode("UNAUTHORIZED")
            .SetPath(error.Path)
            .Build();
    }
    
    private IError CreateInternalError(IError error)
    {
        // In production, don't expose internal error details
        var message = _logger.IsEnabled(LogLevel.Debug) 
            ? error.Message 
            : "An internal error occurred";
            
        return ErrorBuilder.New()
            .SetMessage(message)
            .SetCode(ErrorCodes.InternalError)
            .SetPath(error.Path)
            .Build();
    }
}

/// <summary>
/// Custom domain exception for GraphQL
/// </summary>
public class DomainException : Exception
{
    public DomainError Error { get; }
    
    public DomainException(DomainError error) : base(error.Message)
    {
        Error = error;
    }
}

/// <summary>
/// Custom validation exception
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<ValidationError>? Errors { get; }
    
    public ValidationException(string message, IReadOnlyList<ValidationError>? errors = null) 
        : base(message)
    {
        Errors = errors;
    }
}

public record ValidationError(string Field, string Message);
```

### 4. Configure Error Handling in Program.cs
Update `/src/BasicBudget.GraphQL/Program.cs`:
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    // Error handling
    .AddErrorFilter<GraphQLErrorFilter>()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    // Configure error interface
    .AddType<ApiError>()
    // Add diagnostic listener for debugging
    .AddDiagnosticEventListener<ErrorLoggingDiagnosticEventListener>();
```

### 5. Create Diagnostic Listener
Create `/src/BasicBudget.GraphQL/Infrastructure/ErrorLoggingDiagnosticEventListener.cs`:
```csharp
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;

namespace BasicBudget.GraphQL.Infrastructure;

public class ErrorLoggingDiagnosticEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<ErrorLoggingDiagnosticEventListener> _logger;
    
    public ErrorLoggingDiagnosticEventListener(ILogger<ErrorLoggingDiagnosticEventListener> logger)
    {
        _logger = logger;
    }
    
    public override void RequestError(IRequestContext context, Exception exception)
    {
        _logger.LogError(exception, "GraphQL request error");
    }
    
    public override void TaskError(IExecutionTask task, IError error)
    {
        _logger.LogError("GraphQL task error: {Error}", error.Message);
    }
}
```

### 6. Update Mutation Base Pattern
Create `/src/BasicBudget.GraphQL/MutationBase.cs`:
```csharp
using BasicBudget.Domain.Errors;
using BasicBudget.GraphQL.Types;

namespace BasicBudget.GraphQL;

/// <summary>
/// Base class for mutation payloads
/// </summary>
public abstract record MutationPayload<T>
{
    public T? Data { get; init; }
    public IReadOnlyList<ApiError>? Errors { get; init; }
    
    public static MutationPayload<T> Success(T data) => new SuccessPayload(data);
    public static MutationPayload<T> Failure(params ApiError[] errors) => new ErrorPayload(errors);
    public static MutationPayload<T> Failure(DomainError error) => new ErrorPayload(new[] { ApiError.FromDomainError(error) });
    
    private record SuccessPayload(T Data) : MutationPayload<T> { public override T? Data { get; init; } = Data; }
    private record ErrorPayload(IReadOnlyList<ApiError> Errors) : MutationPayload<T> { public override IReadOnlyList<ApiError>? Errors { get; init; } = Errors; }
}
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

Test error handling:
```graphql
mutation {
  createAccount(input: {
    accountNumber: ""  # Should trigger validation error
    name: "Test"
    accountType: CHECKING
    initialBalance: { amount: -100, currency: "USD" }  # Should trigger domain error
  }) {
    account {
      id
    }
    errors {
      message
      code
      path
    }
  }
}
```

### 8. Create Pull Request
```bash
git add .
git commit -m "feat: FOUND-002 - Setup error handling infrastructure"
git push origin story/FOUND-002-error-handling
gh pr create --title "FOUND-002 - Error Handling" --body "Implements comprehensive error handling infrastructure for GraphQL operations"
```

## Dependencies
- **Blocked By**: FOUND-001 (Scalar configuration should be complete)
- **Blocks**: All mutation stories (they depend on error handling)

## Notes
- Error codes should be consistent across the application
- Consider implementing error recovery strategies for transient failures
- Production error messages should not leak sensitive information
- All errors should be logged for debugging purposes

## Definition of Done
- [ ] ApiError type created with standard fields
- [ ] Error codes defined and documented
- [ ] GraphQL error filter implemented
- [ ] Error logging configured
- [ ] Domain error to API error mapping established
- [ ] Solution builds without errors
- [ ] Manual testing confirms errors are properly formatted
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch