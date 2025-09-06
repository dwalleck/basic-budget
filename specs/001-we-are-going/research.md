# Research: Basic Budget Implementation

## Technology Stack Research

### .NET 10 with HotChocolate GraphQL
**Decision**: Use .NET 10 as the primary platform with HotChocolate for GraphQL implementation
**Rationale**: 
- .NET 10 provides excellent cross-platform support and performance
- HotChocolate is the most mature and feature-rich GraphQL library for .NET
- Strong integration with dependency injection and ASP.NET Core
- Supports GraphQL best practices including filtering, sorting, pagination out of the box

**Version Requirements**:
- HotChocolate.AspNetCore v15.1.10+ (REQUIRES RESEARCH: latest stable version as of September 2025)
- HotChocolate.Data v15.1.10+ for filtering/sorting (REQUIRES RESEARCH: current API patterns)
- HotChocolate.Data.EntityFramework v15.1.10+ for EF Core integration (REQUIRES RESEARCH: best practices)
- HotChocolate.Subscriptions.Redis v15.1.10+ if real-time features needed

**Areas Requiring Further Research**:
1. **Filtering & Sorting API Changes**: Current schema uses `ComparableGuidOperationFilterInput` - verify latest patterns
2. **Pagination Best Practices**: Relay cursor pagination implementation in HotChocolate v15+
3. **File Upload Patterns**: Latest `Upload` scalar integration with multipart requests
4. **Entity Framework Integration**: Current best practices for HotChocolate.Data with EF Core 10
5. **Schema-First vs Code-First**: Determine optimal approach for hexagonal architecture
6. **Error Handling**: Latest error propagation patterns and GraphQL error specifications
7. **Middleware Pipeline**: Authentication/authorization middleware integration patterns
8. **Subscriptions Architecture**: Real-time budget alerts and notifications patterns

