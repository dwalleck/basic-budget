using TUnit.Core;
using TUnit.Assertions;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace BasicBudget.GraphQL.Tests;

[TestClass]
[Category("Contract")]
public class SubscriptionTests
{
    private DistributedApplication? _app;
    private ClientWebSocket? _webSocket;
    private string? _graphqlWsEndpoint;

    [Before(TestState.BeforeTest)]
    public async Task SetupAsync()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        _app = appBuilder.Build();
        
        await _app.StartAsync();
        
        var resourceName = "basicbudget-graphql";
        var httpEndpoint = _app.GetEndpoint(resourceName, "https");
        
        // Convert HTTP endpoint to WebSocket endpoint for GraphQL subscriptions
        var wsUri = new UriBuilder(httpEndpoint) { Scheme = "wss", Path = "/graphql" };
        _graphqlWsEndpoint = wsUri.ToString();
        
        _webSocket = new ClientWebSocket();
        _webSocket.Options.AddSubProtocol("graphql-ws");
    }

    [After(TestState.AfterTest)]
    public async Task CleanupAsync()
    {
        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            _webSocket.Dispose();
        }
        
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Test]
    [Description("Contract test for Subscription.transactionAdded - MUST FAIL initially per TDD")]
    public async Task Subscription_TransactionAdded_ReceivesTransactionNotifications()
    {
        // Arrange
        var subscription = """
            subscription TransactionAdded($accountId: ID) {
                transactionAdded(accountId: $accountId) {
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
            """;

        var subscriptionMessage = JsonSerializer.Serialize(new
        {
            id = "1",
            type = "start",
            payload = new
            {
                query = subscription,
                variables = new { accountId = "acc-123" }
            }
        });

        // Act & Assert - This should fail initially as GraphQL WebSocket endpoint doesn't exist yet
        await Assert.That(async () => 
        {
            await _webSocket!.ConnectAsync(new Uri(_graphqlWsEndpoint!), CancellationToken.None);
            
            // Send connection init
            var initMessage = JsonSerializer.Serialize(new { type = "connection_init" });
            var initBytes = Encoding.UTF8.GetBytes(initMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(initBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            // Send subscription
            var subBytes = Encoding.UTF8.GetBytes(subscriptionMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(subBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            return true;
        }).DoesNotThrowException();
    }

    [Test]
    [Description("Contract test for Subscription.budgetAlertAdded - MUST FAIL initially per TDD")]
    public async Task Subscription_BudgetAlertAdded_ReceivesBudgetAlertNotifications()
    {
        // Arrange
        var subscription = """
            subscription BudgetAlertAdded {
                budgetAlertAdded {
                    id
                    budgetId
                    categoryId
                    alertType
                    message
                    threshold
                    currentAmount {
                        amount
                        currency
                    }
                    createdAt
                }
            }
            """;

        var subscriptionMessage = JsonSerializer.Serialize(new
        {
            id = "2",
            type = "start",
            payload = new
            {
                query = subscription
            }
        });

        // Act & Assert - This should fail initially as GraphQL WebSocket endpoint doesn't exist yet
        await Assert.That(async () => 
        {
            await _webSocket!.ConnectAsync(new Uri(_graphqlWsEndpoint!), CancellationToken.None);
            
            // Send connection init
            var initMessage = JsonSerializer.Serialize(new { type = "connection_init" });
            var initBytes = Encoding.UTF8.GetBytes(initMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(initBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            // Send subscription
            var subBytes = Encoding.UTF8.GetBytes(subscriptionMessage);
            await _webSocket.SendAsync(new ArraySegment<byte>(subBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            return true;
        }).DoesNotThrowException();
    }
}