using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Services;

public interface IBudgetCalculationService
{
    /// <summary>
    /// Calculates projected overage based on current spending trends
    /// </summary>
    /// <param name="budget">The budget to analyze</param>
    /// <param name="transactions">Historical transactions for the budget period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Projected overage information if overage is likely, null otherwise</returns>
    Task<ProjectedOverage?> CalculateProjectedOverageAsync(
        Budget budget, 
        IReadOnlyList<Transaction> transactions, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates spending velocity for a budget category
    /// </summary>
    /// <param name="budgetCategory">The budget category to analyze</param>
    /// <param name="transactions">Transactions for the category</param>
    /// <returns>Average daily spending rate</returns>
    decimal CalculateSpendingVelocity(BudgetCategory budgetCategory, IReadOnlyList<Transaction> transactions);

    /// <summary>
    /// Calculates remaining budget days
    /// </summary>
    /// <param name="budget">The budget to analyze</param>
    /// <returns>Number of days remaining in budget period</returns>
    int CalculateRemainingDays(Budget budget);

    /// <summary>
    /// Determines if a budget category is on track to stay within limits
    /// </summary>
    /// <param name="budgetCategory">The budget category to analyze</param>
    /// <param name="transactions">Transactions for the category</param>
    /// <param name="remainingDays">Days remaining in budget period</param>
    /// <returns>True if on track, false if likely to exceed</returns>
    bool IsCategoryOnTrack(BudgetCategory budgetCategory, IReadOnlyList<Transaction> transactions, int remainingDays);
}

/// <summary>
/// Represents projected budget overage information
/// </summary>
public record ProjectedOverage(
    Money ProjectedOverageAmount,
    DateTime ProjectedDate,
    IReadOnlyList<Guid> ContributingCategoryIds
);