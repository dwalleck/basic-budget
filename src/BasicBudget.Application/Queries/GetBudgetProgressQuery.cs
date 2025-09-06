using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Services;
using BasicBudget.Domain.Errors;

namespace BasicBudget.Application.Queries;

public record GetBudgetProgressQuery(Guid BudgetId) : IRequest<OneOf<BudgetProgressSummary, DomainError>>;

public record BudgetProgressSummary(
    Budget Budget,
    Money TotalAllocated,
    Money TotalSpent,
    Money TotalRemaining,
    decimal OverallPercentageUsed,
    IReadOnlyList<CategoryProgress> CategoryProgress,
    IReadOnlyList<BudgetAlert> ActiveAlerts,
    ProjectedOverage? ProjectedOverage
);

public record CategoryProgress(
    BudgetCategory BudgetCategory,
    Money SpentAmount,
    Money RemainingAmount,
    decimal PercentageUsed,
    IReadOnlyList<AlertThreshold> TriggeredAlerts
);

public record BudgetAlert(
    Guid BudgetCategoryId,
    string CategoryName,
    AlertType AlertType,
    decimal ThresholdPercentage,
    decimal CurrentPercentage,
    Money AllocatedAmount,
    Money SpentAmount,
    DateTime TriggeredAt
);

public record ProjectedOverage(
    Money ProjectedOverageAmount,
    DateTime ProjectedDate,
    IReadOnlyList<Guid> ContributingCategoryIds
);

public class GetBudgetProgressQueryHandler : IRequestHandler<GetBudgetProgressQuery, OneOf<BudgetProgressSummary, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetCalculationService _budgetCalculationService;

    public GetBudgetProgressQueryHandler(
        IBudgetRepository budgetRepository,
        ITransactionRepository transactionRepository,
        IBudgetCalculationService budgetCalculationService)
    {
        _budgetRepository = budgetRepository;
        _transactionRepository = transactionRepository;
        _budgetCalculationService = budgetCalculationService;
    }

    public async Task<OneOf<BudgetProgressSummary, DomainError>> Handle(
        GetBudgetProgressQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Get budget with categories
            var budget = await _budgetRepository.GetByIdWithCategoriesAsync(request.BudgetId, cancellationToken);
            if (budget == null)
            {
                return DomainError.NotFound(
                    "BUDGET_NOT_FOUND",
                    $"Budget with ID '{request.BudgetId}' was not found"
                );
            }

            // Get all transactions for this budget period
            var filterCriteria = new TransactionFilterCriteria(
                AccountId: null,
                CategoryId: null,
                StartDate: budget.StartDate,
                EndDate: budget.EndDate
            );

            var (transactions, _) = await _transactionRepository.GetPagedAsync(
                filterCriteria,
                new TransactionSortCriteria("TransactionDate", false),
                pageNumber: 1,
                pageSize: int.MaxValue, // Get all transactions for the period
                cancellationToken
            );

            // Calculate spending by category
            var spendingByCategory = transactions
                .Where(t => t.CategoryId.HasValue)
                .GroupBy(t => t.CategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount))); // Use absolute value for spending

            // Calculate progress for each category
            var categoryProgressList = new List<CategoryProgress>();
            var activeAlerts = new List<BudgetAlert>();

            foreach (var budgetCategory in budget.Categories)
            {
                var spentAmount = spendingByCategory.TryGetValue(budgetCategory.CategoryId, out var spent) 
                    ? new Money(spent, budgetCategory.AllocatedAmount.Currency)
                    : new Money(0, budgetCategory.AllocatedAmount.Currency);

                var remainingAmount = budgetCategory.AllocatedAmount - spentAmount;
                var percentageUsed = budgetCategory.AllocatedAmount.Amount == 0 
                    ? 0 
                    : (spentAmount.Amount / budgetCategory.AllocatedAmount.Amount) * 100;

                // Check for triggered alerts
                var triggeredAlerts = budgetCategory.AlertThresholds
                    .Where(threshold => percentageUsed >= threshold.Percentage)
                    .ToList();

                if (triggeredAlerts.Any())
                {
                    foreach (var threshold in triggeredAlerts)
                    {
                        activeAlerts.Add(new BudgetAlert(
                            budgetCategory.Id,
                            budgetCategory.Category.Name,
                            threshold.AlertType,
                            threshold.Percentage,
                            percentageUsed,
                            budgetCategory.AllocatedAmount,
                            spentAmount,
                            DateTime.UtcNow
                        ));
                    }
                }

                categoryProgressList.Add(new CategoryProgress(
                    budgetCategory,
                    spentAmount,
                    remainingAmount,
                    percentageUsed,
                    triggeredAlerts
                ));
            }

            // Calculate overall totals
            var totalAllocated = new Money(
                budget.Categories.Sum(c => c.AllocatedAmount.Amount),
                budget.Categories.First().AllocatedAmount.Currency
            );

            var totalSpent = new Money(
                categoryProgressList.Sum(cp => cp.SpentAmount.Amount),
                totalAllocated.Currency
            );

            var totalRemaining = totalAllocated - totalSpent;
            var overallPercentageUsed = totalAllocated.Amount == 0 
                ? 0 
                : (totalSpent.Amount / totalAllocated.Amount) * 100;

            // Calculate projected overage if applicable
            var projectedOverage = await _budgetCalculationService.CalculateProjectedOverageAsync(
                budget, 
                transactions,
                cancellationToken
            );

            var summary = new BudgetProgressSummary(
                budget,
                totalAllocated,
                totalSpent,
                totalRemaining,
                overallPercentageUsed,
                categoryProgressList,
                activeAlerts,
                projectedOverage
            );

            return summary;
        }
        catch (Exception ex)
        {
            return DomainError.Infrastructure(
                "BUDGET_PROGRESS_QUERY_FAILED",
                "Failed to calculate budget progress",
                ex
            );
        }
    }
}