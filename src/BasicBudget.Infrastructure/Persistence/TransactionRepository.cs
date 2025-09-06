using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BasicBudget.Infrastructure.Persistence;

public class TransactionRepository : ITransactionRepository
{
    private readonly BasicBudgetDbContext _context;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(BasicBudgetDbContext context, ILogger<TransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transaction with ID: {TransactionId}", id);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        _logger.LogDebug("Retrieving {Count} transactions by IDs", idList.Count);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => idList.Contains(t.Id))
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transactions for account: {AccountId}", accountId);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transactions for category: {CategoryId}", categoryId);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountAndDateRangeAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transactions for account {AccountId} from {StartDate} to {EndDate}",
            accountId, startDate, endDate);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.AccountId == accountId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetUncategorizedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving uncategorized transactions");

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.CategoryId == null)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetUnreconciledAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving unreconciled transactions for account: {AccountId}", accountId);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.AccountId == accountId && !t.IsReconciled)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByDescriptionAsync(string descriptionPattern, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transactions matching description pattern: {Pattern}", descriptionPattern);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Description.Contains(descriptionPattern))
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Transaction> transactions, int totalCount)> GetPagedAsync(
        TransactionFilterCriteria filter,
        TransactionSortCriteria sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving paged transactions with filters - Page: {PageNumber}, Size: {PageSize}",
            pageNumber, pageSize);

        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .AsQueryable();

        // Apply filters
        query = ApplyFilters(query, filter);

        // Apply sorting
        query = ApplySorting(query, sort);

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (transactions, totalCount);
    }

    public async Task<Transaction?> FindDuplicateAsync(
        Guid accountId,
        Money amount,
        DateTime transactionDate,
        string description,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for duplicate transaction - Account: {AccountId}, Amount: {Amount}, Date: {Date}",
            accountId, amount.Amount, transactionDate);

        // Consider transactions within +/- 1 day as potential duplicates
        var startDate = transactionDate.Date.AddDays(-1);
        var endDate = transactionDate.Date.AddDays(1);

        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t =>
                t.AccountId == accountId &&
                t.Amount.Amount == amount.Amount &&
                t.Amount.Currency == amount.Currency &&
                t.Description == description &&
                t.TransactionDate >= startDate &&
                t.TransactionDate <= endDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetForDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? accountId = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving transactions for date range: {StartDate} to {EndDate}", startDate, endDate);

        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new transaction: {Description} - {Amount}",
            transaction.Description, transaction.Amount.Formatted);

        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        var transactionList = transactions.ToList();
        _logger.LogDebug("Adding {Count} new transactions", transactionList.Count);

        await _context.Transactions.AddRangeAsync(transactionList, cancellationToken);
        return transactionList;
    }

    public Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating transaction: {Description} - {Amount}",
            transaction.Description, transaction.Amount.Formatted);

        _context.Transactions.Update(transaction);
        return Task.FromResult(transaction);
    }

    public Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting transaction: {Description} - {Amount}",
            transaction.Description, transaction.Amount.Formatted);

        _context.Transactions.Remove(transaction);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AnyAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving transaction repository changes");
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilterCriteria criteria)
    {
        if (criteria.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == criteria.AccountId.Value);
        }

        if (criteria.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == criteria.CategoryId.Value);
        }

        if (criteria.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= criteria.StartDate.Value);
        }

        if (criteria.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= criteria.EndDate.Value);
        }

        if (criteria.MinAmount.HasValue)
        {
            query = query.Where(t => Math.Abs(t.Amount.Amount) >= criteria.MinAmount.Value);
        }

        if (criteria.MaxAmount.HasValue)
        {
            query = query.Where(t => Math.Abs(t.Amount.Amount) <= criteria.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.DescriptionContains))
        {
            query = query.Where(t => t.Description.Contains(criteria.DescriptionContains));
        }

        return query;
    }

    private static IQueryable<Transaction> ApplySorting(IQueryable<Transaction> query, TransactionSortCriteria criteria)
    {
        return criteria.SortBy.ToLowerInvariant() switch
        {
            "transactiondate" => criteria.SortDescending
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate),

            "amount" => criteria.SortDescending
                ? query.OrderByDescending(t => Math.Abs(t.Amount.Amount))
                : query.OrderBy(t => Math.Abs(t.Amount.Amount)),

            "description" => criteria.SortDescending
                ? query.OrderByDescending(t => t.Description)
                : query.OrderBy(t => t.Description),

            "account" => criteria.SortDescending
                ? query.OrderByDescending(t => t.Account.Name)
                : query.OrderBy(t => t.Account.Name),

            "category" => criteria.SortDescending
                ? query.OrderByDescending(t => t.Category!.Name)
                : query.OrderBy(t => t.Category!.Name),

            "createdat" => criteria.SortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),

            _ => query.OrderByDescending(t => t.TransactionDate) // Default sort
        };
    }
}