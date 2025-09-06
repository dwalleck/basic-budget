using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Errors;
using OneOf;

namespace BasicBudget.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public AccountNumber AccountNumber { get; private set; }
    public string Name { get; private set; }
    public AccountType AccountType { get; private set; }
    public Money CurrentBalance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { } // EF Core constructor

    private Account(AccountNumber accountNumber, string name, AccountType accountType, Money initialBalance)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        Name = name;
        AccountType = accountType;
        CurrentBalance = initialBalance;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static OneOf<Account, DomainError> Create(
        AccountNumber accountNumber, 
        string name, 
        AccountType accountType, 
        Money initialBalance)
    {
        if (accountNumber is null)
        {
            return new ValidationError("Account number cannot be null", "MISSING_ACCOUNT_NUMBER");
        }
        
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ValidationError("Account name is required", "MISSING_ACCOUNT_NAME");
        }

        if (name.Length > 100)
        {
            return new ValidationError("Account name cannot exceed 100 characters", "ACCOUNT_NAME_TOO_LONG");
        }

        if (initialBalance is null)
        {
            return new ValidationError("Initial balance cannot be null", "MISSING_INITIAL_BALANCE");
        }

        bool canHaveNegativeBalance = accountType == AccountType.CreditCard;
        if (initialBalance.IsNegative && !canHaveNegativeBalance)
        {
            return new NegativeBalanceNotAllowedError(accountType.ToString());
        }

        return new Account(accountNumber, name.Trim(), accountType, initialBalance);
    }
    
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Account name cannot be empty", nameof(newName));
            
        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddTransaction(Transaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
            
        if (transaction.AccountId != Id)
            throw new ArgumentException("Transaction does not belong to this account");
            
        _transactions.Add(transaction);
        RecalculateBalance();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RemoveTransaction(Transaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
            
        _transactions.Remove(transaction);
        RecalculateBalance();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public OneOf<Success, DomainError> UpdateBalance(Money newBalance)
    {
        if (newBalance == null)
            return new ValidationError("Balance cannot be null", "INVALID_BALANCE");
            
        if (newBalance.Currency != CurrentBalance.Currency)
            return new ValidationError("Currency mismatch when updating balance", "CURRENCY_MISMATCH");
            
        if (newBalance.IsNegative && !CanHaveNegativeBalance())
            return new NegativeBalanceNotAllowedError(AccountType.ToString());
            
        CurrentBalance = newBalance;
        UpdatedAt = DateTime.UtcNow;
        return new Success();
    }
    
    public bool CanHaveNegativeBalance()
    {
        return AccountType == AccountType.CreditCard;
    }
    
    public Money GetBalanceOnDate(DateTime date)
    {
        var relevantTransactions = _transactions
            .Where(t => t.TransactionDate <= date)
            .OrderBy(t => t.TransactionDate);
            
        var balance = Money.Zero(CurrentBalance.Currency);
        
        foreach (var transaction in relevantTransactions)
        {
            balance = balance.Add(transaction.Amount);
        }
        
        return balance;
    }
    
    private void RecalculateBalance()
    {
        var balance = Money.Zero(CurrentBalance.Currency);
        
        foreach (var transaction in _transactions.OrderBy(t => t.TransactionDate))
        {
            balance = balance.Add(transaction.Amount);
        }
        
        CurrentBalance = balance;
    }
}

public enum AccountType
{
    Checking,
    Savings,
    CreditCard
}
