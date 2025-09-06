using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Errors;

namespace BasicBudget.Application.Commands;

public record ImportStatementCommand(
    Guid AccountId,
    string FileFormat, // "CSV", "QFX", "OFX"
    string FileContent,
    bool SkipDuplicates = true
) : IRequest<OneOf<ImportStatementResult, DomainError>>;

public record ImportStatementResult(
    IReadOnlyList<Transaction> ImportedTransactions,
    ImportSummary Summary
);

public record ImportSummary(
    int TotalTransactions,
    int SuccessfulImports,
    int FailedImports,
    int DuplicatesSkipped
);

public class ImportStatementCommandHandler : IRequestHandler<ImportStatementCommand, OneOf<ImportStatementResult, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IStatementParser _statementParser;

    public ImportStatementCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IStatementParser statementParser)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _statementParser = statementParser;
    }

    public async Task<OneOf<ImportStatementResult, DomainError>> Handle(
        ImportStatementCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            return DomainError.NotFound(
                "ACCOUNT_NOT_FOUND",
                $"Account with ID '{request.AccountId}' was not found"
            );
        }

        // Parse the statement file
        var parseResult = await _statementParser.ParseAsync(
            request.FileFormat, 
            request.FileContent, 
            cancellationToken
        );

        if (parseResult.IsT1)
        {
            return parseResult.AsT1;
        }

        var parsedTransactions = parseResult.AsT0;
        var importedTransactions = new List<Transaction>();
        var successfulImports = 0;
        var failedImports = 0;
        var duplicatesSkipped = 0;

        foreach (var transactionData in parsedTransactions)
        {
            try
            {
                // Check for duplicates if skip duplicates is enabled
                if (request.SkipDuplicates)
                {
                    var existingTransaction = await _transactionRepository.FindDuplicateAsync(
                        request.AccountId,
                        transactionData.Amount,
                        transactionData.Description,
                        transactionData.TransactionDate,
                        cancellationToken
                    );

                    if (existingTransaction != null)
                    {
                        duplicatesSkipped++;
                        continue;
                    }
                }

                // Create transaction using domain entity
                var transactionResult = Transaction.Create(
                    account,
                    transactionData.Amount,
                    transactionData.Description,
                    transactionData.TransactionDate,
                    category: null // Categories will be assigned later through categorization
                );

                if (transactionResult.IsT1)
                {
                    failedImports++;
                    continue;
                }

                var transaction = transactionResult.AsT0;
                importedTransactions.Add(transaction);
                successfulImports++;

                // Update account balance
                var balanceUpdateResult = account.UpdateBalance(transactionData.Amount);
                if (balanceUpdateResult.IsT1)
                {
                    failedImports++;
                    importedTransactions.Remove(transaction);
                    successfulImports--;
                    continue;
                }
            }
            catch
            {
                failedImports++;
            }
        }

        // Save all imported transactions
        try
        {
            if (importedTransactions.Any())
            {
                await _transactionRepository.AddRangeAsync(importedTransactions, cancellationToken);
                await _accountRepository.UpdateAsync(account, cancellationToken);
                await _transactionRepository.SaveChangesAsync(cancellationToken);
            }

            var summary = new ImportSummary(
                parsedTransactions.Count,
                successfulImports,
                failedImports,
                duplicatesSkipped
            );

            return new ImportStatementResult(importedTransactions, summary);
        }
        catch (Exception ex)
        {
            return DomainError.Infrastructure(
                "IMPORT_SAVE_FAILED",
                "Failed to save imported transactions to database",
                ex
            );
        }
    }
}

// Interface for statement parsing - will be implemented in Infrastructure layer
public interface IStatementParser
{
    Task<OneOf<IReadOnlyList<ParsedTransactionData>, DomainError>> ParseAsync(
        string format, 
        string content, 
        CancellationToken cancellationToken
    );
}

public record ParsedTransactionData(
    Money Amount,
    string Description,
    DateTime TransactionDate
);