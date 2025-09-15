# Implementation Plan: Basic Budget - Family Budgeting Tool

**Branch**: `001-we-are-going` | **Date**: 2025-09-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-we-are-going/spec.md`

## Execution Flow (/plan command scope)

```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
4. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
5. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, or `GEMINI.md` for Gemini CLI).
6. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
7. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
8. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:

- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary

Develop a single-user family budgeting tool that manages multiple financial accounts (checking, savings, credit cards), imports bank statements (CSV, QFX/OFX), and provides basic/complex budgeting with spending category tracking and goal monitoring. Using .NET 10 + .NET Aspire 9.4+ orchestration + HotChocolate GraphQL + Entity Framework + PostgreSQL + hexagonal architecture with MediatR + OneOf error handling + Serilog structured logging + TUnit testing with Aspire TestHost integration.

## Technical Context

**Language/Version**: .NET 10
**Primary Dependencies**: HotChocolate (GraphQL), MediatR (CQRS/mediator), Entity Framework Core (ORM), .NET Aspire (local development orchestration), Serilog (structured logging), OneOf (discriminated unions for error handling)
**Storage**: PostgreSQL database (managed via .NET Aspire for local development)
**Testing**: TUnit testing framework
**Development Orchestration**: .NET Aspire for local testing and development environment
**Logging**: Serilog with structured logging across all hexagonal layers
**Error Handling**: OneOf discriminated unions for domain errors, exceptions only for exceptional circumstances
**CI/CD**: GitHub Actions for automated testing, building, and quality gates
**Target Platform**: Cross-platform server application (Linux/Windows/macOS)
**Project Type**: single - GraphQL API server with hexagonal architecture + Aspire AppHost for orchestration
**Performance Goals**: Support family-scale financial data (thousands of transactions), <500ms query response time
**Constraints**: Local single-user application (no authentication), offline-capable, data privacy focused
**Scale/Scope**: Single family use, up to 10 accounts, thousands of transactions per year, basic/complex budget types

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Hexagonal Architecture (NON-NEGOTIABLE)**:

- Domain Layer: Pure business logic entities with no external dependencies? YES
- Application Layer: MediatR command/query handlers orchestrating domain logic? YES
- Infrastructure Layer: EF Core, GraphQL adapters implementing domain interfaces? YES
- Presentation Layer: Thin GraphQL resolvers delegating to application layer? YES
- Dependency direction flows inward (Presentation → Application → Domain)? YES
- Infrastructure implements domain interfaces via dependency inversion? YES

**Simplicity**:

- Projects: 1 (GraphQL API server with hexagonal layers)
- Using framework directly? YES - EF Core direct, no Repository wrapper
- Single data model? YES - domain entities used for GraphQL schema
- Avoiding unnecessary patterns? YES - but hexagonal layers are mandatory

**Architecture**:

- EVERY feature as library with hexagonal boundaries? YES
- Libraries listed: [BasicBudget.Domain, BasicBudget.Application, BasicBudget.Infrastructure, BasicBudget.GraphQL]
- CLI per library: Domain/Application expose CLI for testing, Infrastructure for migrations
- Library docs: llms.txt format planned? YES

**Testing (NON-NEGOTIABLE)**:

- RED-GREEN-Refactor cycle enforced? YES - test MUST fail first
- Git commits show tests before implementation? YES
- Order: Contract→Integration→Unit strictly followed? YES
- Real dependencies used? YES - actual PostgreSQL, not mocks
- Integration tests for: GraphQL schema, EF Core models, cross-layer integration? YES
- TUnit framework used for all tests? YES
- FORBIDDEN: Implementation before test, skipping RED phase

**Observability**:

- Structured logging included? YES - Serilog across all hexagonal layers
- Distributed tracing? YES - OpenTelemetry integration with .NET Aspire
- Audit trail? YES - Financial operations logged to PostgreSQL sink
- Error context sufficient? YES - Domain errors with financial context propagated
- Performance monitoring? YES - Query execution times and business metrics

**Error Handling**:

