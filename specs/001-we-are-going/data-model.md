# Data Model: Basic Budget

## Domain Entities
*Implementation Reference: These entities will be implemented in `src/BasicBudget.Domain/Entities/` following the hexagonal architecture structure defined in [plan.md](./plan.md#project-structure)*

### Account
**Purpose**: Represents financial accounts (checking, savings, credit cards)
**Attributes**:
- Id: Unique identifier (Guid)
- AccountNumber: Unique account number (Value Object)
- Name: User-friendly account name
- AccountType: Enum (Checking, Savings, CreditCard)
- CurrentBalance: Money value object
- CreatedAt: DateTime
- UpdatedAt: DateTime

**Business Rules**:
- Account numbers must be unique within the system
- Current balance is calculated from all transactions
- Credit card balances can be negative (debt)

**Relationships**:
- One-to-many with Transactions
- Many-to-many with Budgets (account can be included in multiple budgets)

### Transaction
**Purpose**: Individual financial transactions
**Attributes**:
- Id: Unique identifier (Guid)
- AccountId: Foreign key to Account
- Amount: Money value object (positive for income, negative for expenses)
- TransactionDate: DateTime when transaction occurred
- Description: Transaction description from bank
- Category: Reference to Category (nullable until categorized)
- IsReconciled: Boolean flag for statement reconciliation
- ImportedAt: DateTime when transaction was imported (nullable for manual entries)

**Business Rules**:
- Amount cannot be zero
- Transaction date cannot be in future
- Description is required
- Transactions can be manually created or imported from statements

**Relationships**:
- Many-to-one with Account
- Many-to-one with Category (optional)

### Budget
**Purpose**: Financial planning tool with spending categories and limits
**Attributes**:
- Id: Unique identifier (Guid)
- Name: User-friendly budget name
- BudgetType: Enum (Basic, Complex)
- StartDate: Budget period start
- EndDate: Budget period end (nullable for ongoing budgets)
- IsActive: Boolean flag
- CreatedAt: DateTime
- UpdatedAt: DateTime

**Business Rules**:
- Budget periods cannot overlap for same account
- Complex budgets support sub-categories and advanced rules
- Basic budgets have simple category limits

**Relationships**:
- One-to-many with BudgetCategories
- Many-to-many with Accounts (budget can include multiple accounts)

### Category
**Purpose**: Transaction classification system
**Attributes**:
- Id: Unique identifier (Guid)
- Name: Category name (e.g., "Groceries", "Utilities")
- ParentCategoryId: For hierarchical categories (nullable)
- Description: Optional category description
- IsSystemGenerated: Boolean (true for auto-created categories)
- Color: Hex color for UI display

**Business Rules**:
- Category names must be unique within parent level
- Maximum 3 levels of hierarchy (Category -> Subcategory -> Sub-subcategory)
- System-generated categories cannot be deleted

**Relationships**:
- Self-referential for hierarchical structure
- One-to-many with Transactions
- One-to-many with BudgetCategories

### BudgetCategory
**Purpose**: Links budgets with categories and spending limits
**Attributes**:
- Id: Unique identifier (Guid)
- BudgetId: Foreign key to Budget
- CategoryId: Foreign key to Category
- AllocatedAmount: Money value object (spending limit)
- SpentAmount: Money value object (calculated from transactions)
- AlertThreshold: Decimal percentage (0.0-1.0) for spending alerts

**Business Rules**:
- Allocated amount must be positive
- Spent amount is calculated from transactions in budget period
- Alert threshold between 0 and 1 (0% to 100%)

**Relationships**:
- Many-to-one with Budget
- Many-to-one with Category

### BudgetAlert (Real-time Subscription Entity)
**Purpose**: Real-time notifications for budget limit violations
**Attributes**:
- Id: Unique identifier (Guid)
- BudgetId: Foreign key to Budget
- CategoryId: Foreign key to Category (nullable)
- AlertType: Enum (BudgetExceeded, ApproachingLimit, CategoryOverspent, MonthlyLimitWarning)
- Message: User-friendly alert message
- CurrentAmount: Money value object (current spending)
- LimitAmount: Money value object (budget limit)
- PercentageUsed: Decimal percentage (current/limit * 100)
- Timestamp: DateTime when alert was generated
- IsRead: Boolean flag for user acknowledgment

**Business Rules**:
- Alerts are generated automatically when thresholds are exceeded
- Multiple alerts can exist for same budget/category combination
- Alerts expire after budget period ends

**Relationships**:
- Many-to-one with Budget
- Many-to-one with Category (optional)

### SpendingLimitWarning (Real-time Subscription Entity)
**Purpose**: Proactive warnings before budget limits are exceeded  
**Attributes**:
- Id: Unique identifier (Guid)
- CategoryId: Foreign key to Category
- WarningType: Enum (SeventyFivePercent, NinetyPercent, BudgetExceeded, ProjectedOverage)
- CurrentSpending: Money value object
- BudgetLimit: Money value object
- PercentageUsed: Decimal percentage
- ProjectedOverage: Money value object (nullable - for projection warnings)
- DaysRemaining: Integer (days left in budget period)
- Timestamp: DateTime when warning was generated
- IsAcknowledged: Boolean flag for user acknowledgment

**Business Rules**:
- Warnings generated at 75%, 90%, and 100% of budget limits
- Projected overage calculated based on spending trend
- Only one active warning per type per category per period

**Relationships**:
- Many-to-one with Category

## Value Objects
*Implementation Reference: These value objects will be implemented in `src/BasicBudget.Domain/ValueObjects/` with immutable record types and validation logic as specified in [research.md OneOf section](./research.md#oneof-discriminated-unions-for-error-handling)*

### Money
**Purpose**: Represents monetary amounts with currency
**Attributes**:
- Amount: Decimal with 4 decimal places precision
- Currency: ISO currency code (default: USD)

**Business Rules**:
- Currency code must be valid ISO 4217
- Amount precision limited to 4 decimal places
- Immutable value object

### AccountNumber
**Purpose**: Represents account numbers with validation
**Attributes**:
- Value: String representation of account number
- MaskedValue: String with masked digits for display

**Business Rules**:
- Account numbers must be 8-20 characters
- Only alphanumeric characters allowed
- Immutable value object

## Domain Services

### BudgetCalculationService
**Purpose**: Calculate budget performance and spending analytics
**Methods**:
- CalculateSpentAmount(budgetId, categoryId, period)
- CalculateBudgetProgress(budgetId)
- GeneratePeriodicSummary(accountIds, period, summaryType)

### CategorizationService
**Purpose**: Automatically categorize transactions based on rules
**Methods**:
- CategorizeTransaction(transaction)
- LearnFromUserCorrection(transactionId, newCategoryId)
- GetCategorizationConfidence(transaction)

## Repository Interfaces (Domain Layer)
*Implementation Reference: These interfaces will be defined in `src/BasicBudget.Domain/Repositories/` and implemented in `src/BasicBudget.Infrastructure/Persistence/` using Entity Framework Core as detailed in [research.md EF Core section](./research.md#entity-framework-core-with-postgresql)*

### IAccountRepository
- GetByIdAsync(id): Account
- GetAllAsync(): List<Account>
- GetByAccountNumberAsync(accountNumber): Account
- AddAsync(account): void
- UpdateAsync(account): void

### ITransactionRepository
- GetByIdAsync(id): Transaction
- GetByAccountAsync(accountId, period): List<Transaction>
- GetUncategorizedAsync(): List<Transaction>
- AddAsync(transaction): void
- UpdateAsync(transaction): void
- AddRangeAsync(transactions): void

### IBudgetRepository
- GetByIdAsync(id): Budget
- GetActiveAsync(): List<Budget>
- GetByPeriodAsync(startDate, endDate): List<Budget>
- AddAsync(budget): void
- UpdateAsync(budget): void

### ICategoryRepository
- GetByIdAsync(id): Category
- GetAllAsync(): List<Category>
- GetHierarchyAsync(): List<Category>
- AddAsync(category): void
- UpdateAsync(category): void

### IBudgetAlertRepository  
- GetByIdAsync(id): BudgetAlert
- GetActiveAlertsAsync(): List<BudgetAlert>
- GetAlertsByBudgetAsync(budgetId): List<BudgetAlert>
- AddAsync(alert): void
- MarkAsReadAsync(alertId): void

### ISpendingLimitWarningRepository
- GetByIdAsync(id): SpendingLimitWarning
- GetActiveWarningsAsync(): List<SpendingLimitWarning>
- GetWarningsByCategoryAsync(categoryId): List<SpendingLimitWarning>
- AddAsync(warning): void
- AcknowledgeWarningAsync(warningId): void

## State Transitions

### Transaction States
1. **Imported**: Transaction imported from statement file
2. **Categorized**: Category assigned (manually or automatically)  
3. **Reconciled**: Confirmed against bank statement
4. **Disputed**: Marked for investigation

### Budget States
1. **Draft**: Budget created but not active
2. **Active**: Currently tracking spending
3. **Completed**: Budget period ended
4. **Archived**: Historical budget for reference

## Data Validation Rules

### Account Validation
- Name: Required, 1-100 characters
- AccountNumber: Required, unique, 8-20 alphanumeric characters
- AccountType: Required, valid enum value
- CurrentBalance: Required, valid Money value

### Transaction Validation
- Amount: Required, non-zero Money value
- TransactionDate: Required, not future date
- Description: Required, 1-500 characters
- AccountId: Required, valid Account reference

### Budget Validation
- Name: Required, 1-100 characters
- StartDate: Required, valid date
- EndDate: Optional, must be after StartDate if provided
- BudgetType: Required, valid enum value

### Category Validation
- Name: Required, unique within parent level, 1-50 characters
- ParentCategoryId: Optional, valid Category reference
- Color: Optional, valid hex color code