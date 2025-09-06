using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace BasicBudget.Domain.Tests;

public class AccountTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessfulAccount()
    {
        // Arrange
        var accountNumber = AccountNumber.Create("1234567890").AsT0;
        var name = "My Checking Account";
        var accountType = AccountType.Checking;
        var initialBalance = Money.Create(1000.00m, "USD").AsT0;

        // Act
        var result = Account.Create(accountNumber, name, accountType, initialBalance);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var account = result.AsT0;
        await Assert.That(account.AccountNumber).IsEqualTo(accountNumber);
        await Assert.That(account.Name).IsEqualTo(name);
        await Assert.That(account.AccountType).IsEqualTo(accountType);
        await Assert.That(account.CurrentBalance).IsEqualTo(initialBalance);
    }

    [Test]
    public async Task Create_WithEmptyName_ReturnsError()
    {
        // Arrange
        var accountNumber = AccountNumber.Create("1234567890").AsT0;
        var name = "";
        var accountType = AccountType.Checking;
        var initialBalance = Money.Create(1000.00m, "USD").AsT0;

        // Act
        var result = Account.Create(accountNumber, name, accountType, initialBalance);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("name");
    }

    [Test]
    public async Task UpdateBalance_WithValidAmount_UpdatesBalance()
    {
        // Arrange
        var account = CreateValidAccount();
        var newBalance = Money.Create(1500.00m, "USD").AsT0;

        // Act
        account.UpdateBalance(newBalance);

        // Assert
        await Assert.That(account.CurrentBalance).IsEqualTo(newBalance);
    }

    [Test]
    public async Task UpdateBalance_WithDifferentCurrency_ReturnsDomainError()
    {
        // Arrange
        var account = CreateValidAccount();
        var differentCurrencyBalance = Money.Create(1500.00m, "EUR").AsT0;

        // Act
        var result = account.UpdateBalance(differentCurrencyBalance);
        
        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.GetType()).IsEqualTo(typeof(BasicBudget.Domain.Errors.ValidationError));
        await Assert.That(error.Code).IsEqualTo("CURRENCY_MISMATCH");
    }

    private Account CreateValidAccount()
    {
        var accountNumber = AccountNumber.Create("1234567890").AsT0;
        var name = "Test Account";
        var accountType = AccountType.Checking;
        var initialBalance = Money.Create(1000.00m, "USD").AsT0;

        return Account.Create(accountNumber, name, accountType, initialBalance).AsT0;
    }
}