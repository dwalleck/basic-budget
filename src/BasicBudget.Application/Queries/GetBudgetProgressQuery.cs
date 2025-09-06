using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Services;
using BasicBudget.Domain.ValueObjects;

using MediatR;

using OneOf;

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
                return new BudgetNotFoundError(request.BudgetId);
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

            foreach (var budgetCategory in budget.BudgetCategories)
            {
                var spentAmount = spendingByCategory.TryGetValue(budgetCategory.CategoryId, out var spent)
                    ? Money.Create(spent, budgetCategory.AllocatedAmount.Currency).AsT0
                    : Money.Zero(budgetCategory.AllocatedAmount.Currency);

                var remainingAmount = budgetCategory.AllocatedAmount - spentAmount;
                var percentageUsed = budgetCategory.AllocatedAmount.Amount == 0
                    ? 0
                    : (spentAmount.Amount / budgetCategory.AllocatedAmount.Amount) * 100;

                // Check for triggered alerts
                var triggeredAlerts = new List<AlertThreshold>();
                if (budgetCategory.ShouldAlert())
                {
                    var alertThresholdValueObject = AlertThreshold.Create(
                        budgetCategory.AlertThreshold * 100,
                        AlertType.WARNING
                    );
                    if (alertThresholdValueObject.IsT0)
                    {
                        triggeredAlerts.Add(alertThresholdValueObject.AsT0);
                    }
                }

                if (triggeredAlerts.Any())
                {
                    foreach (var threshold in triggeredAlerts)
                    {
                        activeAlerts.Add(new BudgetAlert(
                            budgetCategory.Id,
                            budgetCategory.Category.Name,
                            AlertType.WARNING,
                            budgetCategory.AlertThreshold * 100,
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
                    triggeredAlerts.AsReadOnly()
                ));
            }

            // Calculate overall totals
            var totalAllocated = Money.Create(
                budget.BudgetCategories.Sum(c => c.AllocatedAmount.Amount),
                budget.BudgetCategories.First().AllocatedAmount.Currency
            ).AsT0;

            var totalSpent = Money.Create(
                categoryProgressList.Sum(cp => cp.SpentAmount.Amount),
                totalAllocated.Currency
            ).AsT0;

            var totalRemaining = totalAllocated - totalSpent;
            var overallPercentageUsed = totalAllocated.Amount == 0
                ? 0
                : (totalSpent.Amount / totalAllocated.Amount) * 100;

            // Calculate projected overage if applicable
            var domainProjectedOverage = await _budgetCalculationService.CalculateProjectedOverageAsync(
                budget,
                transactions.ToList().AsReadOnly(),
                cancellationToken
            );

            // Convert domain ProjectedOverage to application ProjectedOverage
            var projectedOverage = domainProjectedOverage != null
                ? new ProjectedOverage(
                    domainProjectedOverage.ProjectedOverageAmount,
                    domainProjectedOverage.ProjectedDate,
                    domainProjectedOverage.ContributingCategoryIds
                )
                : null;

            var summary = new BudgetProgressSummary(
                budget,
                totalAllocated,
                totalSpent,
                totalRemaining,
                overallPercentageUsed,
                categoryProgressList.AsReadOnly(),
                activeAlerts.AsReadOnly(),
                projectedOverage
            );

            return summary;
        }
        catch (Exception ex)
        {
            return new InfrastructureError(
                "Failed to calculate budget progress",
                "BUDGET_PROGRESS_QUERY_FAILED",
                ex
            );
        }
    }
}