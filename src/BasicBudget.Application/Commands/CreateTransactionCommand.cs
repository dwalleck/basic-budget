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
                return DomainError.NotFound(
                    "CATEGORY_NOT_FOUND",
                    $"Category with ID '{request.CategoryId}' was not found"
                );
            }
        }

        // Validate transaction date is not in future
        if (request.TransactionDate > DateTime.UtcNow)
        {
            return DomainError.Validation(
                "FUTURE_TRANSACTION_DATE",
                "Transaction date cannot be in the future",
                nameof(request.TransactionDate)
            );
        }

        // Create the transaction using domain entity factory method
        var transactionResult = Transaction.Create(
            account,
            request.Amount,
            request.Description,
            request.TransactionDate,
            category
        );

        // If transaction creation failed due to domain rules, return error
        if (transactionResult.IsT1)
        {
            return transactionResult.AsT1;
        }

        var transaction = transactionResult.AsT0;

        // Update account balance
        var balanceUpdateResult = account.UpdateBalance(request.Amount);
        if (balanceUpdateResult.IsT1)
        {
            return balanceUpdateResult.AsT1;
        }

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
            return DomainError.Infrastructure(
                "TRANSACTION_SAVE_FAILED",
                "Failed to save transaction to database",
                ex
            );
        }
    }
}