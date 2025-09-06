using OneOf;
using BasicBudget.Domain.Entities;

namespace BasicBudget.Domain.Errors;

// Base error types
public abstract record DomainError(string Message, string Code);

// Account-related errors
public record AccountNotFoundError(Guid AccountId) 
    : DomainError($"Account with ID {AccountId} was not found", "ACCOUNT_NOT_FOUND");

public record InvalidAccountNumberError(string AccountNumber) 
    : DomainError($"Invalid account number: {AccountNumber}", "INVALID_ACCOUNT_NUMBER");

public record DuplicateAccountNumberError(string AccountNumber) 
    : DomainError($"Account number {AccountNumber} already exists", "DUPLICATE_ACCOUNT_NUMBER");

public record InvalidAccountTypeError(string AccountType) 
    : DomainError($"Invalid account type: {AccountType}", "INVALID_ACCOUNT_TYPE");

public record NegativeBalanceNotAllowedError(string AccountType) 
    : DomainError($"Account type {AccountType} cannot have negative balance", "NEGATIVE_BALANCE_NOT_ALLOWED");

// Transaction-related errors
public record TransactionNotFoundError(Guid TransactionId) 
    : DomainError($"Transaction with ID {TransactionId} was not found", "TRANSACTION_NOT_FOUND");

public record InvalidTransactionAmountError(decimal Amount) 
    : DomainError($"Invalid transaction amount: {Amount}. Amount cannot be zero", "INVALID_TRANSACTION_AMOUNT");

public record FutureTransactionDateError(DateTime TransactionDate) 
    : DomainError($"Transaction date cannot be in the future: {TransactionDate:yyyy-MM-dd}", "FUTURE_TRANSACTION_DATE");

public record InvalidTransactionDescriptionError(string Description) 
    : DomainError($"Invalid transaction description: {Description}", "INVALID_TRANSACTION_DESCRIPTION");

// Category-related errors
public record CategoryNotFoundError(Guid CategoryId) 
    : DomainError($"Category with ID {CategoryId} was not found", "CATEGORY_NOT_FOUND");

public record DuplicateCategoryNameError(string CategoryName, Guid? ParentId) 
    : DomainError($"Category name '{CategoryName}' already exists in the same parent level", "DUPLICATE_CATEGORY_NAME");

public record CategoryHierarchyTooDeepError(int MaxDepth) 
    : DomainError($"Category hierarchy cannot exceed {MaxDepth} levels", "CATEGORY_HIERARCHY_TOO_DEEP");

public record CircularCategoryReferenceError(Guid CategoryId) 
    : DomainError($"Cannot create circular reference for category {CategoryId}", "CIRCULAR_CATEGORY_REFERENCE");

public record SystemCategoryCannotBeDeletedError(Guid CategoryId) 
    : DomainError($"System-generated category {CategoryId} cannot be deleted", "SYSTEM_CATEGORY_CANNOT_BE_DELETED");

public record InvalidColorCodeError(string ColorCode) 
    : DomainError($"Invalid color code: {ColorCode}. Must be a valid 6-digit hex code", "INVALID_COLOR_CODE");

// Budget-related errors
public record BudgetNotFoundError(Guid BudgetId) 
    : DomainError($"Budget with ID {BudgetId} was not found", "BUDGET_NOT_FOUND");

public record BudgetPeriodOverlapError(DateTime StartDate, DateTime EndDate) 
    : DomainError($"Budget period {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} overlaps with existing budget", "BUDGET_PERIOD_OVERLAP");

public record InvalidBudgetPeriodError(DateTime StartDate, DateTime EndDate) 
    : DomainError($"Invalid budget period: End date {EndDate:yyyy-MM-dd} must be after start date {StartDate:yyyy-MM-dd}", "INVALID_BUDGET_PERIOD");

public record BudgetNotActiveError(Guid BudgetId) 
    : DomainError($"Budget {BudgetId} is not active", "BUDGET_NOT_ACTIVE");

// Budget Category-related errors
public record BudgetCategoryNotFoundError(Guid BudgetCategoryId) 
    : DomainError($"Budget category with ID {BudgetCategoryId} was not found", "BUDGET_CATEGORY_NOT_FOUND");

public record DuplicateBudgetCategoryError(Guid BudgetId, Guid CategoryId) 
    : DomainError($"Category {CategoryId} is already included in budget {BudgetId}", "DUPLICATE_BUDGET_CATEGORY");

public record InvalidAlertThresholdError(decimal Threshold) 
    : DomainError($"Invalid alert threshold: {Threshold}. Must be between 0 and 1", "INVALID_ALERT_THRESHOLD");

public record InsufficientBudgetAllocationError(Guid CategoryId, decimal Requested, decimal Available) 
    : DomainError($"Insufficient budget allocation for category {CategoryId}. Requested: {Requested}, Available: {Available}", "INSUFFICIENT_BUDGET_ALLOCATION");

// Currency-related errors
public record CurrencyMismatchError(string Currency1, string Currency2) 
    : DomainError($"Currency mismatch: Cannot perform operation between {Currency1} and {Currency2}", "CURRENCY_MISMATCH");

public record UnsupportedCurrencyError(string Currency) 
    : DomainError($"Unsupported currency: {Currency}", "UNSUPPORTED_CURRENCY");

// Import-related errors
public record StatementImportError(string FileName, string Reason) 
    : DomainError($"Failed to import statement file '{FileName}': {Reason}", "STATEMENT_IMPORT_ERROR");

public record InvalidFileFormatError(string FileName, string ExpectedFormat) 
    : DomainError($"Invalid file format for '{FileName}'. Expected: {ExpectedFormat}", "INVALID_FILE_FORMAT");

public record DuplicateTransactionError(string TransactionIdentifier) 
    : DomainError($"Duplicate transaction detected: {TransactionIdentifier}", "DUPLICATE_TRANSACTION");

// Permission-related errors
public record InsufficientPermissionsError(string Operation) 
    : DomainError($"Insufficient permissions to perform operation: {Operation}", "INSUFFICIENT_PERMISSIONS");

public record ConcurrencyConflictError(string EntityType, Guid EntityId) 
    : DomainError($"Concurrency conflict detected for {EntityType} with ID {EntityId}", "CONCURRENCY_CONFLICT");

// Simple success record for void operations
public record Success;