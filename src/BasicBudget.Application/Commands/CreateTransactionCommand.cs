using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Errors;

namespace BasicBudget.Application.Commands;

public record CreateTransactionCommand(
    Guid AccountId,
    Money Amount,
    string Description,
    DateTime TransactionDate,
    Guid? CategoryId = null
) : IRequest<OneOf<Transaction, DomainError>>;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, OneOf<Transaction, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<OneOf<Transaction, DomainError>> Handle(
        CreateTransactionCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            return new AccountNotFoundError(request.AccountId);
        }

        // Validate category exists if provided
        Category? category = null;
        if (request.CategoryId.HasValue)
        {
            category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken);
            if (category == null)
            {
                return new CategoryNotFoundError(request.CategoryId.Value);
            }
        }

        // Validate transaction date is not in future
        if (request.TransactionDate > DateTime.UtcNow)
        {
            return new FutureTransactionDateError(request.TransactionDate);
        }

        // Create the transaction using domain entity factory method
        var transactionResult = Transaction.Create(
            account,
            request.Amount,
            request.Description,
            request.TransactionDate,
            request.CategoryId
        );

        // If transaction creation failed due to domain rules, return error
        if (transactionResult.IsT1)
        {
            return transactionResult.AsT1;
        }

        var transaction = transactionResult.AsT0;

        // Add transaction to account for balance recalculation
        account.AddTransaction(transaction);

        // Persist the transaction and updated account
        try
        {
            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _transactionRepository.SaveChangesAsync(cancellationToken);
            
            return transaction;
        }
        catch (Exception ex)
        {
            return new InfrastructureError(
                "Failed to save transaction to database",
                "TRANSACTION_SAVE_FAILED",
                ex
            );
        }
    }
}