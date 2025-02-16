using ExerciseA;
using ExerciseA.EventHandlers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Services;
using StackExchange.Redis;
using WebSocketBoilerplate;

namespace Tests;

public class ApiTests(ITestOutputHelper outputHelper) : WebApplicationFactory<Program>
{
    private readonly IDatabase _db = RedisConnectionHelper.GetDatabase();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //configure logging
        
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddXUnit(outputHelper);
        });

    }

    [Fact]
    public async Task Api_Can_Successfully_Add_Connection_To_Redis()
    {
        
        _ = CreateClient();
        var wsPort = Environment.GetEnvironmentVariable("PORT");

        if (string.IsNullOrEmpty(wsPort)) throw new Exception("Environment variable WS_PORT is not set");

        var url = "ws://localhost:" + wsPort;
        outputHelper.WriteLine($"Connecting to WebSocket at: {url}");

        var client = new WsRequestClient(
            new[] { typeof(ClientWantsToAuthenticateDto).Assembly },
            url
        );

        await client.ConnectAsync();
        outputHelper.WriteLine("Successfully connected to WebSocket");
        var pattern = "topic:socket:*";
        //todo fix rest of test after ws server adjustment
        // var keys = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First())
        //     .Keys(pattern: pattern);
        //     
        // var socketKey = "";
        // foreach (var key in keys)
        // {
        //     var mems = await _db.SetMembersAsync(key);
        //     if (mems.Any(m => m.ToString() == ""))
        //     {
        //         socketKey = key;
        //         break;
        //     }
        // }
        //
        //
        // var members = await _db.SetMembersAsync(socketKey);
        // Assert.Contains(members, m => m.ToString() == "clientId");
        //
        //  client.Dispose();
        // await Task.Delay(1000);
        //
        // members = await _db.SetMembersAsync(socketKey);
        // Assert.Empty(members);
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
