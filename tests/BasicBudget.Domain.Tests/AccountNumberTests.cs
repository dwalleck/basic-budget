using BasicBudget.Domain.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace BasicBudget.Domain.Tests;

public class AccountNumberTests
{
    [Test]
    public async Task Create_WithValidAccountNumber_ReturnsSuccess()
    {
        // Arrange
        var accountNumber = "1234567890";

        // Act
        var result = AccountNumber.Create(accountNumber);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var account = result.AsT0;
        await Assert.That(account.Value).IsEqualTo(accountNumber.ToUpperInvariant());
    }

    [Test]
    public async Task Create_WithTooShortAccountNumber_ReturnsError()
    {
        // Arrange
        var shortAccountNumber = "123";

        // Act
        var result = AccountNumber.Create(shortAccountNumber);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("length");
    }

    [Test]
    public async Task Create_WithTooLongAccountNumber_ReturnsError()
    {
        // Arrange
        var longAccountNumber = "123456789012345678901";

        // Act
        var result = AccountNumber.Create(longAccountNumber);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("length");
    }

    [Test]
    public async Task Create_WithSpecialCharacters_ReturnsError()
    {
        // Arrange
        var invalidAccountNumber = "1234-5678@90";

        // Act
        var result = AccountNumber.Create(invalidAccountNumber);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
    }

    [Test]
    public async Task MaskedValue_WithShortNumber_ShowsAll()
    {
        // Arrange
        var accountNumber = "1234";
        var account = AccountNumber.Create(accountNumber).AsT0;

        // Act
        var masked = account.MaskedValue;

        // Assert
        await Assert.That(masked).IsEqualTo("1234");
    }

    [Test]
    public async Task MaskedValue_WithMediumNumber_ShowsLastFour()
    {
        // Arrange
        var accountNumber = "12345678";
        var account = AccountNumber.Create(accountNumber).AsT0;

        // Act
        var masked = account.MaskedValue;

        // Assert
        await Assert.That(masked).IsEqualTo("****5678");
    }

    [Test]
    public async Task MaskedValue_WithLongNumber_ShowsFirstAndLastFour()
    {
        // Arrange
        var accountNumber = "123456789012";
        var account = AccountNumber.Create(accountNumber).AsT0;

        // Act
        var masked = account.MaskedValue;

        // Assert
        await Assert.That(masked).IsEqualTo("1234****9012");
    }

    [Test]
    public async Task Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var account1 = AccountNumber.Create("1234567890").AsT0;
        var account2 = AccountNumber.Create("1234567890").AsT0;

        // Act & Assert
        await Assert.That(account1.Equals(account2)).IsTrue();
    }

    [Test]
    public async Task GetHashCode_WithSameValue_ReturnsSameHash()
    {
        // Arrange
        var account1 = AccountNumber.Create("1234567890").AsT0;
        var account2 = AccountNumber.Create("1234567890").AsT0;

        // Act & Assert
        await Assert.That(account1.GetHashCode()).IsEqualTo(account2.GetHashCode());
    }
}