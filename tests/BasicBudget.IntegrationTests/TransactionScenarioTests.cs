using System.ComponentModel;
using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Npgsql;
using Respawn;
using Aspire.Hosting;
using TUnit.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BasicBudget.IntegrationTests;

[TUnit.Core.Category("Integration")]
public class TransactionScenarioTests : TUnit.Core.Interfaces.IAsyncInitializer, IAsyncDisposable
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    
    public async Task InitializeAsync()
    {
        // TUnit async initialization - runs once per test class
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BasicBudget_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        
        _httpClient = _app.CreateHttpClient("basicbudget-graphql");
        // Wait briefly for application to start
        await Task.Delay(2000);
    }

    [Before(HookType.Test)]
    public async Task ResetDatabase()
    {
        // TUnit per-test reset - runs before each test
        var connectionString = "Host=localhost;Port=5432;Database=basicbudget;Username=postgres;Password=postgres";
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var respawn = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
        await respawn.ResetAsync(connection);
    }

    [Test]
    [Description("Integration test for Scenario 2: Add Manual Transaction - MUST FAIL initially per TDD")]
    public async Task AddManualTransaction_WithValidInput_ReturnsTransactionAndUpdatesBalance()
    {
        // This test must fail initially (RED phase of TDD)
        // Arrange - First create an account to add transaction to
        var createAccountMutation = """
            mutation CreateAccount {
                createAccount(input: {
                    accountNumber: "12345678"
                    name: "Primary Checking"
                    accountType: CHECKING
                    initialBalance: { amount: 1000.00, currency: "USD" }
                }) {
                    account { id }
                }
            }
            """;

        var accountResponse = await _httpClient!.PostAsJsonAsync("/graphql", new { query = createAccountMutation });
        await Assert.That(accountResponse.IsSuccessStatusCode).IsTrue();
        
        // Extract account ID from response (this will fail initially)
        var accountContent = await accountResponse.Content.ReadAsStringAsync();
        // For now, use a test ID - this will be fixed when GraphQL is implemented
        var accountId = "test-account-id";

        var transactionMutation = """
            mutation AddTransaction($accountId: ID!) {
                createTransaction(input: {
                    accountId: $accountId
                    amount: { amount: -85.43, currency: "USD" }
                    description: "Kroger - Groceries"
                    transactionDate: "2024-09-06T14:30:00Z"
                }) {
                    transaction {
                        id
                        amount {
                            amount
                            currency
                            formatted
                        }
                        description
                        transactionDate
                        account {
                            id
                            currentBalance {
                                amount
                                formatted
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
            query = transactionMutation,
            variables = new { accountId = accountId }
        };

        // Act - This should fail initially as GraphQL endpoint doesn't exist yet
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - Verify transaction creation succeeded and balance updated
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createTransaction");
        await Assert.That(content).Contains("Kroger - Groceries");
        await Assert.That(content).Contains("-85.43");
        await Assert.That(content).Contains("914.57"); // Expected balance after transaction
        
        // Verify no errors in response
        await Assert.That(content).DoesNotContain("errors");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
}