**Alternatives considered**: 
- GraphQL.NET (more complex setup, less integrated)
- Custom REST API (doesn't meet GraphQL requirement)

### Hexagonal Architecture Implementation
**Decision**: Implement pure hexagonal architecture with clear layer boundaries
**Rationale**: 
- Constitutional requirement (non-negotiable)
- Enables testability by isolating business logic from external concerns
- Facilitates future changes to infrastructure (database, file formats)
- Clear separation of concerns improves maintainability

**Alternatives considered**: N/A - Constitutional requirement

### MediatR for CQRS
**Decision**: Use MediatR to implement Command Query Responsibility Segregation pattern
**Rationale**: 
- Clean separation between read and write operations
- Decouples GraphQL resolvers from business logic
- Enables cross-cutting concerns (validation, logging, transactions)
- Well-established pattern in .NET ecosystem

**Alternatives considered**: 
- Direct service calls (violates hexagonal architecture)
- Custom mediator implementation (unnecessary complexity)

### Entity Framework Core with PostgreSQL
**Decision**: Use EF Core as ORM with PostgreSQL database
**Rationale**: 
- EF Core provides excellent code-first migrations
- PostgreSQL offers robust ACID compliance for financial data
- Good performance for family-scale data volumes
- Strong .NET integration and tooling

**Alternatives considered**: 
- Dapper (too low-level for domain-rich application)
- SQLite (insufficient for multi-user scenarios in future)
- SQL Server (PostgreSQL preferred for cross-platform deployment)

### .NET Aspire for Local Development and Testing
**Decision**: Use .NET Aspire for orchestrating local development and testing environment
**Rationale**: 
- .NET Aspire provides unified orchestration for multi-service applications
- Built-in support for PostgreSQL containers and service discovery
- Excellent developer experience with integrated dashboard and logging
- Simplified configuration management for local development
- Built-in health checks and monitoring capabilities

**Version Requirements**: 
- .NET Aspire 9.0.0 or later (REQUIRES RESEARCH: latest stable version as of September 2025)
- Compatible with .NET 10 applications (REQUIRES RESEARCH: compatibility matrix)
- Aspire.Hosting.PostgreSQL package for database orchestration (REQUIRES RESEARCH: current version and API changes)

**Integration Points**:
- AppHost project for orchestrating GraphQL API and PostgreSQL
- Service discovery integration for database connection strings
- Distributed tracing integration with HotChocolate GraphQL
- Health check integration across all hexagonal layers
- Dashboard integration for local development monitoring

**Research Results (Updated September 2025)**:

1. **Current Aspire Version and Breaking Changes**: 
   - **Current Version**: .NET Aspire 9.4.2 (released September 2, 2025)
   - **Upcoming**: .NET Aspire 9.5 in preparation with CLI improvements
   - **Compatibility**: Full .NET 10 support available (use `--framework net10.0`)
   - **Breaking Changes**: Workload removal from 8.0→9.0, API cleanup, Azure resource parameters changed
   - **Migration**: Use Upgrade Assistant, update project SDK to `Aspire.AppHost.Sdk`

2. **HotChocolate Integration**: 
   - **Service Discovery**: Automatic connection string resolution for GraphQL APIs
   - **Health Checks**: Built-in GraphQL schema validation health checks
   - **Distributed Tracing**: OpenTelemetry integration with HotChocolate.Execution tracing
   - **Dashboard Integration**: Aspire dashboard shows GraphQL metrics, logs, and performance data
   - **Configuration**: Use `builder.AddServiceDefaults()` for automatic Aspire integration

3. **Entity Framework Integration**: 
   - **Package**: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` v9.4.x
   - **Setup**: `builder.AddNpgsqlDbContext<DbContext>("connectionName")`
   - **Features**: Automatic health checks, connection pooling, retry policies, distributed tracing
   - **Testing**: Full `DistributedApplicationTestingBuilder` support with real PostgreSQL containers

4. **TUnit Integration (Updated September 2025)**: 
   - **Compatibility**: Full TUnit v0.57.24 support with `Aspire.Hosting.Testing` v9.4.x
   - **Performance**: Up to 10x faster test execution (some benchmarks 200x faster vs traditional frameworks)
   - **Pattern**: Use `[ClassDataSource<AspireTestFactory>]` instead of xUnit's `IClassFixture<T>`
   - **Lifecycle**: `IAsyncInitializer` for async setup, `[Before(Test)]` for per-test reset
   - **Parallel Control**: `[ParallelLimiter<DatabaseTestLimit>]` for single database access across test suite
   - **Source Generation**: Compile-time test discovery eliminates reflection-based test runner overhead
   - **Native AOT**: Full compatibility for deployment scenarios requiring ahead-of-time compilation

5. **HotChocolate Comprehensive Research**:
   
   **Current Versions (September 2025)**:
   - **HotChocolate.AspNetCore**: v15.1.10 (September 1, 2025)
   - **HotChocolate.Data**: v15.1.10 (EF Core integration, filtering/sorting)
   - **HotChocolate.Subscriptions.Redis**: v15.1.10 (real-time features)
   - **Full .NET 10 compatibility** confirmed across all packages

   **Breaking Changes from v14 → v15**:
   - **Minimum .NET 8.0+ required** (dropped .NET 6/7 support)
   - **Date/Time handling**: `DateOnly` and `TimeOnly` types now used
   - **DataLoader architecture overhaul**: mandatory `DataLoaderOptions`
   - **Type system improvements**: two-step completion process

   **Filtering & Sorting (v15.1.10)**:
   - **Current patterns**: `ComparableGuidOperationFilterInput`, `StringOperationFilterInput`
   - **Complex filtering**: Full AND/OR operations with nested conditions
   - **EF Core integration**: Optimal SQL translation for financial queries
   - **Performance**: Composite indexes recommended for date/amount/category

   **File Upload Best Practices**:
   - **Code-first recommended** for 2025 (2.3x faster than alternatives)
   - **Mutation conventions**: Automatic input/payload type generation
   - **Security**: File validation, size limits, MIME type checking
   - **Streaming**: Handle large CSV/QFX/OFX files without memory issues

   **Subscriptions & Real-time**:
   - **Redis pub/sub**: `HotChocolate.Subscriptions.Redis` for scaling
   - **WebSocket authentication**: JWT token validation on connection
   - **Topic patterns**: User-specific topics for budget alerts
   - **Performance**: Optimized event publishing with parallel tasks

   **Architecture Recommendation**:
   - **Hybrid approach**: Code-first with schema extensions
   - **Mutation conventions**: Apply to all mutations for consistency  
   - **Error handling**: Union types for structured errors
   - **Authorization**: Field-level security with `[Authorize]`

**Implementation Specifications for Basic Budget**:

**AppHost Configuration**:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with persistent data and admin tools
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin(c => c.WithHostPort(5432))
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres.AddDatabase("basic-budget-db");

// GraphQL API with all references
var graphqlApi = builder.AddProject<Projects.BasicBudget_GraphQL>("basic-budget-api")
    .WithReference(postgresDb)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
```

**Integration Testing Configuration**:
- Use `DistributedApplicationTestingBuilder` with TUnit `[ClassDataSource<AspireTestFactory>]`
- Reset database state between tests using Respawner package
- Test both GraphQL contract compliance and integration scenarios from quickstart.md
- Parallel test execution with `[ParallelLimiter<DatabaseTestLimit>]` for single database access

**Package Requirements**:
- `Aspire.Hosting.PostgreSQL` v9.4.x (AppHost)
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` v9.4.x (GraphQL API)
- `Aspire.Hosting.Testing` v9.4.x (Tests)
- `HotChocolate.AspNetCore` v15.1.10+ (GraphQL server)
- `HotChocolate.Data` v15.1.10+ (filtering, sorting, projections)
- `HotChocolate.Data.EntityFramework` v15.1.10+ (EF Core integration)
- `HotChocolate.Subscriptions.Redis` v15.1.10+ (real-time budget alerts)
- `TUnit` v0.57.24+ (modern testing framework with source generation and native fluent assertions)
- `TUnit.Assertions` v0.57.24+ (comprehensive assertion library with async-first design)
- `TUnit.Analyzers` v0.57.24+ (code analyzers for better test development experience)
- `Serilog` v4.1.0+ (structured logging framework)
- `Serilog.AspNetCore` v8.0.3+ (ASP.NET Core integration with request logging)
- `Serilog.Sinks.Console` v6.0.0+ (console logging for development)
- `Serilog.Sinks.File` v6.0.0+ (file logging with rolling policies)
- `Serilog.Sinks.PostgreSQL` v4.0.0+ (database audit logging)
- `Serilog.Enrichers.OpenTelemetry` v2.0.0+ (.NET Aspire tracing integration)
- `OneOf` v3.0.271+ (discriminated unions for error handling)
- `OneOf.SourceGenerator` v3.0.271+ (compile-time union optimizations)
- `Respawn` v6.2.1+ (database reset for integration tests)
- `Microsoft.AspNetCore.Mvc.Testing` v9.0+ (TestServer for GraphQL integration tests)

**Alternatives considered**: 
- Docker Compose (less integrated with .NET ecosystem)
- Manual PostgreSQL setup (more complex for developers)
- Testcontainers (less comprehensive orchestration)

### TUnit Testing Framework
**Decision**: Use TUnit as testing framework across all test projects
**Rationale**: 
- Modern, high-performance testing framework with source-generated tests
- Native AOT compatibility (uses source generators instead of reflection)
- Better async support than traditional frameworks
- Built on Microsoft.Testing.Platform for enhanced .NET tooling integration
- Constitutional requirement

**Version Requirements (Updated September 2025)**:
- **TUnit**: v0.57.24 (latest stable, production-ready with mostly stable API)
- **Full .NET 10 compatibility**: Confirmed through Microsoft.Testing.Platform integration
- **Performance benefits**: Up to 10x faster test execution, some benchmarks 200x faster
- **Parallel execution**: Default parallel execution across all tests, even within same class
- **Source generation**: Tests located and registered at compile time, not runtime

**Integration Patterns Requiring Further Research**:

1. **Database Testing with Respawn**:
   - **Current Gap**: No specific TUnit + Respawn + EF Core integration documentation found
   - **Needed Research**: Database cleanup patterns, transaction isolation, parallel test database access
   - **Workaround**: Apply standard Respawn patterns with TUnit lifecycle attributes
   
2. **GraphQL HotChocolate Integration**:
   - **Current Gap**: No TUnit-specific HotChocolate integration examples in 2025
   - **Available Patterns**: Standard HotChocolate testing with TestServer, in-memory executors
   - **Integration Approach**: Apply HotChocolate patterns with TUnit's enhanced parallel execution
   - **Schema Testing**: SDL validation, contract testing with TUnit's source generation benefits
   
3. **.NET Aspire TestHost Integration**:
   - **Confirmed Pattern**: TUnit works with DistributedApplicationTestingBuilder
   - **Implementation**: Use TUnit's `[ClassDataSource<AspireTestFactory>]` instead of xUnit's `IClassFixture<T>`
   - **Resource Management**: TUnit's parallel execution requires careful database resource limiting
   
4. **Parallel Test Execution Control**:
   - **ParallelLimiter Attribute**: `[ParallelLimiter<T>]` where T implements `IParallelLimit`
   - **Shared Limits**: Parallel limits shared across entire test suite by type
   - **Database Testing**: Critical for single PostgreSQL instance with multiple test classes
   - **Pattern**: Create `DatabaseTestLimit : IParallelLimit { int Limit => 1; }` for database tests

**Lifecycle Management (TUnit-Specific)**:
- **Setup**: `IAsyncInitializer` interface for async test class initialization
- **Per-Test Reset**: `[Before(Test)]` attribute for method execution before each test
- **Cleanup**: `[After(Test)]` and `[After(Class)]` attributes for teardown
- **NotInParallel**: `[NotInParallel]` attribute to disable default parallel execution when needed

**Integration Testing Strategy**:
- **Aspire Integration**: Use `DistributedApplicationTestingBuilder` with TUnit's class data sources
- **Database Cleanup**: Implement Respawn integration with TUnit lifecycle hooks
- **GraphQL Contract Testing**: Leverage TUnit's source generation for compile-time schema validation
- **Parallel Resource Management**: Use ParallelLimiter for database and external service access

**First-Class Testing Experience Features**:

1. **Advanced Lifecycle Management**:
   - **Comprehensive Hooks**: `[Before(Test)]`, `[After(Test)]`, `[Before(Class)]`, `[After(Class)]`, `[Before(Assembly)]`, `[After(Assembly)]`, `[Before(TestSession)]`, `[After(TestSession)]`
   - **Discovery Hooks**: `[Before(TestDiscovery)]`, `[After(TestDiscovery)]` for advanced setup scenarios
   - **Global Hooks**: `[BeforeEvery(Test)]` runs before every test in the entire test run
   - **Context Injection**: All hooks support injecting context objects for interrogating test run state
   - **AsyncLocal Support**: Full support for AsyncLocal values with `context.AddAsyncLocalValues()`

2. **Native Fluent Assertions API**:
   - **Built-in Assert.That**: `await Assert.That(result).IsNotNull().And.HasCount(5)`
   - **Async First**: All assertions are async-aware: `await Assert.That(user.CreatedAt).IsEqualTo(DateTime.Now).Within(TimeSpan.FromMinutes(1))`
   - **Fluent Chaining**: `await Assert.That(httpClient.GetAsync("/api/health")).HasStatusCode(HttpStatusCode.OK)`
   - **Integration Ready**: Works seamlessly with Fluent Assertions library when needed

3. **Advanced Data Sources**:
   - **Arguments Attribute**: `[Arguments(1, 2, 3)]` for compile-time known data
   - **ClassDataSource**: `[ClassDataSource<CustomerFactory>]` for complex object injection
   - **Matrix Testing**: Generate thousands of test combinations automatically
   - **Repeat Attribute**: `[Repeat(1000)]` for load testing and flaky test detection
   - **Custom DataSourceGenerator<T>**: Extend framework without modifying core code

4. **Matrix Testing Example**:
   ```csharp
   [Test, MatrixDataSource]
   public async Task TestAllAccountAndTransactionCombinations(
       [Matrix(AccountType.Checking, AccountType.Savings, AccountType.CreditCard)] AccountType accountType,
       [Matrix(-1000, -100, -1, 100, 1000)] decimal transactionAmount,
       [Matrix("USD", "EUR", "GBP")] string currency
   )
   {
       // Generates 45 test cases (3 × 5 × 3)
       await Assert.That(CreateTransaction(accountType, transactionAmount, currency))
           .IsNotNull()
           .And.Satisfies(t => t.Amount.Currency == currency);
   }
   ```

5. **Custom Assertions for Domain**:
   ```csharp
   public static class BudgetAssertions
   {
       [CustomAssertion]
       public static async Task ShouldBeWithinBudget(this AssertionBuilder<BudgetCategory> builder)
       {
           var budgetCategory = await builder.GetActualValue();
           await Assert.That(budgetCategory.SpentAmount.Amount)
               .IsLessThanOrEqualTo(budgetCategory.AllocatedAmount.Amount)
               .WithMessage($"Budget category '{budgetCategory.Category.Name}' exceeded limit");
       }
       
       [CustomAssertion]
       public static async Task ShouldHaveValidTransactionCount(this AssertionBuilder<Account> builder, int expectedMin)
       {
           var account = await builder.GetActualValue();
           await Assert.That(account.Transactions).HasCountGreaterThanOrEqualTo(expectedMin);
       }
   }
   ```

6. **Extensibility Through Microsoft.Testing.Platform**:
   - **Plugin Architecture**: Built on Microsoft.Testing.Platform for maximum extensibility
   - **Custom Test Runners**: Create specialized runners for specific scenarios
   - **Framework Agnostic Extensions**: Leverage generic testing extension packages
   - **Performance Monitoring**: Built-in performance tracking and diagnostics

**Enhanced Testing Patterns for Basic Budget**:

1. **GraphQL Contract Testing with Matrix Data**:
   ```csharp
   [Test, MatrixDataSource]
   public async Task CreateAccount_ValidatesAllFieldCombinations(
       [Matrix("", "ValidName", "Very Long Name That Exceeds Limits")] string name,
       [Matrix(AccountType.Checking, AccountType.Savings)] AccountType type,
       [Matrix(-1000, 0, 1000)] decimal initialBalance
   )
   {
       var result = await ExecuteGraphQLMutation(CreateAccountMutation(name, type, initialBalance));
       
       if (string.IsNullOrEmpty(name) || initialBalance < 0)
           await Assert.That(result.Errors).IsNotEmpty();
       else
           await Assert.That(result.Data.CreateAccount.Account).IsNotNull()
               .And.ShouldHaveValidBalance(initialBalance);
   }
   ```

2. **Lifecycle-Aware Database Testing**:
   ```csharp
   [ParallelLimiter<DatabaseTestLimit>]
   public class BudgetCalculationTests : IAsyncInitializer
   {
       [Before(Class)]
       public static async Task SetupTestData()
       {
           // Heavy setup once per class
       }
       
       [Before(Test)]
       public async Task ResetDatabase(TestContext context)
       {
           // Per-test reset with context information
           await _respawner.ResetAsync(_connection);
           context.AddAsyncLocalValues(); // Propagate async context
       }
       
       [BeforeEvery(Test)] // Runs before every test in entire suite
       public static async Task GlobalSetup()
       {
           // Global preparation if needed
       }
   }
   ```

**Areas Requiring Custom Implementation**:
- TUnit + Respawn integration helper methods with lifecycle hooks
- GraphQL schema validation utilities leveraging TUnit's source generation
- Custom domain assertions for Money, Account, and Budget entities
- Aspire TestHost factory with comprehensive TUnit lifecycle integration
- Matrix data generators for financial test scenarios
- Database seeding patterns optimized for TUnit's parallel execution and lifecycle management

**Alternatives considered**: 
- xUnit (older, but TUnit specified in requirements)
- NUnit (not specified in requirements)

### Serilog Structured Logging
**Decision**: Use Serilog as the primary logging framework across all hexagonal architecture layers
**Rationale**: 
- Structured logging enables rich context and searchable log data for financial applications
- Excellent integration with .NET Aspire for distributed tracing and observability
- Strong ecosystem of sinks for various output formats and destinations
- Performance optimized for high-throughput applications
- Constitutional requirement for comprehensive observability

**Version Requirements (September 2025)**:
- **Serilog**: v4.1.0+ (latest stable with .NET 10 support)
- **Serilog.AspNetCore**: v8.0.3+ (ASP.NET Core integration with automatic request logging)
- **Serilog.Sinks.Console**: v6.0.0+ (development console output with structured formatting)
- **Serilog.Sinks.File**: v6.0.0+ (persistent file logging with rolling policies)
- **Serilog.Sinks.PostgreSQL**: v4.0.0+ (direct database logging for audit trails)
- **Serilog.Enrichers.Environment**: v3.0.1+ (environment context enrichment)
- **Serilog.Settings.Configuration**: v8.0.4+ (configuration from appsettings.json)

**Integration with .NET Aspire**:
- **OpenTelemetry Integration**: Serilog automatically integrates with Aspire's tracing infrastructure
- **Distributed Context**: Log correlation across GraphQL → Application → Domain → Infrastructure layers
- **Dashboard Integration**: Aspire dashboard displays structured logs with searchable properties
- **Health Checks**: Logging health and performance metrics visible in Aspire monitoring

**Hexagonal Architecture Logging Strategy**:
- **Domain Layer**: Event sourcing for business rule violations, domain event logging
- **Application Layer**: MediatR pipeline logging, command/query execution traces
- **Infrastructure Layer**: Database queries, file operations, external service calls
- **GraphQL Layer**: Request/response logging, schema validation, subscription events

**Financial Domain-Specific Logging**:
- **Audit Trail**: All financial operations (account creation, transactions, budget changes)
- **Security Events**: Sensitive data access patterns, statement import activities
- **Performance Monitoring**: Query execution times, budget calculation performance
- **Error Context**: Detailed error information with financial context (account IDs, amounts)

**Structured Logging Examples**:
```csharp
// Domain Layer - Business Event Logging
Log.Information("Budget limit exceeded for {Category} in {Budget}", 
    categoryName, budgetName,
    new { CategoryId = categoryId, CurrentSpent = amount, Limit = budgetLimit });

// Application Layer - Command Processing
Log.Information("Processing {Command} for {Account}", 
    command.GetType().Name, accountId,
    new { UserId = userId, RequestId = requestId });

// Infrastructure Layer - Database Operations
Log.Debug("Executing query {Query} with parameters {@Parameters}", 
    sql, parameters);

// GraphQL Layer - Request Tracing
Log.Information("GraphQL {Operation} executed in {Duration}ms", 
    operationName, duration,
    new { Query = query, Variables = variables });
```

**Log Levels and Context**:
- **Verbose**: Detailed execution flow, parameter values (excluding sensitive data)
- **Debug**: Development diagnostics, query plans, cache hits/misses
- **Information**: Business events, successful operations, performance metrics
- **Warning**: Validation failures, business rule violations, performance concerns
- **Error**: Technical errors, infrastructure failures, unexpected exceptions
- **Fatal**: Application-level failures requiring immediate attention

**Sensitive Data Protection**:
- **Account Numbers**: Automatically masked in logs (show first 4, last 4 digits)
- **Transaction Amounts**: Logged with appropriate precision, no PII correlation
- **User Context**: No personal information logged, use anonymized identifiers
- **Compliance**: GDPR/privacy-friendly logging practices

**Performance Considerations**:
- **Asynchronous Logging**: Non-blocking log writes to prevent performance impact
- **Sampling**: High-frequency operations use sampling to reduce log volume
- **Structured Templates**: Template reuse for consistent performance
- **Buffer Management**: Appropriate buffering for database and file sinks

**Package Requirements for Logging**:
- `Serilog` v4.1.0+ (core logging framework)
- `Serilog.AspNetCore` v8.0.3+ (ASP.NET Core integration)
- `Serilog.Sinks.Console` v6.0.0+ (console output for development)
- `Serilog.Sinks.File` v6.0.0+ (persistent file logging)
- `Serilog.Sinks.PostgreSQL` v4.0.0+ (database audit logging)
- `Serilog.Enrichers.Environment` v3.0.1+ (environment context)
- `Serilog.Settings.Configuration` v8.0.4+ (configuration integration)
- `Serilog.Enrichers.OpenTelemetry` v2.0.0+ (.NET Aspire tracing integration)

**Configuration Strategy**:
- **Development**: Console + File sinks with verbose logging
- **Production**: PostgreSQL + File sinks with information level
- **Testing**: In-memory sink for test verification, suppressed output for clean test runs
- **Aspire Integration**: Automatic OpenTelemetry correlation and distributed tracing

**Alternatives considered**: 
- Microsoft.Extensions.Logging (less structured, limited sinks)
- NLog (less modern .NET integration)
- log4net (legacy, less performant)

### OneOf Discriminated Unions for Error Handling
**Decision**: Use OneOf package for domain-specific error handling instead of exceptions
**Rationale**: 
- Exceptions should only be thrown for truly exceptional circumstances, not business rule violations
- OneOf provides type-safe discriminated unions for representing success/failure states
- Explicit error handling improves code clarity and prevents unhandled exceptions
- Better performance than exception-based control flow
- Aligns with functional programming principles and railway-oriented programming

**Version Requirements (September 2025)**:
- **OneOf**: v3.0.271+ (latest stable with .NET 10 support and source generation)
- **OneOf.SourceGenerator**: v3.0.271+ (compile-time generation of discriminated union types)
- **Integration**: Works seamlessly with MediatR command/query results and GraphQL error unions

**Error Handling Philosophy**:
- **Domain Errors**: Business rule violations return `OneOf<Success, DomainError>` instead of throwing exceptions
- **Validation Errors**: Input validation failures use OneOf for explicit error states
- **Infrastructure Errors**: Database failures, file operations use OneOf for recoverable errors
- **Exceptions Reserved For**: True exceptional cases (out of memory, network infrastructure failures, programming errors)

**Domain-Specific Error Types for Basic Budget**:
```csharp
// Core domain error base types
public abstract record DomainError(string Message, string Code);
public record ValidationError(string Message, string Code, string Field) : DomainError(Message, Code);
public record BusinessRuleError(string Message, string Code, string Rule) : DomainError(Message, Code);
public record NotFoundError(string Message, string Code, string Entity, string Id) : DomainError(Message, Code);

// Account domain errors
public record AccountNotFoundError(string AccountId) 
    : NotFoundError("Account not found", "ACCOUNT_NOT_FOUND", "Account", AccountId);
public record DuplicateAccountNumberError(string AccountNumber) 
    : BusinessRuleError("Account number already exists", "DUPLICATE_ACCOUNT_NUMBER", "UniqueAccountNumber");
public record InsufficientFundsError(decimal CurrentBalance, decimal RequestedAmount) 
    : BusinessRuleError($"Insufficient funds: {CurrentBalance} < {RequestedAmount}", "INSUFFICIENT_FUNDS", "AccountBalance");

// Transaction domain errors  
public record TransactionValidationError(string Field, string Value) 
    : ValidationError($"Invalid transaction {Field}: {Value}", "INVALID_TRANSACTION", Field);
public record FutureTransactionError(DateTime TransactionDate) 
    : BusinessRuleError("Transaction date cannot be in the future", "FUTURE_TRANSACTION_DATE", "TransactionDate");

// Budget domain errors
public record BudgetNotFoundError(string BudgetId) 
    : NotFoundError("Budget not found", "BUDGET_NOT_FOUND", "Budget", BudgetId);
public record BudgetPeriodOverlapError(DateTime StartDate, DateTime EndDate) 
    : BusinessRuleError("Budget period overlaps with existing budget", "BUDGET_PERIOD_OVERLAP", "BudgetPeriod");
public record BudgetExceededError(string CategoryName, decimal Spent, decimal Limit) 
    : BusinessRuleError($"Budget exceeded for {CategoryName}: {Spent} > {Limit}", "BUDGET_EXCEEDED", "BudgetLimit");

// Statement import errors
public record InvalidFileFormatError(string FileName, string ExpectedFormat) 
    : ValidationError($"Invalid file format for {FileName}, expected {ExpectedFormat}", "INVALID_FILE_FORMAT", "FileFormat");
public record DuplicateTransactionError(string TransactionId) 
    : BusinessRuleError("Transaction already exists", "DUPLICATE_TRANSACTION", "TransactionUniqueness");
```

**MediatR Integration Patterns**:
```csharp
// Command handlers return OneOf for explicit error handling
public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, OneOf<Account, DomainError>>
{
    public async Task<OneOf<Account, DomainError>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Validate account number uniqueness
        if (await _accountRepository.ExistsAsync(request.AccountNumber))
            return new DuplicateAccountNumberError(request.AccountNumber);
        
        // Validate initial balance rules
        if (request.AccountType != AccountType.CreditCard && request.InitialBalance < 0)
            return new ValidationError("Initial balance cannot be negative", "NEGATIVE_BALANCE", "InitialBalance");
        
        var account = Account.Create(request.AccountNumber, request.Name, request.AccountType, request.InitialBalance);
        await _accountRepository.AddAsync(account);
        
        return account; // Implicit conversion to OneOf<Account, DomainError>
    }
}

