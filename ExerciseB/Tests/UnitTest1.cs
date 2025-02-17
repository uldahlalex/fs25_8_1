using System.Text.Json;
using Api;
using ExerciseA.EventHandlers;
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
        //return create host
    }

    [Fact]
    public async Task Api_Can_Successfully_Add_Connection_To_Redis()
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
        outputHelper.WriteLine("Successfully connected to WebSocket");

        var connectionManager = Server.Services
            .GetRequiredService<ConnectionManager>();
        var result = await connectionManager.GetTopicsFromMemberId(clientId);
        outputHelper.WriteLine(JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task Api_Can_Successfully_Remove_Connection_Upon_Disconnect()
    {
        var client = new WsRequestClient([typeof(ClientWantsToAuthenticateDto).Assembly],
            "ws://localhost:" + Environment.GetEnvironmentVariable("PORT"));
        await client.ConnectAsync();
        await Task.Delay(1000);
        client.Dispose();

        //Assert connection is gone from redis
    }

    [Fact]
    public async Task Api_Can_Successfully_Add_Connection_To_Topic_Subscriptions()
    {
        var client = new WsRequestClient([typeof(ClientWantsToAuthenticateDto).Assembly],
            "ws://localhost:" + Environment.GetEnvironmentVariable("PORT"));
        await client.ConnectAsync();
        var requestId = new Guid().ToString();
        await client.SendMessage<ClientWantsToAuthenticateDto, ServerAuthenticatesClientDto>(
            new ClientWantsToAuthenticateDto()
            {
                requestId = requestId
            });
        //Assert connection is added top topic subscription
    }
}