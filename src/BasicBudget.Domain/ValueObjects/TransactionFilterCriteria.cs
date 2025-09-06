namespace BasicBudget.Domain.ValueObjects;

/// <summary>
/// Criteria for filtering transactions in repository queries
/// </summary>
public record TransactionFilterCriteria(
    Guid? AccountId = null,
    Guid? CategoryId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? DescriptionContains = null
)
{
    /// <summary>
    /// Creates a filter for transactions within a specific date range
    /// </summary>
    public static TransactionFilterCriteria ForDateRange(DateTime startDate, DateTime endDate)
    {
        return new TransactionFilterCriteria(StartDate: startDate, EndDate: endDate);
    }

    /// <summary>
    /// Creates a filter for transactions in a specific account
    /// </summary>
    public static TransactionFilterCriteria ForAccount(Guid accountId)
    {
        return new TransactionFilterCriteria(AccountId: accountId);
    }

    /// <summary>
    /// Creates a filter for transactions in a specific category
    /// </summary>
    public static TransactionFilterCriteria ForCategory(Guid categoryId)
    {
        return new TransactionFilterCriteria(CategoryId: categoryId);
    }

    /// <summary>
    /// Creates a filter for transactions within an amount range
    /// </summary>
    public static TransactionFilterCriteria ForAmountRange(decimal minAmount, decimal maxAmount)
    {
        return new TransactionFilterCriteria(MinAmount: minAmount, MaxAmount: maxAmount);
    }
}