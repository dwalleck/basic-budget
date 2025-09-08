using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CategoryRepository : ICategoryRepository
{
    private readonly BasicBudgetDbContext _context;

    public CategoryRepository(BasicBudgetDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FindAsync([id], cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(string name, Guid? parentCategoryId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == name && c.ParentCategoryId == parentCategoryId, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetHierarchyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.Children)
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetChildrenAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.Where(c => c.ParentCategoryId == parentCategoryId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetSystemGeneratedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories.Where(c => c.IsSystemGenerated).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? parentCategoryId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.AnyAsync(c => c.Name == name && c.ParentCategoryId == parentCategoryId, cancellationToken);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
        return category;
    }

    public Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        return Task.FromResult(category);
    }

    public Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