- Domain errors use OneOf instead of exceptions? YES - All business rule violations return OneOf<Success, DomainError>
- Exceptions reserved for exceptional cases? YES - Infrastructure failures, system errors only
- GraphQL error unions implemented? YES - Payload types convert OneOf results to GraphQL errors
- MediatR handlers return explicit error states? YES - OneOf<TResult, DomainError> pattern throughout

**CI/CD & Quality Gates**:

- Automated testing pipeline? YES - GitHub Actions with PR validation and main branch CI
- TDD enforcement? YES - CI fails if implementation exists without corresponding tests
- Code coverage requirements? YES - Minimum 80% coverage across all hexagonal layers
- Automated version management? YES - MAJOR.MINOR.BUILD with auto-increment BUILD numbers
- Integration testing with real dependencies? YES - PostgreSQL and Redis services in CI pipeline

**Versioning**:

- Version number assigned? YES - 1.0.0 (MAJOR.MINOR.BUILD)
- BUILD increments on every change? YES
- Breaking changes handled? YES - database migrations, GraphQL schema evolution

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)

```
# Hexagonal Architecture Structure with .NET Aspire
src/
├── BasicBudget.Domain/           # Core business logic
│   ├── Entities/                 # Account, Transaction, Budget, Category
│   ├── ValueObjects/             # Money, AccountNumber, etc.
│   ├── Services/                 # Domain services
│   ├── Repositories/             # Repository interfaces (no implementation)
│   └── Events/                   # Domain events
├── BasicBudget.Application/      # Use cases and orchestration
│   ├── Commands/                 # MediatR command handlers
│   ├── Queries/                  # MediatR query handlers
│   ├── DTOs/                     # Data transfer objects
│   ├── Services/                 # Application services
│   └── Interfaces/               # Application service interfaces
├── BasicBudget.Infrastructure/   # External concerns
│   ├── Persistence/              # EF Core DbContext, repositories
│   ├── FileSystem/               # Statement file parsing
│   └── Configuration/            # DI container setup
├── BasicBudget.GraphQL/          # Presentation layer
│   ├── Resolvers/                # GraphQL resolvers
│   ├── Types/                    # GraphQL type definitions
│   ├── Queries/                  # Query definitions
│   └── Mutations/                # Mutation definitions
└── BasicBudget.AppHost/          # .NET Aspire orchestration
    ├── Program.cs                # Aspire app configuration
    └── appsettings.json          # Aspire configuration

tests/
├── BasicBudget.Domain.Tests/     # Unit tests for domain logic
├── BasicBudget.Application.Tests/ # Unit tests for application logic
├── BasicBudget.Infrastructure.Tests/ # Integration tests
├── BasicBudget.GraphQL.Tests/    # Contract tests for GraphQL
└── BasicBudget.IntegrationTests/ # End-to-end integration tests
```

**Structure Decision**: Hexagonal Architecture - mandatory layered structure with clear dependency boundaries

## Phase 0: Outline & Research

1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:

   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts

*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `/scripts/update-agent-context.sh [claude|gemini|copilot]` for your AI assistant
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach

*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:

- Load `/templates/tasks-template.md` as base
- Generate tasks following hexagonal architecture layers with explicit references:

**Domain Layer Tasks** (Reference: [data-model.md](./data-model.md)):

- Entities: Account, Transaction, Budget, Category (lines 5-138 in data-model.md)
- Value Objects: Money, AccountNumber (lines 152-175 in data-model.md)
- Domain Services: BudgetCalculationService, CategorizationService (lines 176-191 in data-model.md)
- Repository Interfaces: IAccountRepository, ITransactionRepository, etc. (lines 192-235 in data-model.md)
- Domain Events: Account creation, budget exceeded alerts

