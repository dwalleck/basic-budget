using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Entities;

public class Budget
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public BudgetType BudgetType { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    private readonly List<BudgetCategory> _budgetCategories = new();
    public IReadOnlyCollection<BudgetCategory> BudgetCategories => _budgetCategories.AsReadOnly();

    private readonly List<Account> _accounts = new();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    private Budget() { } // EF Core constructor

    public Budget(string name, BudgetType budgetType, DateTime startDate, DateTime? endDate = null)
    {
        Id = Guid.NewGuid();
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        BudgetType = budgetType;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ValidateBusinessRules();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Budget name cannot be empty", nameof(newName));

        if (newName.Length > 100)
            throw new ArgumentException("Budget name cannot exceed 100 characters");

        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEndDate(DateTime? newEndDate)
    {
        if (newEndDate.HasValue && newEndDate.Value <= StartDate)
            throw new ArgumentException("End date must be after start date");

        EndDate = newEndDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        if (_accounts.Any(a => a.Id == account.Id))
            throw new ArgumentException("Account is already included in this budget");

        _accounts.Add(account);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        _accounts.Remove(account);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddBudgetCategory(BudgetCategory budgetCategory)
    {
        if (budgetCategory == null)
            throw new ArgumentNullException(nameof(budgetCategory));

        if (budgetCategory.BudgetId != Id)
            throw new ArgumentException("Budget category does not belong to this budget");

        if (_budgetCategories.Any(bc => bc.CategoryId == budgetCategory.CategoryId))
            throw new ArgumentException("Category is already included in this budget");

        _budgetCategories.Add(budgetCategory);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveBudgetCategory(BudgetCategory budgetCategory)
    {
        if (budgetCategory == null)
            throw new ArgumentNullException(nameof(budgetCategory));

        _budgetCategories.Remove(budgetCategory);
        UpdatedAt = DateTime.UtcNow;
    }

    public Money GetTotalAllocated()
    {
        if (!_budgetCategories.Any())
            return Money.Zero();

        var firstCurrency = _budgetCategories.First().AllocatedAmount.Currency;

        return _budgetCategories.Aggregate(
            Money.Zero(firstCurrency),
            (total, bc) => total.Add(bc.AllocatedAmount)
        );
    }

    public Money GetTotalSpent()
    {
        if (!_budgetCategories.Any())
            return Money.Zero();

        var firstCurrency = _budgetCategories.First().AllocatedAmount.Currency;

        return _budgetCategories.Aggregate(
            Money.Zero(firstCurrency),
            (total, bc) => total.Add(bc.SpentAmount)
        );
    }

    public decimal GetOverallProgress()
    {
        var totalAllocated = GetTotalAllocated();

        if (totalAllocated.IsZero)
            return 0m;

        var totalSpent = GetTotalSpent();
        return Math.Min(100m, (totalSpent.Amount / totalAllocated.Amount) * 100m);
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow.Date;

        if (!IsActive)
            return false;

        if (now < StartDate.Date)
            return false;

        if (EndDate.HasValue && now > EndDate.Value.Date)
            return false;

        return true;
    }

    public TimeSpan GetDuration()
    {
        var endDate = EndDate ?? DateTime.UtcNow;
        return endDate - StartDate;
    }

    public int GetDaysRemaining()
    {
        if (!EndDate.HasValue)
            return -1; // Ongoing budget

        var remaining = EndDate.Value.Date - DateTime.UtcNow.Date;
        return Math.Max(0, remaining.Days);
    }

    public bool HasOverlappingPeriod(Budget otherBudget)
    {
        if (otherBudget == null)
            return false;

        var thisEnd = EndDate ?? DateTime.MaxValue;
        var otherEnd = otherBudget.EndDate ?? DateTime.MaxValue;

        return StartDate < otherEnd && thisEnd > otherBudget.StartDate;
    }

    private void ValidateBusinessRules()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Budget name is required");

        if (Name.Length > 100)
            throw new ArgumentException("Budget name cannot exceed 100 characters");

        if (EndDate.HasValue && EndDate.Value <= StartDate)
            throw new ArgumentException("End date must be after start date");
    }
}

public enum BudgetType
{
    Basic,
    Complex,
    Monthly,
    Yearly,
    Custom
}