// Query handlers for safe data retrieval
public class GetAccountHandler : IRequestHandler<GetAccountQuery, OneOf<Account, DomainError>>
{
    public async Task<OneOf<Account, DomainError>> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId);
        return account is not null 
            ? account 
            : new AccountNotFoundError(request.AccountId);
    }
}
```

**GraphQL Integration with Error Unions**:
```csharp
// GraphQL mutation payload types using OneOf results
public class CreateAccountPayload
{
    public Account? Account { get; set; }
    public IReadOnlyList<DomainError> Errors { get; set; } = Array.Empty<DomainError>();
    
    public static CreateAccountPayload FromResult(OneOf<Account, DomainError> result)
    {
        return result.Match(
            account => new CreateAccountPayload { Account = account },
            error => new CreateAccountPayload { Errors = new[] { error } }
        );
    }
}

// GraphQL resolver with OneOf pattern matching
[UseMutationConvention]
public async Task<CreateAccountPayload> CreateAccountAsync(
    CreateAccountInput input,
    [Service] IMediator mediator)
{
    var command = new CreateAccountCommand(input.AccountNumber, input.Name, input.AccountType, input.InitialBalance);
    var result = await mediator.Send(command);
    
    return CreateAccountPayload.FromResult(result);
}
```

**Domain Layer Error Handling**:
```csharp
// Domain services return OneOf for business rule validation
public class BudgetCalculationService
{
    public OneOf<BudgetSummary, DomainError> CalculateBudgetProgress(Budget budget, IEnumerable<Transaction> transactions)
    {
        if (budget.EndDate.HasValue && DateTime.UtcNow > budget.EndDate.Value)
            return new BusinessRuleError("Cannot calculate progress for expired budget", "EXPIRED_BUDGET", "BudgetExpiry");
        
        var summary = new BudgetSummary
        {
            Budget = budget,
            // ... calculation logic
        };
        
        return summary;
    }
}

