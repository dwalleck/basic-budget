using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class AccountRepository : IAccountRepository
{
    private readonly BasicBudgetDbContext _context;

    public AccountRepository(BasicBudgetDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.FindAsync([id], cancellationToken);
    }

    public async Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByTypeAsync(AccountType accountType, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.Where(a => a.AccountType == accountType).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
        return account;
    }

    public Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        return Task.FromResult(account);
    }

    public Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
