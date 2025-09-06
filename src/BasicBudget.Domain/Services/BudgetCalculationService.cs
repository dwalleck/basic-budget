using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Services;

public class BudgetCalculationService
{
    public Money CalculateSpentAmount(Budget budget, Guid categoryId, DateTime startDate, DateTime endDate)
    {
        if (budget == null)
            throw new ArgumentNullException(nameof(budget));

        var budgetCategory = budget.BudgetCategories.FirstOrDefault(bc => bc.CategoryId == categoryId);
        if (budgetCategory == null)
            return Money.Zero();

        // Calculate spent amount from transactions within the specified period
        var spentAmount = Money.Zero(budgetCategory.AllocatedAmount.Currency);

        foreach (var account in budget.Accounts)
        {
            var relevantTransactions = account.Transactions
                .Where(t => t.CategoryId == categoryId)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .Where(t => t.Amount.IsNegative) // Only expenses
                .ToList();

            foreach (var transaction in relevantTransactions)
            {
                spentAmount = spentAmount.Add(transaction.GetAbsoluteAmount());
            }
        }

        return spentAmount;
    }

    public decimal CalculateBudgetProgress(Budget budget)
    {
        if (budget == null)
            throw new ArgumentNullException(nameof(budget));

        var totalAllocated = budget.GetTotalAllocated();
        if (totalAllocated.IsZero)
            return 0m;

        var totalSpent = budget.GetTotalSpent();
        return Math.Min(100m, (totalSpent.Amount / totalAllocated.Amount) * 100m);
    }

    public BudgetProgressSummary GenerateBudgetProgressSummary(Budget budget)
    {
        if (budget == null)
            throw new ArgumentNullException(nameof(budget));

        var overallProgress = CalculateBudgetProgress(budget);
        var categoryProgress = new List<BudgetCategoryProgressItem>();

        foreach (var budgetCategory in budget.BudgetCategories)
        {
            var progressItem = new BudgetCategoryProgressItem
            {
                BudgetCategory = budgetCategory,
                ProgressPercentage = budgetCategory.GetPercentageSpent(),
                IsOverBudget = budgetCategory.IsOverBudget(),
                ProjectedTotal = budget.EndDate.HasValue
                    ? budgetCategory.ProjectEndOfPeriodSpending(budget.EndDate.Value)
                    : budgetCategory.SpentAmount
            };

            categoryProgress.Add(progressItem);
        }

        var projectedOverage = CalculateProjectedOverage(budget);
        var daysRemaining = budget.GetDaysRemaining();

        return new BudgetProgressSummary
        {
            Budget = budget,
            OverallProgress = overallProgress,
            CategoryProgress = categoryProgress,
            ProjectedOverage = projectedOverage.IsPositive ? projectedOverage : null,
            DaysRemaining = daysRemaining
        };
    }

    public PeriodicSummary GeneratePeriodicSummary(
        IEnumerable<Account> accounts,
        DateTime startDate,
        DateTime endDate,
        PeriodicSummaryType summaryType)
    {
        if (accounts == null)
            throw new ArgumentNullException(nameof(accounts));

        var accountsList = accounts.ToList();
        if (!accountsList.Any())
            throw new ArgumentException("At least one account is required", nameof(accounts));

        var currency = accountsList.First().CurrentBalance.Currency;
        var allTransactions = accountsList
            .SelectMany(a => a.Transactions)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        var totalIncome = allTransactions
            .Where(t => t.IsIncome)
            .Aggregate(Money.Zero(currency), (sum, t) => sum.Add(t.Amount));

        var totalExpenses = allTransactions
            .Where(t => t.IsExpense)
            .Aggregate(Money.Zero(currency), (sum, t) => sum.Add(t.GetAbsoluteAmount()));

        var netAmount = totalIncome.Subtract(totalExpenses);

        var categoryBreakdown = GenerateCategoryBreakdown(allTransactions, currency);
        var accountBreakdown = GenerateAccountBreakdown(accountsList, startDate, endDate);

        return new PeriodicSummary
        {
            Period = new DateRange { StartDate = startDate, EndDate = endDate },
            SummaryType = summaryType,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetAmount = netAmount,
            CategoryBreakdown = categoryBreakdown,
            AccountBreakdown = accountBreakdown
        };
    }

    private Money CalculateProjectedOverage(Budget budget)
    {
        if (!budget.EndDate.HasValue)
            return Money.Zero(budget.GetTotalAllocated().Currency);

        var totalProjectedOverage = Money.Zero(budget.GetTotalAllocated().Currency);

        foreach (var budgetCategory in budget.BudgetCategories)
        {
            var projectedTotal = budgetCategory.ProjectEndOfPeriodSpending(budget.EndDate.Value);
            if (projectedTotal.Amount > budgetCategory.AllocatedAmount.Amount)
            {
                var overage = projectedTotal.Subtract(budgetCategory.AllocatedAmount);
                totalProjectedOverage = totalProjectedOverage.Add(overage);
            }
        }

        return totalProjectedOverage;
    }

    private List<CategorySummaryItem> GenerateCategoryBreakdown(List<Transaction> transactions, string currency)
    {
        return transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummaryItem
            {
                Category = g.Key!,
                TotalAmount = g.Aggregate(Money.Zero(currency), (sum, t) => sum.Add(t.GetAbsoluteAmount())),
                TransactionCount = g.Count(),
                AverageAmount = Money.Create(g.Average(t => t.GetAbsoluteAmount().Amount), currency).Match(
                    money => money,
                    error => Money.Zero(currency)
                )
            })
            .OrderByDescending(c => c.TotalAmount.Amount)
            .ToList();
    }

    private List<AccountSummaryItem> GenerateAccountBreakdown(List<Account> accounts, DateTime startDate, DateTime endDate)
    {
        return accounts
            .Select(a => new AccountSummaryItem
            {
                Account = a,
                TotalAmount = a.Transactions
                    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                    .Aggregate(Money.Zero(a.CurrentBalance.Currency), (sum, t) => sum.Add(t.Amount)),
                TransactionCount = a.Transactions
                    .Count(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            })
            .ToList();
    }
}

// Supporting classes for the service
public class BudgetProgressSummary
{
    public Budget Budget { get; set; } = null!;
    public decimal OverallProgress { get; set; }
    public List<BudgetCategoryProgressItem> CategoryProgress { get; set; } = new();
    public Money? ProjectedOverage { get; set; }
    public int DaysRemaining { get; set; }
}

public class BudgetCategoryProgressItem
{
    public BudgetCategory BudgetCategory { get; set; } = null!;
    public decimal ProgressPercentage { get; set; }
    public bool IsOverBudget { get; set; }
    public Money ProjectedTotal { get; set; } = null!;
}

public class PeriodicSummary
{
    public DateRange Period { get; set; } = null!;
    public PeriodicSummaryType SummaryType { get; set; }
    public Money TotalIncome { get; set; } = null!;
    public Money TotalExpenses { get; set; } = null!;
    public Money NetAmount { get; set; } = null!;
    public List<CategorySummaryItem> CategoryBreakdown { get; set; } = new();
    public List<AccountSummaryItem> AccountBreakdown { get; set; } = new();
}

public class CategorySummaryItem
{
    public Category Category { get; set; } = null!;
    public Money TotalAmount { get; set; } = null!;
    public int TransactionCount { get; set; }
    public Money AverageAmount { get; set; } = null!;
}

public class AccountSummaryItem
{
    public Account Account { get; set; } = null!;
    public Money TotalAmount { get; set; } = null!;
    public int TransactionCount { get; set; }
}

public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public enum PeriodicSummaryType
{
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}