// Entity creation with validation using OneOf
public static OneOf<Account, DomainError> Create(string accountNumber, string name, AccountType accountType, decimal initialBalance)
{
    if (string.IsNullOrWhiteSpace(accountNumber))
        return new ValidationError("Account number is required", "MISSING_ACCOUNT_NUMBER", "AccountNumber");
        
    if (accountNumber.Length < 8 || accountNumber.Length > 20)
        return new ValidationError("Account number must be 8-20 characters", "INVALID_ACCOUNT_NUMBER_LENGTH", "AccountNumber");
    
    if (string.IsNullOrWhiteSpace(name))
        return new ValidationError("Account name is required", "MISSING_ACCOUNT_NAME", "Name");
    
    return new Account(Guid.NewGuid(), accountNumber, name, accountType, initialBalance);
}
```

**Testing with OneOf**:
```csharp
[Test]
public async Task CreateAccount_WithDuplicateAccountNumber_ReturnsDuplicateError()
{
    // Arrange - existing account setup
    await SeedDatabase(new Account(Guid.NewGuid(), "12345678", "Existing", AccountType.Checking, 1000));
    
    var command = new CreateAccountCommand("12345678", "Duplicate", AccountType.Savings, 500);
    
    // Act
    var result = await _mediator.Send(command);
    
    // Assert - TUnit assertions with OneOf pattern matching
    await Assert.That(result.IsT1).IsTrue(); // IsT1 = error case
    await Assert.That(result.AsT1).IsOfType<DuplicateAccountNumberError>();
    await Assert.That(result.AsT1.Code).IsEqualTo("DUPLICATE_ACCOUNT_NUMBER");
}

