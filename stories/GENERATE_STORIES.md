# Story Generation Guide

## Stories Created
The following example stories have been created to establish the pattern:
- `STORY_TEMPLATE.md` - Template for all stories
- `INDEX.md` - Master tracking document
- `queries/QUERY-001-list-all-accounts.md` - Example query story
- `mutations/MUT-003-create-transaction.md` - Example mutation story
- `types/TYPE-001-pagination-types.md` - Example type story
- `types/FOUND-001-configure-scalars.md` - Foundation story
- `types/FOUND-002-error-handling.md` - Foundation story

## Remaining Stories to Generate

### Query Stories (queries/)
- QUERY-002: Get Account by ID
- QUERY-003: Get Account by Number
- QUERY-004: List Transactions with Pagination
- QUERY-005: Get Single Transaction
- QUERY-006: Transaction Filtering
- QUERY-007: Transaction Sorting
- QUERY-008: List All Budgets
- QUERY-009: Get Single Budget
- QUERY-010: Get Active Budgets
- QUERY-011: Get Budget Progress
- QUERY-012: List All Categories
- QUERY-013: Get Single Category
- QUERY-014: Get Category Hierarchy
- QUERY-015: Periodic Summary
- QUERY-016: Monthly Spending
- QUERY-017: Budget Progress Summary

### Mutation Stories (mutations/)
- MUT-002: Update Account
- MUT-004: Update Transaction
- MUT-005: Categorize Transaction
- MUT-006: Create Budget
- MUT-007: Update Budget
- MUT-008: Add Budget Category
- MUT-009: Create Category
- MUT-010: Update Category
- MUT-011: Import Statement

### Type Stories (types/)
- TYPE-002: Filter Input Types
- TYPE-003: Sort Input Types
- TYPE-004: Money Input Type
- TYPE-005: Error Type
- TYPE-006: Summary Types
- TYPE-007: Alert and Warning Types
- FOUND-003: Configure MediatR Pipeline

### Subscription Stories (subscriptions/)
- SUB-001: Transaction Added
- SUB-002: Budget Alert Added
- SUB-003: Spending Limit Warning
- SUB-004: Budget Progress Changed

### Infrastructure Stories (infrastructure/)
- INFRA-001: Setup Subscription Transport
- INFRA-002: Configure File Upload
- INFRA-003: Implement Caching Layer
- INFRA-004: Setup Background Jobs

## Story Structure
Each story should follow the template and include:

1. **Status Tracking** - Checkboxes for progress
2. **Overview** - Brief description
3. **Acceptance Criteria** - Measurable outcomes
4. **Technical Context** - Architecture requirements and file locations
5. **Implementation Steps** - Detailed steps with code examples
6. **Verification** - Testing commands and examples
7. **Dependencies** - What blocks/is blocked by this story
8. **Definition of Done** - Final checklist

## Key Requirements for All Stories

### Must Include
- Branch creation command with story ID
- Hexagonal architecture enforcement notes
- OneOf<TSuccess, DomainError> pattern for error handling
- Build verification step
- Pull request creation step

### Architecture Reminders
```
Domain Layer → No external dependencies
Application Layer → MediatR handlers with OneOf returns
Infrastructure Layer → Implements domain interfaces
GraphQL Layer → Thin resolvers delegating to application
```

### Standard PR Process
```bash
git checkout -b story/[STORY_ID]-[description]
# ... implement ...
dotnet build
dotnet test
git add .
git commit -m "feat: [STORY_ID] - [Description]"
git push origin story/[STORY_ID]-[description]
gh pr create --title "[STORY_ID] - [Title]" --body "[Description]"
```

## Generation Script Pattern

For each remaining story:
1. Copy the appropriate template (query/mutation/type/subscription)
2. Update the story ID and title
3. Reference the GraphQL schema section
4. Define specific implementation steps
5. List actual file paths to be modified
6. Include example test queries/mutations
7. Specify dependencies based on INDEX.md

## Priority Order
Generate stories in this order:
1. Foundation stories (FOUND-*)
2. Type definition stories (TYPE-*)
3. Basic CRUD queries (QUERY-001 through QUERY-014)
4. Basic CRUD mutations (MUT-*)
5. Advanced queries (QUERY-015 through QUERY-017)
6. Infrastructure stories (INFRA-*)
7. Subscription stories (SUB-*)

## Notes
- Each story should be completely self-contained
- Include enough context that someone unfamiliar with the project can implement it
- Reference specific lines in the GraphQL schema
- Include example requests/responses for testing
- Keep stories atomic - one feature per story