using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Entities;

public class BudgetCategory
{
    public Guid Id { get; private set; }
    public Guid BudgetId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Money AllocatedAmount { get; private set; }
    public Money SpentAmount { get; private set; }
    public decimal AlertThreshold { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Budget Budget { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    private BudgetCategory() { } // EF Core constructor

    public BudgetCategory(Guid budgetId, Guid categoryId, Money allocatedAmount, decimal alertThreshold = 0.8m)
    {
        Id = Guid.NewGuid();
        BudgetId = budgetId;
        CategoryId = categoryId;
        AllocatedAmount = allocatedAmount ?? throw new ArgumentNullException(nameof(allocatedAmount));
        SpentAmount = Money.Zero(allocatedAmount.Currency);
        AlertThreshold = alertThreshold;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ValidateBusinessRules();
    }

    public void UpdateAllocatedAmount(Money newAmount)
    {
        if (newAmount == null)
            throw new ArgumentNullException(nameof(newAmount));

        if (!newAmount.IsPositive)
            throw new ArgumentException("Allocated amount must be positive");

        if (newAmount.Currency != AllocatedAmount.Currency)
            throw new ArgumentException("Currency must match existing allocation");

        AllocatedAmount = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSpentAmount(Money newAmount)
    {
        if (newAmount == null)
            throw new ArgumentNullException(nameof(newAmount));

        if (newAmount.Currency != SpentAmount.Currency)
            throw new ArgumentException("Currency must match existing spent amount");

        SpentAmount = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddToSpentAmount(Money additionalAmount)
    {
        if (additionalAmount == null)
            throw new ArgumentNullException(nameof(additionalAmount));

        if (additionalAmount.Currency != SpentAmount.Currency)
            throw new ArgumentException("Currency must match existing spent amount");

        SpentAmount = SpentAmount.Add(additionalAmount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubtractFromSpentAmount(Money amountToSubtract)
    {
        if (amountToSubtract == null)
            throw new ArgumentNullException(nameof(amountToSubtract));

        if (amountToSubtract.Currency != SpentAmount.Currency)
            throw new ArgumentException("Currency must match existing spent amount");

        SpentAmount = SpentAmount.Subtract(amountToSubtract);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAlertThreshold(decimal newThreshold)
    {
        if (newThreshold < 0 || newThreshold > 1)
            throw new ArgumentException("Alert threshold must be between 0 and 1");

        AlertThreshold = newThreshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public Money GetRemainingAmount()
    {
        return AllocatedAmount.Subtract(SpentAmount);
    }

    public decimal GetPercentageSpent()
    {
        if (AllocatedAmount.IsZero)
            return 0m;

        return Math.Min(100m, (SpentAmount.Amount / AllocatedAmount.Amount) * 100m);
    }

    public decimal GetPercentageRemaining()
    {
        return Math.Max(0m, 100m - GetPercentageSpent());
    }

    public bool IsOverBudget()
    {
        return SpentAmount.Amount > AllocatedAmount.Amount;
    }

    public bool ShouldAlert()
    {
        return GetPercentageSpent() / 100m >= AlertThreshold;
    }

    public Money GetOverageAmount()
    {
        if (!IsOverBudget())
            return Money.Zero(AllocatedAmount.Currency);

        return SpentAmount.Subtract(AllocatedAmount);
    }

    public Money GetAlertThresholdAmount()
    {
        return AllocatedAmount.Multiply(AlertThreshold);
    }

    public bool IsNearingLimit(decimal warningPercentage = 0.75m)
    {
        return GetPercentageSpent() / 100m >= warningPercentage && !IsOverBudget();
    }

    public string GetStatusDescription()
    {
        if (IsOverBudget())
            return $"Over budget by {GetOverageAmount().Formatted}";

        if (ShouldAlert())
            return $"Alert threshold reached ({GetPercentageSpent():F1}%)";

        if (IsNearingLimit())
            return $"Nearing limit ({GetPercentageSpent():F1}%)";

        return $"On track ({GetPercentageSpent():F1}%)";
    }

    public Money ProjectEndOfPeriodSpending(DateTime budgetEndDate)
    {
        if (Budget?.StartDate == null || budgetEndDate <= Budget.StartDate)
            return SpentAmount;

        var totalDays = (budgetEndDate - Budget.StartDate).Days;
        var daysPassed = (DateTime.UtcNow - Budget.StartDate).Days;

        if (daysPassed <= 0)
            return SpentAmount;

        var dailySpendingRate = SpentAmount.Amount / daysPassed;
        var projectedTotal = dailySpendingRate * totalDays;

        var result = Money.Create(projectedTotal, SpentAmount.Currency);
        return result.Match(
            money => money,
            error => throw new InvalidOperationException($"Failed to create projected spending amount: {error.Message}")
        );
    }

    private void ValidateBusinessRules()
    {
        if (BudgetId == Guid.Empty)
            throw new ArgumentException("Budget ID cannot be empty");

        if (CategoryId == Guid.Empty)
            throw new ArgumentException("Category ID cannot be empty");

        if (!AllocatedAmount.IsPositive)
            throw new ArgumentException("Allocated amount must be positive");

        if (AlertThreshold < 0 || AlertThreshold > 1)
            throw new ArgumentException("Alert threshold must be between 0 and 1");
    }
}