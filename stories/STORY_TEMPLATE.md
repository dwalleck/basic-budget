# Story: [STORY_ID] - [TITLE]

## Status

- [ ] Not Started
- [ ] In Progress
- [ ] Code Complete
- [ ] PR Opened
- [ ] Merged

## Overview

[Brief description of what needs to be implemented]

## Acceptance Criteria

- [ ] [Specific measurable outcome 1]
- [ ] [Specific measurable outcome 2]
- [ ] [All code builds successfully]
- [ ] [Pull request opened when complete]

## Technical Context

### Architecture Requirements

- **Hexagonal Architecture**: Strictly enforce layer separation
  - Domain Layer: Business logic only, no external dependencies
  - Application Layer: Use cases via MediatR command/query handlers
  - Infrastructure Layer: External concerns, implements domain interfaces
  - GraphQL Layer: Thin resolvers delegating to application layer
- **Error Handling**: Use OneOf<TSuccess, DomainError> pattern for all operations
- **Dependency Direction**: Must flow inward (Presentation → Application → Domain)

### Implementation Location

- **Domain**: `src/BasicBudget.Domain/`
- **Application**: `src/BasicBudget.Application/`
- **Infrastructure**: `src/BasicBudget.Infrastructure/`
- **GraphQL**: `src/BasicBudget.GraphQL/`

### Related Files

- [List specific files that need to be modified or referenced]

### Schema Reference

```graphql
# Paste relevant GraphQL schema section here
```

## Implementation Steps

### 1. Create Feature Branch

```bash
git checkout -b story/[STORY_ID]-[brief-description]
```

### 2. Domain Layer (if needed)

- [ ] Create/modify domain entities
- [ ] Define value objects
- [ ] Add domain-specific errors to `DomainErrors.cs`
- [ ] Define repository interfaces

### 3. Application Layer

- [ ] Create command/query record
- [ ] Implement MediatR handler
- [ ] Use OneOf<TSuccess, DomainError> for return type
- [ ] Inject required repositories via constructor

### 4. Infrastructure Layer (if needed)

- [ ] Implement repository interfaces
- [ ] Add EF Core configurations
- [ ] Create database migrations if schema changes

### 5. GraphQL Layer

- [ ] Define input types (if mutation)
- [ ] Define payload types (if mutation)
- [ ] Add resolver method to Query/Mutation/Subscription class
- [ ] Map between GraphQL types and domain models
- [ ] Handle errors appropriately

### 6. Write Automated Tests

- [ ] Unit tests for domain logic (if applicable)
- [ ] Unit tests for application handlers
- [ ] Integration tests for repository implementations (if applicable)
- [ ] GraphQL integration tests for new queries/mutations
- [ ] Test both success and error scenarios
- [ ] Ensure all edge cases are covered

Example test structure (using TUnit):

```csharp
// Unit test for handler
[Test]
public async Task Handle_ValidRequest_ReturnsSuccess()
{
    // Arrange
    // Act
    // Assert
}

// GraphQL integration test
[Test]
public async Task Query_ReturnsExpectedData()
{
    // Use test server/client
    // Execute GraphQL query
    // Assert response
}
```

### 7. Verify & Test

```bash
# Build entire solution
dotnet build

# Run all tests (including new ones)
dotnet test

# Run tests with coverage (outputs to console)
dotnet test --coverage --coverage-output-format cobertura

# Run tests with coverage and save report
dotnet test --coverage --coverage-output-format cobertura --coverage-output coverage.xml

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

### 8. Create Pull Request

```bash
git add .
git commit -m "feat: [STORY_ID] - [Brief description of what was implemented]"
git push origin story/[STORY_ID]-[brief-description]
# Open PR via GitHub CLI or web interface
gh pr create --title "[STORY_ID] - [Title]" --body "Implements [description]"
```

## Dependencies

- **Blocked By**: [List any stories that must be completed first]
- **Blocks**: [List any stories that depend on this one]

## Notes

[Any additional context, gotchas, or important information]

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Code follows hexagonal architecture
- [ ] Errors handled with OneOf pattern
- [ ] **Unit tests written for new domain logic**
- [ ] **Unit tests written for application handlers**
- [ ] **Integration tests written for GraphQL endpoints**
- [ ] **All tests pass (new and existing)**
- [ ] **Code coverage maintained or improved**
- [ ] Solution builds without errors
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch
