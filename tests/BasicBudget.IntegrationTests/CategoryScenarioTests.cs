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
using TUnit.Core.Interfaces;

namespace BasicBudget.IntegrationTests;

[TUnit.Core.Category("Integration")]
public class CategoryScenarioTests : TUnit.Core.Interfaces.IAsyncInitializer, IAsyncDisposable
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
    [Description("Integration test for Scenario 3: Create Spending Categories - MUST FAIL initially per TDD")]
    public async Task CreateSpendingCategories_WithHierarchy_ReturnsCategoryStructure()
    {
        // This test must fail initially (RED phase of TDD)
        // Arrange - Create parent category first
        var createParentCategoryMutation = """
            mutation CreateParentCategory {
                createCategory(input: {
                    name: "Food"
                    description: "All food-related expenses"
                    categoryType: EXPENSE
                    color: "#4CAF50"
                }) {
                    category {
                        id
                        name
                        description
                        categoryType
                        color
                        parentCategory {
                            id
                        }
                        subCategories {
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

        var parentResponse = await _httpClient!.PostAsJsonAsync("/graphql", new { query = createParentCategoryMutation });
        await Assert.That(parentResponse.IsSuccessStatusCode).IsTrue();

        // Extract parent category ID from response (this will fail initially)
        var parentContent = await parentResponse.Content.ReadAsStringAsync();
        // For now, use a test ID - this will be fixed when GraphQL is implemented
        var parentCategoryId = "test-parent-category-id";

        var createSubCategoryMutation = """
            mutation CreateSubCategory($parentId: ID!) {
                createCategory(input: {
                    name: "Groceries"
                    description: "Grocery store purchases"
                    categoryType: EXPENSE
                    color: "#8BC34A"
                    parentCategoryId: $parentId
                }) {
                    category {
                        id
                        name
                        description
                        categoryType
                        color
                        parentCategory {
                            id
                            name
                        }
                        subCategories {
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
            query = createSubCategoryMutation,
            variables = new { parentId = parentCategoryId }
        };

        // Act - This should fail initially as GraphQL endpoint doesn't exist yet
        var response = await _httpClient!.PostAsJsonAsync("/graphql", request);

        // Assert - Verify category hierarchy creation succeeded
        await Assert.That(response.IsSuccessStatusCode).IsTrue();

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("createCategory");
        await Assert.That(content).Contains("Groceries");
        await Assert.That(content).Contains("Grocery store purchases");
        await Assert.That(content).Contains("Food"); // Parent category name

        // Verify no errors in response
        await Assert.That(content).DoesNotContain("errors");

        // Verify hierarchical relationship exists
        await Assert.That(content).Contains("parentCategory");
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