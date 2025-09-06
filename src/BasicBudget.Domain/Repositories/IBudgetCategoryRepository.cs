using BasicBudget.Domain.Entities;

namespace BasicBudget.Domain.Repositories;

public interface IBudgetCategoryRepository
{
    Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BudgetCategory?> GetByBudgetAndCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BudgetCategory>> GetByBudgetAsync(Guid budgetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BudgetCategory>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BudgetCategory>> GetOverBudgetAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BudgetCategory>> GetNearingLimitAsync(decimal warningPercentage = 0.75m, CancellationToken cancellationToken = default);
    Task<IEnumerable<BudgetCategory>> GetRequiringAlertsAsync(CancellationToken cancellationToken = default);
    Task<BudgetCategory> AddAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default);
    Task<BudgetCategory> UpdateAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default);
    Task DeleteAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}