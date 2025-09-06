using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.ValueObjects;

namespace BasicBudget.Application.Queries;

public record GetTransactionsQuery(
    Guid? AccountId = null,
    Guid? CategoryId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int PageSize = 20,
    int PageNumber = 1,
    string? SortBy = "TransactionDate",
    bool SortDescending = true
) : IRequest<OneOf<TransactionPagedResult, DomainError>>;

public record TransactionPagedResult(
    IReadOnlyList<Transaction> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage
);

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, OneOf<TransactionPagedResult, DomainError>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetTransactionsQueryHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<OneOf<TransactionPagedResult, DomainError>> Handle(
        GetTransactionsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate account exists if specified
            if (request.AccountId.HasValue)
            {
                var account = await _accountRepository.GetByIdAsync(request.AccountId.Value, cancellationToken);
                if (account == null)
                {
                    return new AccountNotFoundError(request.AccountId.Value);
                }
            }

            // Validate category exists if specified
            if (request.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken);
                if (category == null)
                {
                    return new CategoryNotFoundError(request.CategoryId.Value);
                }
            }

            // Validate pagination parameters
            if (request.PageSize <= 0 || request.PageSize > 100)
            {
                return new ValidationError("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");
            }

            if (request.PageNumber <= 0)
            {
                return new ValidationError("Page number must be greater than 0", "INVALID_PAGE_NUMBER");
            }

            // Validate date range
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate > request.EndDate)
            {
                return new ValidationError("Start date must be before or equal to end date", "INVALID_DATE_RANGE");
            }

            // Build filter criteria
            var filterCriteria = new TransactionFilterCriteria(
                request.AccountId,
                request.CategoryId,
                request.StartDate,
                request.EndDate
            );

            // Build sort criteria
            var sortCriteria = new TransactionSortCriteria(
                request.SortBy ?? "TransactionDate",
                request.SortDescending
            );

            // Execute query with pagination
            var (transactions, totalCount) = await _transactionRepository.GetPagedAsync(
                filterCriteria,
                sortCriteria,
                request.PageNumber,
                request.PageSize,
                cancellationToken
            );

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var hasNextPage = request.PageNumber < totalPages;
            var hasPreviousPage = request.PageNumber > 1;

            var result = new TransactionPagedResult(
                transactions.ToList().AsReadOnly(),
                totalCount,
                request.PageNumber,
                request.PageSize,
                hasNextPage,
                hasPreviousPage
            );

            return result;
        }
        catch (Exception ex)
        {
            // Infrastructure exceptions should bubble up, not be converted to domain errors
            throw;
        }
    }
}