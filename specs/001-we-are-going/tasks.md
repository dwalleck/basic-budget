# Tasks: Basic Budget - Family Budgeting Tool

**Input**: Design documents from `/home/dwalleck/repos/basic-budget/specs/001-we-are-going/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/ (✓), quickstart.md (✓)

## Execution Flow (main)

```
1. Load plan.md from feature directory
   → ✓ Found: .NET 10, HotChocolate GraphQL, Hexagonal Architecture, MediatR, EF Core, PostgreSQL
   → Extract: Aspire orchestration, Serilog logging, OneOf error handling, TUnit testing
2. Load optional design documents:
   → data-model.md: Extract entities → Account, Transaction, Budget, Category, Money, AccountNumber
   → contracts/: graphql-schema.graphql → contract test tasks for all operations
   → research.md: Extract decisions → setup tasks for all technologies
3. Generate tasks by category:
   → Setup: project init, Aspire, dependencies, CI/CD
   → Tests: contract tests for GraphQL operations, integration tests
   → Core: domain entities, application handlers, infrastructure
   → Integration: EF Core, file parsing, GraphQL resolvers
   → Polish: unit tests, performance, documentation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (Constitutional TDD requirement)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests? ✓
   → All entities have models? ✓
   → All endpoints implemented? ✓
