using BasicBudget.Domain.Entities;
using BasicBudget.Domain.ValueObjects;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace BasicBudget.Domain.Tests;

public class TransactionTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessfulTransaction()
    {
        // Arrange
        var account = CreateValidAccount();
        var amount = Money.Create(100.50m, "USD").AsT0;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow.AddDays(-1); // Yesterday to avoid future date issues

        // Act
        var result = Transaction.Create(account, amount, description, transactionDate);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var transaction = result.AsT0;
        await Assert.That(transaction.AccountId).IsEqualTo(account.Id);
        await Assert.That(transaction.Amount).IsEqualTo(amount);
        await Assert.That(transaction.Description).IsEqualTo(description);
        await Assert.That(transaction.TransactionDate).IsEqualTo(transactionDate);
        await Assert.That(transaction.IsReconciled).IsFalse();
    }

    [Test]
    public async Task Create_WithEmptyDescription_ReturnsError()
    {
        // Arrange
        var account = CreateValidAccount();
        var amount = Money.Create(100.50m, "USD").AsT0;
        var description = "";
        var transactionDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = Transaction.Create(account, amount, description, transactionDate);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("description");
    }

    [Test]
    public async Task Create_WithZeroAmount_ReturnsError()
    {
        // Arrange
        var account = CreateValidAccount();
        var amount = Money.Create(0m, "USD").AsT0;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = Transaction.Create(account, amount, description, transactionDate);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("amount");
    }

    [Test]
    public async Task Create_WithFutureDate_ReturnsError()
    {
        // Arrange
        var account = CreateValidAccount();
        var amount = Money.Create(100.50m, "USD").AsT0;
        var description = "Test transaction";
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = Transaction.Create(account, amount, description, futureDate);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("future");
    }

    [Test]
    public async Task Categorize_WithValidCategory_AssignsCorrectly()
    {
        // Arrange
        var transaction = CreateValidTransaction();
        var categoryId = Guid.NewGuid();

        // Act
        transaction.Categorize(categoryId);

        // Assert
        await Assert.That(transaction.CategoryId).IsEqualTo(categoryId);
    }

    [Test]
    public async Task MarkAsReconciled_SetsReconciledFlag()
    {
        // Arrange
        var transaction = CreateValidTransaction();
        await Assert.That(transaction.IsReconciled).IsFalse();

        // Act
        transaction.MarkAsReconciled();

        // Assert
        await Assert.That(transaction.IsReconciled).IsTrue();
    }

    [Test]
    public async Task UpdateDescription_WithValidDescription_UpdatesCorrectly()
    {
        // Arrange
        var transaction = CreateValidTransaction();
        var newDescription = "Updated description";

        // Act
        transaction.UpdateDescription(newDescription);

        // Assert
        await Assert.That(transaction.Description).IsEqualTo(newDescription);
    }

    [Test]
    public async Task UpdateDescription_WithEmptyDescription_ThrowsException()
    {
        // Arrange
        var transaction = CreateValidTransaction();

        // Act & Assert
        await Assert.That(() => transaction.UpdateDescription("")).Throws<ArgumentException>();
    }

    private Account CreateValidAccount()
    {
        var accountNumber = AccountNumber.Create("1234567890").AsT0;
        var name = "Test Account";
        var accountType = AccountType.Checking;
        var initialBalance = Money.Create(1000.00m, "USD").AsT0;

        return Account.Create(accountNumber, name, accountType, initialBalance).AsT0;
    }

    private Transaction CreateValidTransaction()
    {
        var account = CreateValidAccount();
        var amount = Money.Create(100.50m, "USD").AsT0;
        var description = "Test transaction";
        var transactionDate = DateTime.UtcNow.AddDays(-1); // Yesterday to avoid future date issues

        return Transaction.Create(account, amount, description, transactionDate).AsT0;
    }
}