**Application Layer Tasks** (Reference: [research.md MediatR section](./research.md#mediatr-for-cqrs)):

- Command Handlers: CreateAccount, CreateTransaction, ImportStatement commands
- Query Handlers: GetAccount, GetTransactions, GetBudgetProgress queries
- DTOs: Command/Query input models, Application service interfaces
- Application Services: Orchestration between domain services

**Infrastructure Layer Tasks** (Reference: [research.md EF Core section](./research.md#entity-framework-core-with-postgresql)):

- EF Core DbContext: BasicBudgetDbContext with entity configurations
- Repository Implementations: Concrete implementations of domain repository interfaces
- File System Services: CSV/QFX/OFX statement parsers (FR-003, FR-004 from spec.md)
- Database Migrations: Schema creation following data-model.md specifications

**GraphQL Layer Tasks** (Reference: [contracts/graphql-schema.graphql](./contracts/graphql-schema.graphql)):

- Resolvers: Query, Mutation, and Subscription resolvers (lines 3-83 in schema)
- Types: GraphQL type definitions matching domain entities
- Schema Configuration: HotChocolate setup with filtering, sorting, pagination
- Error Handling: OneOf result conversion to GraphQL error payloads

**Testing Tasks** (Reference: [quickstart.md scenarios](./quickstart.md#quick-start-scenarios)):

- Contract Tests: Each GraphQL operation → contract test task [P]
- Integration Tests: Each quickstart scenario → integration test (10 scenarios total)
- TUnit Setup: Database parallel limiting, Aspire TestHost integration

**Ordering Strategy (Constitutional TDD Requirements)**:

- Phase A: Contract tests (all GraphQL operations) - MUST FAIL initially
- Phase B: Domain model implementation (entities, value objects)
- Phase C: Application layer (MediatR handlers)
- Phase D: Infrastructure layer (repositories, database)
- Phase E: GraphQL layer (resolvers, types)
- Phase F: Integration tests (quickstart scenarios)
- Mark [P] for parallel execution within same layer

**Hexagonal Architecture Task Dependencies**:

- Domain has no dependencies (can be built first)
- Application depends on Domain interfaces
- Infrastructure implements Domain interfaces
- GraphQL depends on Application layer

**Estimated Output**: 35-40 numbered, ordered tasks in tasks.md following constitutional TDD approach

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Implementation Readiness Assessment

**✅ Ready to Proceed**: All planning phases complete, ready for `/tasks` command
**📋 Next Immediate Actions**:

1. **Execute**: `cd /home/dwalleck/repos/basic-budget && /tasks` - Generate concrete implementation tasks
2. **Review**: Generated tasks.md for task dependencies and parallel execution opportunities
3. **Setup**: Initialize git repository with constitutional TDD workflow
4. **Begin**: Start with contract tests (Phase A) following generated task sequence

**📚 Key Implementation References**:

- **Domain Models**: [data-model.md](./data-model.md) lines 5-274 (entities, value objects, repositories)
- **GraphQL Schema**: [contracts/graphql-schema.graphql](./contracts/graphql-schema.graphql) (complete API contract)
- **Testing Examples**: [quickstart.md](./quickstart.md) lines 420-550 (TUnit integration tests)
- **Technology Integration**: [research.md](./research.md) (version requirements, patterns, configurations)
- **Error Handling**: [research.md OneOf section](./research.md#oneof-discriminated-unions-for-error-handling) lines 488-707
- **Logging Strategy**: [research.md Serilog section](./research.md#serilog-structured-logging) lines 383-481
- **CI/CD Pipeline**: [research.md GitHub Actions section](./research.md#github-actions-ci-cd-pipeline) lines 710-938

**🏗️ Expected Implementation Flow** (Post `/tasks` execution):

1. **Contract Tests First**: GraphQL schema validation tests (must fail initially per TDD)
2. **Domain Layer**: Pure business entities with OneOf error handling
3. **Application Layer**: MediatR command/query handlers with Serilog logging
4. **Infrastructure Layer**: EF Core repositories, file parsers, database context
5. **GraphQL Layer**: HotChocolate resolvers with subscription support
6. **Integration Tests**: Aspire TestHost scenarios from quickstart guide
7. **CI/CD Setup**: GitHub Actions workflows for automated quality gates

## Phase 3+: Future Implementation

*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)
**Phase 4**: Implementation (execute tasks.md following constitutional principles)
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

## Progress Tracking

*This checklist is updated during execution flow*

**Phase Status**:

- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:

- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented (none required)

---
*Based on Constitution v1.0.0 - See `/memory/constitution.md`*
