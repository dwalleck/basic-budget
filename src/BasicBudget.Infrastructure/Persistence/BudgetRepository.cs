using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BudgetRepository : IBudgetRepository
{
    private readonly BasicBudgetDbContext _context;

    public BudgetRepository(BasicBudgetDbContext context)
    {
        _context = context;
    }

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Budget?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.FirstOrDefaultAsync(b => b.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Budget>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Budget>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.Where(b => b.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Budget>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.Where(b => b.StartDate <= endDate && b.EndDate >= startDate).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Budget>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets.Where(b => b.Accounts.Any(a => a.Id == accountId)).ToListAsync(cancellationToken);
    }

    public async Task<Budget?> GetCurrentBudgetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Budgets
            .Where(b => b.IsActive && b.StartDate <= today && b.EndDate >= today)
            .Where(b => b.Accounts.Any(a => a.Id == accountId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Budget?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Budget>> FindOverlappingAsync(DateTime startDate, DateTime? endDate, Guid? excludeBudgetId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Budgets.AsQueryable();

        if (excludeBudgetId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBudgetId.Value);
        }

        return await query.Where(b => b.StartDate < endDate && b.EndDate > startDate).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingBudgetAsync(DateTime startDate, DateTime? endDate, Guid? excludeBudgetId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Budgets.AsQueryable();

        if (excludeBudgetId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBudgetId.Value);
        }

        return await query.AnyAsync(b => b.StartDate < endDate && b.EndDate > startDate, cancellationToken);
    }

    public async Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        await _context.Budgets.AddAsync(budget, cancellationToken);
        return budget;
    }

    public Task<Budget> UpdateAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Update(budget);
        return Task.FromResult(budget);
    }

    public Task DeleteAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Remove(budget);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
