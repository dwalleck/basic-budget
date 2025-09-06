using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;

using MediatR;

using OneOf;

namespace BasicBudget.Application.Commands;

public record CreateAccountCommand(
    string AccountNumber,
    string Name,
    AccountType AccountType,
    Money InitialBalance
) : IRequest<OneOf<Account, DomainError>>;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, OneOf<Account, DomainError>>
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<OneOf<Account, DomainError>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Create AccountNumber value object first
        var accountNumberResult = AccountNumber.Create(request.AccountNumber);
        if (accountNumberResult.IsT1)
        {
            return accountNumberResult.AsT1; // Return validation error
        }

        var accountNumber = accountNumberResult.AsT0;

        // Check if account number already exists
        var existingAccount = await _accountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);
        if (existingAccount != null)
        {
            return new DuplicateAccountNumberError(request.AccountNumber);
        }

        // Create the account using the domain factory method
        var accountResult = Account.Create(
            accountNumber,
            request.Name,
            request.AccountType,
            request.InitialBalance
        );

        if (accountResult.IsT1)
        {
            return accountResult.AsT1; // Return domain error
        }

        var account = accountResult.AsT0;

        // Persist the account
        await _accountRepository.AddAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);
        
        return account;
    }
}