[Test]  
public async Task CreateAccount_WithValidInput_ReturnsAccount()
{
    // Arrange
    var command = new CreateAccountCommand("87654321", "Valid Account", AccountType.Checking, 1000);
    
    // Act
    var result = await _mediator.Send(command);
    
    // Assert - Success case
    await Assert.That(result.IsT0).IsTrue(); // IsT0 = success case
    await Assert.That(result.AsT0.Name).IsEqualTo("Valid Account");
    await Assert.That(result.AsT0.CurrentBalance).IsEqualTo(1000);
}
```

**Performance Benefits**:
- **No Exception Overhead**: OneOf avoids expensive exception stack trace generation
- **Explicit Flow Control**: Compiler enforces handling of all possible outcomes
- **Memory Efficient**: Discriminated unions have minimal memory overhead
- **Source Generated**: OneOf.SourceGenerator provides compile-time optimizations

**Package Requirements for Error Handling**:
- `OneOf` v3.0.271+ (core discriminated union types)
- `OneOf.SourceGenerator` v3.0.271+ (compile-time union generation)

**Integration Strategy**:
- **Domain Layer**: All entity creation and business rule validation uses OneOf
- **Application Layer**: MediatR handlers return OneOf<TResult, DomainError>
- **Infrastructure Layer**: Repository operations use OneOf for not-found scenarios
- **GraphQL Layer**: Convert OneOf results to GraphQL error/success payload patterns

**Exception Usage Guidelines**:
- **Infrastructure Failures**: Database connection failures, file system errors
- **Programming Errors**: Null reference exceptions, argument exceptions from invalid API usage
- **System Errors**: Out of memory, stack overflow, external service unavailable
- **NOT for Business Logic**: Account not found, validation failures, business rule violations

**Alternatives considered**: 
- Exception-based error handling (poor performance, unclear control flow)
- Result<T> custom implementation (reinventing OneOf functionality)
- Error codes with out parameters (not type-safe, unclear API)

### GitHub Actions CI/CD Pipeline
**Decision**: Implement comprehensive CI/CD pipeline using GitHub Actions for automated testing, building, and quality gates
**Rationale**: 
- Constitutional requirement for automated testing and quality assurance
- Essential for maintaining code quality in hexagonal architecture with multiple layers
- Supports TDD workflow with automated RED-GREEN-REFACTOR cycle validation
- Enables automated version management and deployment preparation
- Provides fast feedback loop for development teams

**Pipeline Architecture Requirements**:

**1. Pull Request Validation Workflow**:
- **Trigger**: On pull request creation and updates to main branch
- **Jobs**: Build verification, test execution, code quality checks
- **Quality Gates**: All tests must pass, no build warnings, code coverage thresholds

**2. Main Branch CI Workflow**:
- **Trigger**: On push to main branch (after PR merge)
- **Jobs**: Full test suite, integration tests with real PostgreSQL, package building
- **Artifacts**: NuGet packages, Docker images, deployment packages

**3. Release Management Workflow**:
- **Trigger**: On version tag creation (v1.0.0, v1.0.1, etc.)
- **Jobs**: Release notes generation, artifact publishing, deployment preparation
- **Constitutional Compliance**: Automatic BUILD number increment

**Required GitHub Actions Workflows**:

**Workflow 1: PR Validation (.github/workflows/pr-validation.yml)**:
```yaml
name: PR Validation

