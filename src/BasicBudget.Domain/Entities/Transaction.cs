using BasicBudget.Domain.Errors;
using BasicBudget.Domain.ValueObjects;

using OneOf;

namespace BasicBudget.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool IsReconciled { get; private set; }
    public DateTime? ImportedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Account Account { get; private set; } = null!;
    public Category? Category { get; private set; }

    private Transaction() { } // EF Core constructor

    public Transaction(
        Guid accountId,
        Money amount,
        DateTime transactionDate,
        string description,
        Guid? categoryId = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        TransactionDate = transactionDate;
        Description = description?.Trim() ?? throw new ArgumentNullException(nameof(description));
        CategoryId = categoryId;
        IsReconciled = false;
        ImportedAt = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ValidateBusinessRules();
    }

    // Factory method for creating transactions  
    public static OneOf<Transaction, DomainError> Create(
        Account account,
        Money amount,
        string description,
        DateTime transactionDate,
        Guid? categoryId = null)
    {
        if (account == null)
            return new AccountNotFoundError(Guid.Empty);

        if (amount == null)
            return new InvalidTransactionAmountError(0);

        if (string.IsNullOrWhiteSpace(description))
            return new InvalidTransactionDescriptionError(description ?? "");

        if (transactionDate > DateTime.UtcNow)
            return new FutureTransactionDateError(transactionDate);

        try
        {
            return new Transaction(account.Id, amount, transactionDate, description, categoryId);
        }
        catch (ArgumentException ex)
        {
            return new InvalidTransactionAmountError(amount?.Amount ?? 0);
        }
    }

    // Constructor for imported transactions
    public static Transaction CreateImported(
        Guid accountId,
        Money amount,
        DateTime transactionDate,
        string description,
        Guid? categoryId = null)
    {
        var transaction = new Transaction(accountId, amount, transactionDate, description, categoryId);
        transaction.ImportedAt = DateTime.UtcNow;
        return transaction;
    }

    public void UpdateAmount(Money newAmount)
    {
        if (newAmount == null)
            throw new ArgumentNullException(nameof(newAmount));

        if (newAmount.IsZero)
            throw new ArgumentException("Transaction amount cannot be zero");

        Amount = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Transaction description cannot be empty", nameof(newDescription));

        if (newDescription.Length > 500)
            throw new ArgumentException("Transaction description cannot exceed 500 characters");

        Description = newDescription.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTransactionDate(DateTime newDate)
    {
        if (newDate > DateTime.UtcNow)
            throw new ArgumentException("Transaction date cannot be in the future");

        TransactionDate = newDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Categorize(Guid categoryId)
    {
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCategory()
    {
        CategoryId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsReconciled()
    {
        IsReconciled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsUnreconciled()
    {
        IsReconciled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsIncome => Amount.IsPositive;
    public bool IsExpense => Amount.IsNegative;
    public bool IsCategorized => CategoryId.HasValue;
    public bool IsImported => ImportedAt.HasValue;

    public Money GetAbsoluteAmount()
    {
        if (Amount.IsNegative)
        {
            var result = Money.Create(-Amount.Amount, Amount.Currency);
            return result.Match(
                money => money,
                error => throw new InvalidOperationException($"Failed to create absolute amount: {error.Message}")
            );
        }
        return Amount;
    }

    private void ValidateBusinessRules()
    {
        if (AccountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty");

        if (Amount.IsZero)
            throw new ArgumentException("Transaction amount cannot be zero");

        if (TransactionDate > DateTime.UtcNow)
            throw new ArgumentException("Transaction date cannot be in the future");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Transaction description is required");

        if (Description.Length > 500)
            throw new ArgumentException("Transaction description cannot exceed 500 characters");
    }
}