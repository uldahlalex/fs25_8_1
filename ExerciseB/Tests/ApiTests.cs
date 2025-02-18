using System.Text.Json;
using Api;
using Api.EventHandlers;
using Api.EventHandlers.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSocketBoilerplate;

namespace Tests;

public class ApiTests(ITestOutputHelper outputHelper) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddXUnit(outputHelper);
        });
    }

    [Fact]
    public async Task Api_Can_Successfully_Add_Connection()
    {
        _ = CreateClient();
        var wsPort = Environment.GetEnvironmentVariable("PORT");

        if (string.IsNullOrEmpty(wsPort)) throw new Exception("Environment variable WS_PORT is not set");

        var clientId = "clientA";
        var url = "ws://localhost:" + wsPort + "?id=" + clientId;
        outputHelper.WriteLine($"Connecting to WebSocket at: {url}");

        var client = new WsRequestClient(
            new[] { typeof(ClientWantsToAuthenticateDto).Assembly },
            url
        );

        await client.ConnectAsync();
        var manager = Services.GetRequiredService<IConnectionManager>();
        Assert.Single(manager.ConnectionIdToSocket.Values);
        Assert.Single(manager.SocketToConnectionId.Values);
        

    }

 
}