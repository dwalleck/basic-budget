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

    public Account(AccountNumber accountNumber, string name, AccountType accountType, Money initialBalance)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        AccountType = accountType;
        CurrentBalance = initialBalance ?? throw new ArgumentNullException(nameof(initialBalance));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        ValidateBusinessRules();
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
    
    private void ValidateBusinessRules()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Account name is required");
            
        if (Name.Length > 100)
            throw new ArgumentException("Account name cannot exceed 100 characters");
            
        if (CurrentBalance.IsNegative && !CanHaveNegativeBalance())
            throw new ArgumentException($"Account type {AccountType} cannot have negative balance");
    }
}

public enum AccountType
{
    Checking,
    Savings,
    CreditCard
}