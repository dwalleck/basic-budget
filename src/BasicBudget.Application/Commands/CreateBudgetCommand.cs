using MediatR;
using OneOf;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;
using BasicBudget.Domain.Repositories;
using BasicBudget.Domain.Errors;

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
            return DomainError.Validation(
                "INVALID_DATE_RANGE",
                "Start date must be before end date",
                nameof(request.StartDate)
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
            cancellationToken
        );

        if (overlappingBudgets.Any())
        {
            return DomainError.BusinessRule(
                "OVERLAPPING_BUDGET_PERIODS",
                $"Budget period overlaps with existing budget: {string.Join(", ", overlappingBudgets.Select(b => b.Name))}"
            );
        }

        // Validate all categories exist
        var categoryIds = request.CategoryAllocations.Select(ca => ca.CategoryId).ToList();
        var categories = await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);
        
        if (categories.Count != categoryIds.Count)
        {
            var missingIds = categoryIds.Except(categories.Select(c => c.Id)).ToList();
            return DomainError.NotFound(
                "CATEGORIES_NOT_FOUND",
                $"Categories not found: {string.Join(", ", missingIds)}"
            );
        }

        // Create the budget using domain entity factory method
        var budgetResult = Budget.Create(
            request.Name,
            request.BudgetType,
            request.StartDate,
            request.EndDate
        );

        if (budgetResult.IsT1)
        {
            return budgetResult.AsT1;
        }

        var budget = budgetResult.AsT0;

        // Add category allocations
        foreach (var allocation in request.CategoryAllocations)
        {
            var category = categories.First(c => c.Id == allocation.CategoryId);
            
            var budgetCategoryResult = BudgetCategory.Create(
                budget,
                category,
                allocation.AllocatedAmount,
                allocation.AlertThresholds.Select(at => new Domain.ValueObjects.AlertThreshold(
                    at.Percentage,
                    at.AlertType
                )).ToList()
            );

            if (budgetCategoryResult.IsT1)
            {
                return budgetCategoryResult.AsT1;
            }

            var addCategoryResult = budget.AddCategory(budgetCategoryResult.AsT0);
            if (addCategoryResult.IsT1)
            {
                return addCategoryResult.AsT1;
            }
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
            return DomainError.Infrastructure(
                "BUDGET_SAVE_FAILED",
                "Failed to save budget to database",
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
            BudgetType.Custom => new OneOf<bool, DomainError>(true), // Custom budgets can have any period
            _ => DomainError.Validation(
                "INVALID_BUDGET_TYPE",
                $"Unsupported budget type: {budgetType}",
                nameof(budgetType)
            )
        };
    }

    private static OneOf<bool, DomainError> ValidateMonthlyPeriod(DateTime startDate, DateTime endDate)
    {
        var expectedEndDate = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));
        
        if (startDate.Day != 1 || endDate.Date != expectedEndDate.Date)
        {
            return DomainError.Validation(
                "INVALID_MONTHLY_PERIOD",
                "Monthly budget must start on the 1st and end on the last day of the month",
                "StartDate/EndDate"
            );
        }

        return true;
    }

    private static OneOf<bool, DomainError> ValidateYearlyPeriod(DateTime startDate, DateTime endDate)
    {
        var expectedEndDate = new DateTime(startDate.Year, 12, 31);
        
        if (startDate.Month != 1 || startDate.Day != 1 || endDate.Date != expectedEndDate.Date)
        {
            return DomainError.Validation(
                "INVALID_YEARLY_PERIOD",
                "Yearly budget must start on January 1st and end on December 31st of the same year",
                "StartDate/EndDate"
            );
        }

        return true;
    }
}