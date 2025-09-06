using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;

using MediatR;

using OneOf;

namespace BasicBudget.Application.Queries;

public record GetAccountQuery(Guid AccountId) : IRequest<OneOf<Account, DomainError>>;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, OneOf<Account, DomainError>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<OneOf<Account, DomainError>> Handle(
        GetAccountQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

            if (account == null)
            {
                return new AccountNotFoundError(request.AccountId);
            }

            return account;
        }
        catch (Exception ex)
        {
            // Infrastructure exceptions should bubble up, not be converted to domain errors
            throw;
        }
    }
}