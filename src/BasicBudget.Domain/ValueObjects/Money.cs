using System.Globalization;

using OneOf;

namespace BasicBudget.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static OneOf<Money, MoneyError> Create(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
            return new MoneyError("Currency code cannot be empty");

        var normalizedCurrency = currency.ToUpperInvariant();

        if (!IsValidCurrencyCode(normalizedCurrency))
            return new MoneyError($"Invalid currency code: {currency}");

        if (decimal.Round(amount, 4) != amount)
            return new MoneyError("Amount precision cannot exceed 4 decimal places");

        return new Money(amount, normalizedCurrency);
    }

    public string Formatted => Amount.ToString("C", GetCultureInfo());

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        return new Money(decimal.Round(Amount * multiplier, 4), Currency);
    }

    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;

    private static bool IsValidCurrencyCode(string currency)
    {
        var validCurrencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "CHF", "CNY", "INR" };
        return validCurrencies.Contains(currency);
    }

    private CultureInfo GetCultureInfo()
    {
        return Currency switch
        {
            "USD" => new CultureInfo("en-US"),
            "EUR" => new CultureInfo("de-DE"),
            "GBP" => new CultureInfo("en-GB"),
            "CAD" => new CultureInfo("en-CA"),
            "AUD" => new CultureInfo("en-AU"),
            "JPY" => new CultureInfo("ja-JP"),
            _ => CultureInfo.InvariantCulture
        };
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public static implicit operator decimal(Money money) => money.Amount;

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal multiplier) => money.Multiply(multiplier);
    public static Money operator *(decimal multiplier, Money money) => money.Multiply(multiplier);
}

public record MoneyError(string Message);