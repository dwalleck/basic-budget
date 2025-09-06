using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByTypeAsync(AccountType accountType, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default);
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);
    Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAsync(Account account, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}