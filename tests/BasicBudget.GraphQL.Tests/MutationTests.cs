using System.ComponentModel;
using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Aspire.Hosting;

namespace BasicBudget.GraphQL.Tests;

[TUnit.Core.Category("Contract")]
public class MutationTests
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    [Before(HookType.Test)]
    public async Task SetupAsync()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        _app = appBuilder.Build();
        
        await _app.StartAsync();
        
        var resourceName = "basicbudget-graphql";
        var httpEndpoint = _app.GetEndpoint(resourceName, "https");
        
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = httpEndpoint;
    }

    [After(HookType.Test)]
    public async Task CleanupAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Test]
    [Description("Contract test for Mutation.createAccount - MUST FAIL initially per TDD")]
    public async Task Mutation_CreateAccount_ReturnsCreatedAccount()
    {
        // Arrange
        var mutation = """
            mutation CreateAccount($input: CreateAccountInput!) {
                createAccount(input: $input) {
                    account {
                        id
                        name
                        accountNumber
                        accountType
                        currentBalance {
                            amount
                            currency
                        }
                        createdAt
                    }
                    errors {
                        ... on ValidationError {
                            message
                            field
                        }
                        ... on BusinessRuleError {
                            message
                            code
                        }
                    }
                }
            }
            """;

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    name = "Test Checking Account",
                    accountNumber = "123456789",
                    accountType = "CHECKING",
                    initialBalance = new { amount = 1000.00m, currency = "USD" }
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createAccount");
    }

    [Test]
    [Description("Contract test for Mutation.createTransaction - MUST FAIL initially per TDD")]
    public async Task Mutation_CreateTransaction_ReturnsCreatedTransaction()
    {
        // Arrange
        var mutation = """
            mutation CreateTransaction($input: CreateTransactionInput!) {
                createTransaction(input: $input) {
                    transaction {
                        id
                        amount {
                            amount
                            currency
                        }
                        description
                        transactionDate
                        account {
                            id
                            name
                        }
                        category {
                            id
                            name
                        }
                    }
                    errors {
                        ... on ValidationError {
                            message
                            field
                        }
                        ... on BusinessRuleError {
                            message
                            code
                        }
                    }
                }
            }
            """;

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    accountId = "acc-123",
                    amount = new { amount = -50.00m, currency = "USD" },
                    description = "Grocery store purchase",
                    transactionDate = "2024-09-06T10:00:00Z",
                    categoryId = "cat-groceries"
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createTransaction");
    }

    [Test]
    [Description("Contract test for Mutation.importStatement - MUST FAIL initially per TDD")]
    public async Task Mutation_ImportStatement_ReturnsImportedTransactions()
    {
        // Arrange
        var mutation = """
            mutation ImportStatement($input: ImportStatementInput!) {
                importStatement(input: $input) {
                    transactions {
                        id
                        amount {
                            amount
                            currency
                        }
                        description
                        transactionDate
                        account {
                            id
                            name
                        }
                    }
                    importSummary {
                        totalTransactions
                        successfulImports
                        failedImports
                        duplicatesSkipped
                    }
                    errors {
                        ... on ValidationError {
                            message
                            field
                        }
                        ... on BusinessRuleError {
                            message
                            code
                        }
                    }
                }
            }
            """;

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    accountId = "acc-123",
                    fileFormat = "CSV",
                    fileContent = "Date,Description,Amount\n2024-09-01,Test Transaction,-25.00",
                    skipDuplicates = true
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("importStatement");
        await Assert.That(content).Contains("importSummary");
    }

    [Test]
    [Description("Contract test for Mutation.createBudget - MUST FAIL initially per TDD")]
    public async Task Mutation_CreateBudget_ReturnsCreatedBudget()
    {
        // Arrange
        var mutation = """
            mutation CreateBudget($input: CreateBudgetInput!) {
                createBudget(input: $input) {
                    budget {
                        id
                        name
                        budgetType
                        totalAllocated {
                            amount
                            currency
                        }
                        period
                        startDate
                        endDate
                        isActive
                        categories {
                            id
                            category {
                                id
                                name
                            }
                            allocatedAmount {
                                amount
                                currency
                            }
                        }
                    }
                    errors {
                        ... on ValidationError {
                            message
                            field
                        }
                        ... on BusinessRuleError {
                            message
                            code
                        }
                    }
                }
            }
            """;

        var request = new
        {
            query = mutation,
            variables = new
            {
                input = new
                {
                    name = "Monthly Budget September 2024",
                    budgetType = "MONTHLY",
                    startDate = "2024-09-01T00:00:00Z",
                    endDate = "2024-09-30T23:59:59Z",
                    categories = new[]
                    {
                        new { categoryId = "cat-groceries", allocatedAmount = new { amount = 500.00m, currency = "USD" } },
                        new { categoryId = "cat-utilities", allocatedAmount = new { amount = 200.00m, currency = "USD" } }
                    }
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createBudget");
    }
}