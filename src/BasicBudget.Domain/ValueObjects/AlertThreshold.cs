using BasicBudget.Domain.Errors;

using OneOf;

namespace BasicBudget.Domain.ValueObjects;

public enum AlertType
{
    WARNING,
    CRITICAL,
    EXCEEDED
}

public record AlertThreshold
{
    public decimal Percentage { get; init; }
    public AlertType AlertType { get; init; }

    private AlertThreshold(decimal percentage, AlertType alertType)
    {
        Percentage = percentage;
        AlertType = alertType;
    }

    public static OneOf<AlertThreshold, DomainError> Create(decimal percentage, AlertType alertType)
    {
        if (percentage < 0 || percentage > 100)
        {
            return new InvalidAlertThresholdError(percentage);
        }

        return new AlertThreshold(percentage, alertType);
    }

    public bool IsTriggered(decimal currentPercentage)
    {
        return currentPercentage >= Percentage;
    }

    public override string ToString()
    {
        return $"{AlertType} at {Percentage}%";
    }
}