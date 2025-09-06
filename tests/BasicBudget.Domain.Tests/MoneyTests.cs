using BasicBudget.Domain.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace BasicBudget.Domain.Tests;

public class MoneyTests
{
    [Test]
    public async Task Create_WithValidAmountAndCurrency_ReturnsSuccess()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act  
        var result = Money.Create(amount, currency);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var money = result.AsT0;
        await Assert.That(money.Amount).IsEqualTo(amount);
        await Assert.That(money.Currency).IsEqualTo(currency);
    }

    [Test]
    public async Task Create_WithInvalidCurrency_ReturnsError()
    {
        // Arrange
        var amount = 100.50m;
        var invalidCurrency = "INVALID";

        // Act
        var result = Money.Create(amount, invalidCurrency);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("Invalid currency code");
    }

    [Test]
    public async Task Create_WithNegativeAmount_AllowsNegativeValues()
    {
        // Arrange
        var amount = -100.50m;
        var currency = "USD";

        // Act
        var result = Money.Create(amount, currency);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var money = result.AsT0;
        await Assert.That(money.Amount).IsEqualTo(amount);
        await Assert.That(money.IsNegative).IsTrue();
    }

    [Test]
    public async Task Add_SameCurrency_ReturnsCorrectSum()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD").AsT0;
        var money2 = Money.Create(50.25m, "USD").AsT0;

        // Act
        var result = money1.Add(money2);

        // Assert
        await Assert.That(result.Amount).IsEqualTo(150.75m);
        await Assert.That(result.Currency).IsEqualTo("USD");
    }

    [Test]
    public async Task Add_DifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD").AsT0;
        var money2 = Money.Create(50.25m, "EUR").AsT0;

        // Act & Assert
        await Assert.That(() => money1.Add(money2)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task IsZero_WithZeroAmount_ReturnsTrue()
    {
        // Arrange
        var money = Money.Create(0m, "USD").AsT0;

        // Act & Assert
        await Assert.That(money.IsZero).IsTrue();
    }

    [Test]
    public async Task IsPositive_WithPositiveAmount_ReturnsTrue()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").AsT0;

        // Act & Assert
        await Assert.That(money.IsPositive).IsTrue();
        await Assert.That(money.IsNegative).IsFalse();
    }

    [Test]
    public async Task Formatted_ForUSD_ReturnsCorrectFormat()
    {
        // Arrange
        var money = Money.Create(1234.56m, "USD").AsT0;

        // Act
        var formatted = money.Formatted;

        // Assert
        await Assert.That(formatted).IsEqualTo("$1,234.56");
    }
}