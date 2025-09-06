using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByAccountAndDateRangeAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetUncategorizedAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetUnreconciledAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByDescriptionAsync(string descriptionPattern, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Transaction> transactions, int totalCount)> GetPagedAsync(
        TransactionFilterCriteria filter,
        TransactionSortCriteria sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Transaction?> FindDuplicateAsync(
        Guid accountId,
        Money amount,
        DateTime transactionDate,
        string description,
        CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}