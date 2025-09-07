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

### 6. Verify & Test
```bash
# Build entire solution
dotnet build

# Run existing tests
dotnet test

# Manual testing via GraphQL playground
dotnet run --project src/BasicBudget.GraphQL
```

### 7. Create Pull Request
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
- [ ] Solution builds without errors
- [ ] Existing tests still pass
- [ ] Pull request opened with clear description
- [ ] Code review approved
- [ ] Merged to main branch