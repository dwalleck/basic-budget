using System.ComponentModel;
using System.Net.Http.Json;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Npgsql;

using Respawn;

using TUnit.Assertions;
using TUnit.Core;

namespace BasicBudget.IntegrationTests;

[TUnit.Core.Category("Integration")]
public class AccountScenarioTests : TUnit.Core.Interfaces.IAsyncInitializer, IAsyncDisposable
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public async Task InitializeAsync()
    {
        // TUnit async initialization - runs once per test class
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BasicBudget_AppHost>();
        
        // Configure HTTP client resilience handler for better reliability
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Use the correct resource name from AppHost ("graphql-api")
        _httpClient = _app.CreateHttpClient("graphql-api");
        
        // Wait for resources to be healthy before proceeding
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("graphql-api", cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("postgresdb", cts.Token);
    }

    [Before(HookType.Test)]
    public async Task ResetDatabase()
    {
        // TUnit per-test reset - runs before each test
        // Get the connection string from Aspire's managed resources
        var connectionString = await _app!.GetConnectionStringAsync("postgresdb");
        
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
    [Description("Integration test for Scenario 1: Create First Account - MUST FAIL initially per TDD")]
    public async Task CreateAccount_WithValidInput_ReturnsAccount()
    {
        // This test must fail initially (RED phase of TDD)
        // Arrange
        var mutation = """
            mutation CreateCheckingAccount {
              createAccount(input: {
                accountNumber: "12345678"
                name: "Primary Checking"
                accountType: CHECKING
                initialBalance: { amount: 1000.00, currency: "USD" }
              }) {
                account { 
                    id
                    name
                    accountNumber
                    accountType
                    currentBalance { 
                        amount
                        currency
                        formatted 
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

        var request = new { query = mutation };

        // Act - This should fail initially as GraphQL endpoint doesn't exist yet
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - Verify account creation succeeded
        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createAccount");
        await Assert.That(content).Contains("Primary Checking");
        await Assert.That(content).Contains("12345678");
        await Assert.That(content).Contains("1000.00");

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

// Custom parallel limiter to ensure single database access across test suite
public class DatabaseTestLimit : TUnit.Core.Interfaces.IParallelLimit
{
    public int Limit => 1; // Only allow one database test at a time
}