using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace BasicBudget.GraphQL.Tests;

[TestClass]
[Category("Contract")]
public class QueryTests
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    [Before(TestState.BeforeTest)]
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

    [After(TestState.AfterTest)]
    public async Task CleanupAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Test]
    [Description("Contract test for Query.accounts operation - MUST FAIL initially per TDD")]
    public async Task Query_Accounts_ReturnsAccountsArray()
    {
        // Arrange
        var query = """
            query GetAccounts {
                accounts {
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
            }
            """;

        var request = new
        {
            query = query
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsNotNull();
        await Assert.That(content).Contains("accounts");
    }

    [Test]
    [Description("Contract test for Query.account(id) operation - MUST FAIL initially per TDD")]
    public async Task Query_Account_ById_ReturnsSpecificAccount()
    {
        // Arrange
        var testAccountId = "acc-123";
        var query = $$"""
            query GetAccount($id: ID!) {
                account(id: $id) {
                    id
                    name
                    accountNumber
                    accountType
                    currentBalance {
                        amount
                        currency
                    }
                }
            }
            """;

        var request = new
        {
            query = query,
            variables = new { id = testAccountId }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("account");
    }

    [Test]
    [Description("Contract test for Query.transactions with filtering - MUST FAIL initially per TDD")]
    public async Task Query_Transactions_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var query = """
            query GetTransactions($accountId: ID, $startDate: DateTime, $first: Int) {
                transactions(accountId: $accountId, startDate: $startDate, first: $first) {
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                    nodes {
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
                }
            }
            """;

        var request = new
        {
            query = query,
            variables = new 
            { 
                accountId = "acc-123",
                startDate = "2024-01-01T00:00:00Z",
                first = 10
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("transactions");
        await Assert.That(content).Contains("pageInfo");
        await Assert.That(content).Contains("nodes");
    }

    [Test]
    [Description("Contract test for Query.budgets operation - MUST FAIL initially per TDD")]
    public async Task Query_Budgets_ReturnsBudgetsArray()
    {
        // Arrange
        var query = """
            query GetBudgets {
                budgets {
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
                        spentAmount {
                            amount
                            currency
                        }
                    }
                }
            }
            """;

        var request = new { query = query };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - This should fail initially as GraphQL endpoint doesn't exist yet
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("budgets");
    }
}