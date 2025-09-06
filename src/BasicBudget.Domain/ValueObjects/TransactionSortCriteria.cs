namespace BasicBudget.Domain.ValueObjects;

/// <summary>
/// Criteria for sorting transactions in repository queries
/// </summary>
public record TransactionSortCriteria(
    string SortBy = "TransactionDate",
    bool SortDescending = true
)
{
    /// <summary>
    /// Valid sort fields for transactions
    /// </summary>
    public static class SortFields
    {
        public const string TransactionDate = "TransactionDate";
        public const string Amount = "Amount";
        public const string Description = "Description";
        public const string Account = "Account";
        public const string Category = "Category";
        public const string CreatedAt = "CreatedAt";
    }

    /// <summary>
    /// Sort by transaction date (newest first)
    /// </summary>
    public static TransactionSortCriteria ByDateDescending => new(SortFields.TransactionDate, true);

    /// <summary>
    /// Sort by transaction date (oldest first)
    /// </summary>
    public static TransactionSortCriteria ByDateAscending => new(SortFields.TransactionDate, false);

    /// <summary>
    /// Sort by amount (highest first)
    /// </summary>
    public static TransactionSortCriteria ByAmountDescending => new(SortFields.Amount, true);

    /// <summary>
    /// Sort by amount (lowest first)
    /// </summary>
    public static TransactionSortCriteria ByAmountAscending => new(SortFields.Amount, false);

    /// <summary>
    /// Sort by description alphabetically
    /// </summary>
    public static TransactionSortCriteria ByDescriptionAscending => new(SortFields.Description, false);

    /// <summary>
    /// Sort by account name alphabetically
    /// </summary>
    public static TransactionSortCriteria ByAccountAscending => new(SortFields.Account, false);

    /// <summary>
    /// Validates if the sort field is supported
    /// </summary>
    public bool IsValidSortField()
    {
        return SortBy.ToLowerInvariant() switch
        {
            "transactiondate" or "amount" or "description" or "account" or "category" or "createdat" => true,
            _ => false
        };
    }
}