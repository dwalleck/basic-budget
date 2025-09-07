# BasicBudget Story Index

## Overview
This index tracks all implementation stories for the BasicBudget GraphQL API. Each story is atomic and can be implemented independently where dependencies allow.

## Story Categories

### Foundation Stories (Complete First)
- [x] FOUND-001: Configure GraphQL Scalar Types
- [ ] FOUND-002: Setup Error Handling Infrastructure
- [ ] FOUND-003: Configure MediatR Pipeline

### Type Definition Stories
- [ ] TYPE-001: PageInfo and Connection Types
- [ ] TYPE-002: Filter Input Types  
- [ ] TYPE-003: Sort Input Types
- [ ] TYPE-004: Money Input Type
- [ ] TYPE-005: Error Type
- [ ] TYPE-006: Summary Types
- [ ] TYPE-007: Alert and Warning Types

### Query Stories

#### Account Queries
- [ ] QUERY-001: List All Accounts
- [ ] QUERY-002: Get Account by ID
- [ ] QUERY-003: Get Account by Number

#### Transaction Queries  
- [ ] QUERY-004: List Transactions with Pagination
- [ ] QUERY-005: Get Single Transaction
- [ ] QUERY-006: Transaction Filtering
- [ ] QUERY-007: Transaction Sorting

#### Budget Queries
- [ ] QUERY-008: List All Budgets
- [ ] QUERY-009: Get Single Budget
- [ ] QUERY-010: Get Active Budgets
- [ ] QUERY-011: Get Budget Progress

#### Category Queries
- [ ] QUERY-012: List All Categories
- [ ] QUERY-013: Get Single Category  
- [ ] QUERY-014: Get Category Hierarchy

#### Summary Queries
- [ ] QUERY-015: Periodic Summary
- [ ] QUERY-016: Monthly Spending
- [ ] QUERY-017: Budget Progress Summary

### Mutation Stories

#### Account Mutations
- [x] MUT-001: Create Account (IMPLEMENTED)
- [ ] MUT-002: Update Account

#### Transaction Mutations
- [ ] MUT-003: Create Transaction
- [ ] MUT-004: Update Transaction
- [ ] MUT-005: Categorize Transaction

#### Budget Mutations
- [ ] MUT-006: Create Budget
- [ ] MUT-007: Update Budget
- [ ] MUT-008: Add Budget Category

#### Category Mutations
- [ ] MUT-009: Create Category
- [ ] MUT-010: Update Category

#### Import Mutations
- [ ] MUT-011: Import Statement

### Subscription Stories
- [ ] SUB-001: Transaction Added
- [ ] SUB-002: Budget Alert Added
- [ ] SUB-003: Spending Limit Warning
- [ ] SUB-004: Budget Progress Changed

### Infrastructure Stories
- [ ] INFRA-001: Setup Subscription Transport
- [ ] INFRA-002: Configure File Upload
- [ ] INFRA-003: Implement Caching Layer
- [ ] INFRA-004: Setup Background Jobs

## Story Status Legend
- [ ] Not Started
- [x] Completed
- 🚧 In Progress
- ⚠️ Blocked
- 🔄 In Review

## Dependency Graph

### Must Complete First (Foundation)
```
FOUND-001 → FOUND-002 → FOUND-003
    ↓
TYPE-001, TYPE-004, TYPE-005
```

### Query Dependencies
```
TYPE-001 → QUERY-004 (Pagination)
TYPE-002 → QUERY-006 (Filtering)
TYPE-003 → QUERY-007 (Sorting)
```

### Mutation Dependencies
```
TYPE-004 → All Mutations (Money Input)
TYPE-005 → All Mutations (Error Handling)
```

### Subscription Dependencies
```
INFRA-001 → All Subscriptions
TYPE-007 → SUB-002, SUB-003
```

## Implementation Order (Recommended)

### Phase 1: Foundation (Week 1, Days 1-2)
1. FOUND-001: Configure Scalars
2. FOUND-002: Error Handling
3. FOUND-003: MediatR Pipeline
4. TYPE-004: Money Input
5. TYPE-005: Error Type

### Phase 2: Basic CRUD (Week 1, Days 3-5)
1. QUERY-001: List Accounts
2. QUERY-002: Get Account
3. MUT-002: Update Account
4. QUERY-012: List Categories
5. MUT-009: Create Category

### Phase 3: Transactions (Week 2, Days 1-3)
1. TYPE-001: Pagination Types
2. QUERY-004: List Transactions
3. MUT-003: Create Transaction
4. MUT-004: Update Transaction
5. MUT-005: Categorize Transaction

### Phase 4: Budgets (Week 2, Days 4-5)
1. QUERY-008: List Budgets
2. MUT-006: Create Budget
3. MUT-007: Update Budget
4. MUT-008: Add Budget Category

### Phase 5: Advanced Queries (Week 3, Days 1-3)
1. TYPE-002: Filter Types
2. TYPE-003: Sort Types
3. QUERY-006: Transaction Filtering
4. QUERY-007: Transaction Sorting
5. TYPE-006: Summary Types

### Phase 6: Analytics (Week 3, Days 4-5)
1. QUERY-015: Periodic Summary
2. QUERY-016: Monthly Spending
3. QUERY-017: Budget Progress

### Phase 7: Real-time (Week 4)
1. INFRA-001: Subscription Transport
2. SUB-001: Transaction Added
3. TYPE-007: Alert Types
4. SUB-002: Budget Alerts
5. SUB-003: Spending Warnings

## Tracking Metrics
- **Total Stories**: 45
- **Completed**: 1
- **In Progress**: 0
- **Blocked**: 0
- **Not Started**: 44

## Last Updated
2025-01-06