9. Return: SUCCESS (46 tasks ready for execution)
```

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md hexagonal architecture structure:

- **Domain**: `src/BasicBudget.Domain/`
- **Application**: `src/BasicBudget.Application/`
- **Infrastructure**: `src/BasicBudget.Infrastructure/`
- **GraphQL**: `src/BasicBudget.GraphQL/`
- **AppHost**: `src/BasicBudget.AppHost/`
- **Tests**: `tests/BasicBudget.[Layer].Tests/`

## Phase 3.1: Setup & Project Initialization

- [x] **T001** Create project structure following hexagonal architecture layout in `src/` and `tests/` directories as defined in plan.md lines 119-154
- [x] **T002** [P] Initialize BasicBudget.Domain class library with .NET 10 and add OneOf v3.0.271+ package for error handling
- [x] **T003** [P] Initialize BasicBudget.Application class library with .NET 10 and add MediatR v12.4.0+ package for CQRS
- [x] **T004** [P] Initialize BasicBudget.Infrastructure class library with .NET 10 and add Entity Framework Core, Npgsql, and Serilog packages per research.md lines 186-204
- [x] **T005** [P] Initialize BasicBudget.GraphQL class library with .NET 10 and add HotChocolate.AspNetCore v15.1.10+ packages per research.md lines 190-193
- [x] **T006** [P] Initialize BasicBudget.AppHost project with .NET 10 and add .NET Aspire 9.4.x packages per research.md lines 187-189
- [x] **T007** [P] Configure solution file linking all projects with proper dependencspecs/001-we-are-going/tasks.mdies following hexagonal architecture (Domain ← Application ← Infrastructure/GraphQL)
- [x] **T008** [P] Setup GitHub Actions CI/CD workflows in `.github/workflows/` using templates from research.md lines 738-890 (pr-validation.yml, main-ci.yml, release.yml)

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

### Contract Tests (GraphQL Schema Validation)

- [x] **T009** [P] Create TUnit test project `tests/BasicBudget.GraphQL.Tests/` with Aspire.Hosting.Testing and TUnit v0.57.24+ packages
- [x] **T010** [P] Create GraphQL contract test for Query.accounts operation in `tests/BasicBudget.GraphQL.Tests/QueryTests.cs` - MUST FAIL initially
- [x] **T011** [P] Create GraphQL contract test for Query.account(id) operation in `tests/BasicBudget.GraphQL.Tests/QueryTests.cs` - MUST FAIL initially
- [x] **T012** [P] Create GraphQL contract test for Query.transactions with filtering in `tests/BasicBudget.GraphQL.Tests/QueryTests.cs` - MUST FAIL initially
- [x] **T013** [P] Create GraphQL contract test for Query.budgets operation in `tests/BasicBudget.GraphQL.Tests/QueryTests.cs` - MUST FAIL initially
- [x] **T014** [P] Create GraphQL contract test for Mutation.createAccount in `tests/BasicBudget.GraphQL.Tests/MutationTests.cs` - MUST FAIL initially
- [x] **T015** [P] Create GraphQL contract test for Mutation.createTransaction in `tests/BasicBudget.GraphQL.Tests/MutationTests.cs` - MUST FAIL initially
- [x] **T016** [P] Create GraphQL contract test for Mutation.importStatement in `tests/BasicBudget.GraphQL.Tests/MutationTests.cs` - MUST FAIL initially
- [x] **T017** [P] Create GraphQL contract test for Mutation.createBudget in `tests/BasicBudget.GraphQL.Tests/MutationTests.cs` - MUST FAIL initially
- [x] **T018** [P] Create GraphQL contract test for Subscription.transactionAdded in `tests/BasicBudget.GraphQL.Tests/SubscriptionTests.cs` - MUST FAIL initially
- [x] **T019** [P] Create GraphQL contract test for Subscription.budgetAlertAdded in `tests/BasicBudget.GraphQL.Tests/SubscriptionTests.cs` - MUST FAIL initially

### Integration Tests (Quickstart Scenarios)

- [x] **T020** [P] Create TUnit integration test project `tests/BasicBudget.IntegrationTests/` with Aspire TestHost, Respawn v6.2.1+, and ParallelLimiter setup
- [x] **T021** [P] Create integration test for Scenario 1 (Create First Account) in `tests/BasicBudget.IntegrationTests/AccountScenarioTests.cs` - MUST FAIL initially
- [x] **T022** [P] Create integration test for Scenario 2 (Add Manual Transaction) in `tests/BasicBudget.IntegrationTests/TransactionScenarioTests.cs` - MUST FAIL initially
- [x] **T023** [P] Create integration test for Scenario 3 (Create Spending Categories) in `tests/BasicBudget.IntegrationTests/CategoryScenarioTests.cs` - MUST FAIL initially
- [x] **T024** [P] Create integration test for Scenario 4 (Create Monthly Budget) in `tests/BasicBudget.IntegrationTests/BudgetScenarioTests.cs` - MUST FAIL initially

## Phase 3.3: Domain Layer Implementation

- [x] **T025** [P] Create Money value object in `src/BasicBudget.Domain/ValueObjects/Money.cs` with validation and OneOf error handling per data-model.md lines 156-164
- [x] **T026** [P] Create AccountNumber value object in `src/BasicBudget.Domain/ValueObjects/AccountNumber.cs` with masking and validation per data-model.md lines 165-175
- [x] **T027** [P] Create Account entity in `src/BasicBudget.Domain/Entities/Account.cs` with business rules per data-model.md lines 6-24
- [x] **T028** [P] Create Transaction entity in `src/BasicBudget.Domain/Entities/Transaction.cs` with business rules per data-model.md lines 26-46
- [x] **T029** [P] Create Budget entity in `src/BasicBudget.Domain/Entities/Budget.cs` with business rules per data-model.md lines 48-67
- [x] **T030** [P] Create Category entity in `src/BasicBudget.Domain/Entities/Category.cs` with hierarchical structure per data-model.md lines 69-87
- [x] **T031** [P] Create BudgetCategory entity in `src/BasicBudget.Domain/Entities/BudgetCategory.cs` with spending calculations per data-model.md lines 89-106
- [x] **T032** [P] Create domain error types in `src/BasicBudget.Domain/Errors/DomainErrors.cs` using OneOf patterns from research.md lines 509-543
- [x] **T033** [P] Create repository interfaces in `src/BasicBudget.Domain/Repositories/` for Account, Transaction, Budget, Category per data-model.md lines 194-235
- [x] **T034** [P] Create BudgetCalculationService in `src/BasicBudget.Domain/Services/BudgetCalculationService.cs` per data-model.md lines 178-184

## Phase 3.4: Application Layer Implementation

- [x] **T035** Create CreateAccountCommand and handler in `src/BasicBudget.Application/Commands/CreateAccountCommand.cs` returning OneOf<Account, DomainError>
- [x] **T036** Create CreateTransactionCommand and handler in `src/BasicBudget.Application/Commands/CreateTransactionCommand.cs` with validation
- [x] **T037** Create ImportStatementCommand and handler in `src/BasicBudget.Application/Commands/ImportStatementCommand.cs` for CSV/QFX processing
- [x] **T038** Create CreateBudgetCommand and handler in `src/BasicBudget.Application/Commands/CreateBudgetCommand.cs` with period validation
- [x] **T039** Create GetAccountQuery and handler in `src/BasicBudget.Application/Queries/GetAccountQuery.cs` with OneOf error handling
- [x] **T040** Create GetTransactionsQuery and handler in `src/BasicBudget.Application/Queries/GetTransactionsQuery.cs` with filtering support
- [x] **T041** Create GetBudgetProgressQuery and handler in `src/BasicBudget.Application/Queries/GetBudgetProgressQuery.cs` with calculations

## Phase 3.5: Infrastructure Layer Implementation

- [x] **T042** Create BasicBudgetDbContext in `src/BasicBudget.Infrastructure/Persistence/BasicBudgetDbContext.cs` with entity configurations and Serilog integration
- [x] **T043** Create AccountRepository implementation in `src/BasicBudget.Infrastructure/Persistence/AccountRepository.cs` implementing IAccountRepository
- [x] **T044** Create TransactionRepository implementation in `src/BasicBudget.Infrastructure/Persistence/TransactionRepository.cs` with LINQ filtering
- [x] **T045** Create CSV statement parser in `src/BasicBudget.Infrastructure/FileSystem/CsvStatementParser.cs` per functional requirements FR-003, FR-004
- [x] **T045a** Create missing Domain layer types: AlertType enum, AlertThreshold value object in `src/BasicBudget.Domain/ValueObjects/`
- [x] **T045b** Create IBudgetCalculationService interface in `src/BasicBudget.Domain/Services/IBudgetCalculationService.cs`
- [x] **T045c** Create missing repository interfaces: IBudgetRepository, ICategoryRepository, ITransactionRepository in `src/BasicBudget.Domain/Repositories/`
- [x] **T045d** Add TransactionFilterCriteria and TransactionSortCriteria to Domain layer `src/BasicBudget.Domain/ValueObjects/`
- [ ] **T046** Create database migrations for all entities using `dotnet ef migrations add InitialCreate -p src/BasicBudget.Infrastructure`

## Phase 3.6: GraphQL Layer Implementation

- [ ] **T047** Create GraphQL types in `src/BasicBudget.GraphQL/Types/` matching domain entities with HotChocolate attributes
- [ ] **T048** Create Query resolvers in `src/BasicBudget.GraphQL/Queries/QueryResolvers.cs` delegating to MediatR handlers
- [ ] **T049** Create Mutation resolvers in `src/BasicBudget.GraphQL/Mutations/MutationResolvers.cs` with OneOf error conversion
- [ ] **T050** Create Subscription resolvers in `src/BasicBudget.GraphQL/Subscriptions/SubscriptionResolvers.cs` for real-time budget alerts
- [ ] **T051** Configure HotChocolate server in `src/BasicBudget.GraphQL/Program.cs` with filtering, sorting, and subscriptions

## Phase 3.7: .NET Aspire Orchestration

- [ ] **T052** Configure Aspire AppHost in `src/BasicBudget.AppHost/Program.cs` with PostgreSQL and Redis services per research.md lines 159-176
- [ ] **T053** Configure service discovery and health checks for GraphQL API in AppHost project

## Phase 3.8: Final Integration & Polish

- [ ] **T054** [P] Create unit tests for domain entities in `tests/BasicBudget.Domain.Tests/` using TUnit with matrix testing for validation scenarios
- [ ] **T055** [P] Create unit tests for application handlers in `tests/BasicBudget.Application.Tests/` with mocked dependencies
- [ ] **T056** [P] Add comprehensive Serilog configuration across all layers per research.md lines 383-481
- [ ] **T057** [P] Create performance tests for GraphQL operations ensuring <500ms response times
- [ ] **T058** [P] Update README.md with quickstart instructions and architecture overview

---

## Dependencies Graph

```
Setup (T001-T008) → Tests (T009-T024) → Domain (T025-T034) → Application (T035-T041) → Infrastructure (T042-T046) → GraphQL (T047-T051) → Aspire (T052-T053) → Polish (T054-T058)
```

## Parallel Execution Examples

**Phase 3.1 Setup - All parallel**:

```bash
# Can run simultaneously (different projects)
Task T002, T003, T004, T005, T006, T007, T008
```

**Phase 3.2 Contract Tests - All parallel**:

```bash
# Can run simultaneously (different test files)
Task T009, T010, T011, T012, T013, T014, T015, T016, T017, T018, T019, T020, T021, T022, T023, T024
```

**Phase 3.3 Domain - All parallel**:

```bash
# Can run simultaneously (different entity files)
Task T025, T026, T027, T028, T029, T030, T031, T032, T033, T034
```

**Phase 3.8 Polish - All parallel**:

```bash
# Can run simultaneously (different areas)
Task T054, T055, T056, T057, T058
```

## Task Validation

✅ All GraphQL operations have contract tests (T010-T019)
✅ All entities have implementation tasks (T025-T034)
✅ All quickstart scenarios have integration tests (T021-T024)
✅ Constitutional TDD requirement: Tests before implementation
✅ Hexagonal architecture: Proper dependency flow (Domain ← Application ← Infrastructure/GraphQL)
✅ All technology stack components covered: Aspire, HotChocolate, MediatR, EF Core, Serilog, OneOf, TUnit

**Total Tasks**: 58 tasks ready for execution following constitutional TDD principles
