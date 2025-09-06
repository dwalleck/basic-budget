using System.ComponentModel;
using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Npgsql;
using Respawn;
using Microsoft.Extensions.DependencyInjection;

namespace BasicBudget.IntegrationTests;

[TUnit.Core.Category("Integration")]
public class BudgetScenarioTests : TUnit.Core.Interfaces.IAsyncInitializer, IAsyncDisposable
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
    [Description("Integration test for Scenario 4: Create Monthly Budget - MUST FAIL initially per TDD")]
    public async Task CreateMonthlyBudget_WithCategoryAllocations_ReturnsBudgetWithProgress()
    {
        // This test must fail initially (RED phase of TDD)
        // Arrange - Create categories first (will fail initially but shows the intent)
        var createCategoryMutation = """
            mutation CreateCategory {
                createCategory(input: {
                    name: "Groceries"
                    description: "Grocery store purchases"
                    categoryType: EXPENSE
                    color: "#8BC34A"
                }) {
                    category { id }
                }
            }
            """;

        var categoryResponse = await _httpClient!.PostAsJsonAsync("/graphql", new { query = createCategoryMutation });
        await Assert.That(categoryResponse.IsSuccessStatusCode).IsTrue();
        
        // For now, use test IDs - this will be fixed when GraphQL is implemented
        var groceryCategoryId = "test-grocery-category-id";

        var createBudgetMutation = """
            mutation CreateMonthlyBudget($groceryCategoryId: ID!) {
                createBudget(input: {
                    name: "September 2024 Budget"
                    budgetType: MONTHLY
                    startDate: "2024-09-01T00:00:00Z"
                    endDate: "2024-09-30T23:59:59Z"
                    categories: [
                        {
                            categoryId: $groceryCategoryId
                            allocatedAmount: { amount: 400.00, currency: "USD" }
                            alertThresholds: [
                                { percentage: 75, alertType: WARNING }
                                { percentage: 90, alertType: CRITICAL }
                                { percentage: 100, alertType: EXCEEDED }
                            ]
                        }
                    ]
                }) {
                    budget {
                        id
                        name
                        budgetType
                        startDate
                        endDate
                        isActive
                        totalAllocated {
                            amount
                            currency
                            formatted
                        }
                        categories {
                            id
                            category {
                                id
                                name
                            }
                            allocatedAmount {
                                amount
                                currency
                                formatted
                            }
                            spentAmount {
                                amount
                                currency
                                formatted
                            }
                            remainingAmount {
                                amount
                                currency
                                formatted
                            }
                            percentageUsed
                            alertThresholds {
                                percentage
                                alertType
                                isTriggered
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
            query = createBudgetMutation,
            variables = new { groceryCategoryId = groceryCategoryId }
        };

        // Act - This should fail initially as GraphQL endpoint doesn't exist yet
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - Verify budget creation succeeded with proper allocations
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createBudget");
        await Assert.That(content).Contains("September 2024 Budget");
        await Assert.That(content).Contains("MONTHLY");
        await Assert.That(content).Contains("400.00"); // Grocery allocation
        await Assert.That(content).Contains("Groceries");
        
        // Verify alert thresholds are set
        await Assert.That(content).Contains("alertThresholds");
        await Assert.That(content).Contains("75"); // 75% warning threshold
        await Assert.That(content).Contains("90"); // 90% critical threshold
        
        // Verify no errors in response
        await Assert.That(content).DoesNotContain("errors");

        // Verify budget is active by default
        await Assert.That(content).Contains("\"isActive\": true");
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