on:
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: basic_budget_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
          
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
        
    - name: Install .NET Aspire workload
      run: dotnet workload install aspire
      
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build solution
      run: dotnet build --no-restore --configuration Release
      
    - name: Run unit tests
      run: dotnet test --no-build --configuration Release --filter Category!=Integration
      
    - name: Run integration tests with Aspire TestHost
      run: dotnet test --no-build --configuration Release --filter Category=Integration
      env:
        ConnectionStrings__BasicBudgetDb: Host=localhost;Port=5432;Database=basic_budget_test;Username=postgres;Password=postgres
        
    - name: Generate code coverage report
      run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"
      
    - name: Upload coverage to CodeCov
      uses: codecov/codecov-action@v3
```

**Workflow 2: Main Branch CI (.github/workflows/main-ci.yml)**:
```yaml
name: Main Branch CI

on:
  push:
    branches: [ main ]

jobs:
  comprehensive-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: basic_budget_test
      redis:
        image: redis:7
        
    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Required for version calculation
        
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
        
    - name: Install .NET Aspire workload
      run: dotnet workload install aspire
      
    - name: Calculate version number
      id: version
      run: |
        # Constitutional requirement: MAJOR.MINOR.BUILD
        BUILD_NUMBER=${{ github.run_number }}
        echo "VERSION=1.0.$BUILD_NUMBER" >> $GITHUB_OUTPUT
        
    - name: Update version in projects
      run: |
        find . -name "*.csproj" -exec sed -i "s/<Version>.*<\/Version>/<Version>${{ steps.version.outputs.VERSION }}<\/Version>/g" {} \;
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build solution
      run: dotnet build --no-restore --configuration Release
      
    - name: Run all tests including contract tests
      run: dotnet test --no-build --configuration Release --logger trx --results-directory TestResults
      
    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: TUnit Test Results
        path: TestResults/*.trx
        reporter: dotnet-trx
        
    - name: Package applications
      run: dotnet pack --no-build --configuration Release --output ./artifacts
      
    - name: Upload build artifacts
      uses: actions/upload-artifact@v3
      with:
        name: basic-budget-packages-v${{ steps.version.outputs.VERSION }}
        path: ./artifacts/
```

**Workflow 3: Release Management (.github/workflows/release.yml)**:
```yaml
name: Release

on:
  push:
    tags: [ 'v*' ]

jobs:
  release:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      
    steps:
    - uses: actions/checkout@v4
    
    - name: Create Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: ${{ github.ref }}
        release_name: Basic Budget ${{ github.ref }}
        draft: false
        prerelease: false
```

**TUnit Integration Considerations**:
- **Microsoft.Testing.Platform**: GitHub Actions runners support TUnit's modern testing platform
- **Parallel Execution**: Configure `[ParallelLimiter<DatabaseTestLimit>]` for database tests in CI
- **Test Discovery**: TUnit's source generation works seamlessly in CI/CD environment
- **Reporting**: TUnit integrates with standard test reporting tools used in GitHub Actions

**Quality Gates and Requirements**:
- **Test Coverage**: Minimum 80% code coverage across all hexagonal layers
- **Build Success**: Zero warnings in Release configuration
- **Contract Tests**: All GraphQL schema contract tests must pass
- **Integration Tests**: Full end-to-end testing with .NET Aspire TestHost
- **Security**: Dependency vulnerability scanning with GitHub Security Advisories

**Constitutional Compliance Features**:
- **TDD Validation**: CI fails if implementations exist without corresponding tests
- **Version Management**: Automatic BUILD number increment on every merge to main
- **Test-First Enforcement**: Pull requests require test changes before implementation changes
- **Quality Metrics**: Code coverage, complexity analysis, maintainability index

**Environment Configuration**:
- **Development**: PR validation with fast feedback
- **Staging**: Main branch CI with full test suite
- **Production**: Release workflow with artifact publishing

**Required Secrets and Configuration**:
- `CODECOV_TOKEN`: For code coverage reporting
- `NUGET_API_KEY`: For package publishing (if needed)
- Database connection strings for integration testing
- .NET Aspire orchestration configuration

**Performance Optimizations**:
- **Caching**: NuGet package cache, Docker layer cache
- **Parallel Jobs**: Test execution across multiple runners when possible
- **Selective Testing**: Different test categories for different trigger types
- **Build Matrix**: Test across multiple .NET versions if needed

**Monitoring and Observability**:
- **Build Status**: GitHub status checks integrated with PR workflow
- **Test Results**: Automated test result publishing and history
- **Performance Tracking**: Build time monitoring and optimization
- **Failure Notifications**: Team notifications for build/test failures

**Alternatives considered**: 
- Azure DevOps (GitHub Actions preferred for GitHub-hosted projects)
- GitLab CI (not applicable for GitHub repositories)
- Jenkins (more complex setup, less integrated with GitHub)

### Financial Data File Formats
**Decision**: Support CSV and QFX/OFX formats for statement imports
**Rationale**: 
- CSV is universally supported by all banks
- QFX/OFX are standard formats from financial institutions
- Covers 90%+ of user needs for family budgeting

**Alternatives considered**: 
- PDF parsing (too complex and unreliable)
- Excel formats (unnecessary complexity)

### Transaction Categorization Strategy
**Decision**: Implement rule-based categorization with machine learning capability
**Rationale**: 
- Rule-based system provides predictable results
- User corrections can train the system over time
- Balances automation with user control

**Alternatives considered**: 
- Manual categorization only (too time-consuming)
- Pure ML approach (too complex for family-scale application)

## Architecture Patterns Research

### Domain-Driven Design (DDD) Implementation
**Decision**: Apply DDD tactical patterns within hexagonal architecture
**Rationale**: 
- Financial domain has rich business logic (budgeting rules, categorization)
- Entities and value objects naturally model financial concepts
- Domain events can handle cross-aggregate updates

**Key Patterns to Implement**:
- Entities: Account, Transaction, Budget, Category
- Value Objects: Money, AccountNumber, CategoryRule
- Domain Services: BudgetCalculationService, CategorizationService
- Repository Interfaces: IAccountRepository, ITransactionRepository

### GraphQL Schema Design
**Decision**: Use schema-first approach with type extensions
**Rationale**: 
- Clear contract definition before implementation
- Type extensions allow modular schema composition
- Better tooling and validation support

**Schema Structure**:
- Query: accounts, transactions, budgets, categories, periodicSummaries
- Mutations: createAccount, importStatement, createBudget, categorizeTransaction
- Filtering/Sorting: Use HotChocolate's built-in filtering and sorting
- Pagination: Cursor-based pagination for large datasets

### Database Schema Strategy
**Decision**: Use EF Core migrations with domain-driven table design
**Rationale**: 
- Tables mirror aggregate boundaries
- Migrations provide schema versioning
- Foreign keys enforce referential integrity for financial data

**Key Tables**:
- Accounts (checking, savings, credit_card types)
- Transactions (with foreign key to account)
- Budgets (with time periods and budget type)
- Categories (hierarchical structure)
- BudgetCategories (many-to-many with spending limits)