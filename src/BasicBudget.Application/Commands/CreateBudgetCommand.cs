using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.ValueObjects;

using MediatR;

using OneOf;

namespace BasicBudget.Application.Commands;

public record CreateBudgetCommand(
    string Name,
    BudgetType BudgetType,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<BudgetCategoryAllocation> CategoryAllocations
) : IRequest<OneOf<Budget, DomainError>>;

public record BudgetCategoryAllocation(
    Guid CategoryId,
    Money AllocatedAmount,
    IReadOnlyList<AlertThreshold> AlertThresholds
);

public record AlertThreshold(
    decimal Percentage,
    AlertType AlertType
);

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, OneOf<Budget, DomainError>>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<OneOf<Budget, DomainError>> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        // Validate date range
        if (request.StartDate >= request.EndDate)
        {
            return new ValidationError(
                "Start date must be before end date",
                "INVALID_DATE_RANGE"
            );
        }

        // Validate period alignment based on budget type
        var periodValidation = ValidatePeriodAlignment(request.BudgetType, request.StartDate, request.EndDate);
        if (periodValidation.IsT1)
        {
            return periodValidation.AsT1;
        }

        // Check for overlapping budgets
        var overlappingBudgets = await _budgetRepository.FindOverlappingAsync(
            request.StartDate,
            request.EndDate,
            null, // excludeBudgetId - this is a new budget so no exclusion needed
            cancellationToken
        );

        if (overlappingBudgets.Any())
        {
            return new BudgetPeriodOverlapError(request.StartDate, request.EndDate);
        }

        // Validate all categories exist
        var categoryIds = request.CategoryAllocations.Select(ca => ca.CategoryId).ToList();
        var categories = await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);

        if (categories.Count() != categoryIds.Count)
        {
            var missingIds = categoryIds.Except(categories.Select(c => c.Id)).ToList();
            return new ValidationError(
                $"Categories not found: {string.Join(", ", missingIds)}",
                "CATEGORIES_NOT_FOUND"
            );
        }

        // Create the budget
        var budget = new Budget(
            request.Name,
            request.BudgetType,
            request.StartDate,
            request.EndDate
        );


        // Add category allocations
        foreach (var allocation in request.CategoryAllocations)
        {
            var category = categories.First(c => c.Id == allocation.CategoryId);

            var defaultAlertThreshold = allocation.AlertThresholds.Any()
                ? allocation.AlertThresholds.First().Percentage / 100m
                : 0.8m;

            var budgetCategory = new BudgetCategory(
                budget.Id,
                category.Id,
                allocation.AllocatedAmount,
                defaultAlertThreshold
            );

            // BudgetCategory created successfully - it will be persisted via repository
        }

        // Persist the budget
        try
        {
            await _budgetRepository.AddAsync(budget, cancellationToken);
            await _budgetRepository.SaveChangesAsync(cancellationToken);
            return budget;
        }
        catch (Exception ex)
        {
            return new InfrastructureError(
                "Failed to save budget to database",
                "BUDGET_SAVE_FAILED",
                ex
            );
        }
    }

    private static OneOf<bool, DomainError> ValidatePeriodAlignment(
        BudgetType budgetType,
        DateTime startDate,
        DateTime endDate)
    {
        return budgetType switch
        {
            BudgetType.Monthly => ValidateMonthlyPeriod(startDate, endDate),
            BudgetType.Yearly => ValidateYearlyPeriod(startDate, endDate),
            BudgetType.Custom => true, // Custom budgets can have any period
            _ => new ValidationError(
                $"Unsupported budget type: {budgetType}",
                "INVALID_BUDGET_TYPE"
            )
        };
    }

    private static OneOf<bool, DomainError> ValidateMonthlyPeriod(DateTime startDate, DateTime endDate)
    {
        var expectedEndDate = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));

        if (startDate.Day != 1 || endDate.Date != expectedEndDate.Date)
        {
            return new ValidationError(
                "Monthly budget must start on the 1st and end on the last day of the month",
                "INVALID_MONTHLY_PERIOD"
            );
        }

        return true;
    }

    private static OneOf<bool, DomainError> ValidateYearlyPeriod(DateTime startDate, DateTime endDate)
    {
        var expectedEndDate = new DateTime(startDate.Year, 12, 31);

        if (startDate.Month != 1 || startDate.Day != 1 || endDate.Date != expectedEndDate.Date)
        {
            return new ValidationError(
                "Yearly budget must start on January 1st and end on December 31st of the same year",
                "INVALID_YEARLY_PERIOD"
            );
        }

        return true;
    }
}