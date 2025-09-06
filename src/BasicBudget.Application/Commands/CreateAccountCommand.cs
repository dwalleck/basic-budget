using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Errors;

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

        // Create the account using domain entity constructor
        try
        {
            var account = new Account(
                accountNumber,
                request.Name,
                request.AccountType,
                request.InitialBalance
            );

            // Persist the account
            await _accountRepository.AddAsync(account, cancellationToken);
            await _accountRepository.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (ArgumentException ex)
        {
            // Domain validation errors from Account constructor
            // These should be converted to appropriate domain errors
            return new InvalidAccountNumberError(request.AccountNumber);
        }
    }
}