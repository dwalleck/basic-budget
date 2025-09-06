# Basic Budget Constitution

## Core Principles

### I. Hexagonal Architecture (NON-NEGOTIABLE)
All applications MUST use hexagonal architecture with proper layered abstraction:
- **Domain Layer**: Core business logic, entities, domain services - no dependencies on external concerns
- **Application Layer**: Use cases, command/query handlers via MediatR - orchestrates domain logic  
- **Infrastructure Layer**: External concerns (databases, file systems, GraphQL) - implements domain interfaces
- **Presentation Layer**: GraphQL resolvers, API controllers - thin layer delegating to application layer

Dependency direction MUST always flow inward: Presentation → Application → Domain
Infrastructure implements domain interfaces via dependency inversion

### II. Library-First Development
Every feature starts as a standalone library with clear boundaries:
- Libraries must be self-contained and independently testable
- Each library exposes functionality via CLI when applicable
- Clear single purpose required - no organizational-only libraries

### III. Test-First Development (NON-NEGOTIABLE)
TDD mandatory following strict Red-Green-Refactor cycle:
- Tests written first → User approved → Tests fail → Then implement
- Red-Green-Refactor cycle strictly enforced
- Integration tests required for: new libraries, contract changes, inter-service communication, shared schemas
- Real dependencies used in tests (actual databases, not mocks)

### IV. GraphQL API Design
GraphQL APIs must provide comprehensive querying capabilities:
- Support filtering, sorting, and pagination for all list queries
- Provide time-period based queries for temporal data
- Include periodic summary queries (weekly, monthly, quarterly)
- Follow GraphQL best practices for schema design

### V. Simplicity and Observability
- Start simple, apply YAGNI principles
- Structured logging required for all operations  
- Text I/O ensures debuggability
- Error context must be sufficient for troubleshooting

## Architecture Constraints

### Hexagonal Architecture Enforcement
- Domain models contain business logic only
- Application layer coordinates via command/query pattern
- Infrastructure adapters implement domain interfaces
- No direct dependencies on external frameworks in domain/application layers
- MediatR handles cross-cutting concerns and request/response flow

### Data Access Patterns
- Entity Framework Core used directly (no Repository/UoW wrapper unless proven need)
- Domain entities used for GraphQL schema (no DTOs unless serialization differs)
- Database migrations managed through EF Core tooling

### Testing Requirements
- TUnit framework for all test projects
- Contract tests for all GraphQL operations
- Integration tests using real PostgreSQL database
- Test organization: Contract → Integration → Unit

## Development Workflow

### Quality Gates
- All code must pass hexagonal architecture review
- GraphQL schema changes require contract test updates
- Database schema changes require migration and rollback testing
- No implementation without failing tests first
- All services must be orchestrated through .NET Aspire AppHost
- Integration tests must use Aspire TestHost with real dependencies

### Technology Stack Standards
- .NET 10 for all projects
- .NET Aspire 9.4+ for local development orchestration
- HotChocolate for GraphQL implementation
- Entity Framework Core for data access
- PostgreSQL for data persistence (containerized via Aspire)
- MediatR for CQRS pattern implementation
- TUnit for testing framework with Aspire TestHost integration

## Governance

Constitution supersedes all other development practices. Hexagonal architecture compliance is mandatory and non-negotiable. All code reviews must verify proper layer separation and dependency direction. Complexity must be justified against constitutional principles.

**Version**: 1.0.0 | **Ratified**: 2025-09-06 | **Last Amended**: 2025-09-06