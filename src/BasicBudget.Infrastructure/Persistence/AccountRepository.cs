using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;

namespace BasicBudget.Infrastructure.Persistence;

public class AccountRepository : IAccountRepository
{
    private readonly BasicBudgetDbContext _context;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(BasicBudgetDbContext context, ILogger<AccountRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving account with ID: {AccountId}", id);
        
        return await _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving account with account number: {AccountNumber}", accountNumber);
        
        return await _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate))
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all accounts");
        
        return await _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        _logger.LogDebug("Retrieving {Count} accounts by IDs", idList.Count);
        
        return await _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate))
            .Where(a => idList.Contains(a.Id))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Account> accounts, int totalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving paged accounts: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
        
        var query = _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate))
            .OrderBy(a => a.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var accounts = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (accounts, totalCount);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new account: {AccountName} ({AccountNumber})", account.Name, account.AccountNumber);
        
        await _context.Accounts.AddAsync(account, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Account> accounts, CancellationToken cancellationToken = default)
    {
        var accountList = accounts.ToList();
        _logger.LogDebug("Adding {Count} new accounts", accountList.Count);
        
        await _context.Accounts.AddRangeAsync(accountList, cancellationToken);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating account: {AccountName} ({AccountNumber})", account.Name, account.AccountNumber);
        
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting account: {AccountName} ({AccountNumber})", account.Name, account.AccountNumber);
        
        _context.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving account repository changes");
        return await _context.SaveChangesAsync(cancellationToken);
    }
}