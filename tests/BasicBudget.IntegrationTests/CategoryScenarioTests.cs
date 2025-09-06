using System.ComponentModel;
using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Npgsql;
using Respawn;
using TUnit.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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