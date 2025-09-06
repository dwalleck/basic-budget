using BasicBudget.Domain.Entities;

namespace BasicBudget.Domain.Repositories;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Budget?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Budget?> GetCurrentBudgetAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Budget?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Budget>> FindOverlappingAsync(DateTime startDate, DateTime? endDate, Guid? excludeBudgetId = null, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingBudgetAsync(DateTime startDate, DateTime? endDate, Guid? excludeBudgetId = null, CancellationToken cancellationToken = default);
    Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<Budget> UpdateAsync(Budget budget, CancellationToken cancellationToken = default);
    Task DeleteAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}