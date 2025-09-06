using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BudgetCategoryRepository : IBudgetCategoryRepository
{
    private readonly BasicBudgetDbContext _context;

    public BudgetCategoryRepository(BasicBudgetDbContext context)
    {
        _context = context;
    }

    public async Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<BudgetCategory?> GetByBudgetAndCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .FirstOrDefaultAsync(bc => bc.BudgetId == budgetId && bc.CategoryId == categoryId, cancellationToken);
    }

    public async Task<IEnumerable<BudgetCategory>> GetByBudgetAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .Where(bc => bc.BudgetId == budgetId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BudgetCategory>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .Where(bc => bc.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BudgetCategory>> GetOverBudgetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .Where(bc => bc.SpentAmount.Amount > bc.AllocatedAmount.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BudgetCategory>> GetNearingLimitAsync(decimal warningPercentage = 0.75m, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .Where(bc => (bc.SpentAmount.Amount / bc.AllocatedAmount.Amount) >= warningPercentage)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BudgetCategory>> GetRequiringAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BudgetCategories
            .Where(bc => (bc.SpentAmount.Amount / bc.AllocatedAmount.Amount) >= bc.AlertThreshold)
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetCategory> AddAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default)
    {
        await _context.BudgetCategories.AddAsync(budgetCategory, cancellationToken);
        return budgetCategory;
    }

    public Task<BudgetCategory> UpdateAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default)
    {
        _context.BudgetCategories.Update(budgetCategory);
        return Task.FromResult(budgetCategory);
    }

    public Task DeleteAsync(BudgetCategory budgetCategory, CancellationToken cancellationToken = default)
    {
        _context.BudgetCategories.Remove(budgetCategory);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
