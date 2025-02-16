using ExerciseA;
using ExerciseA.EventHandlers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Testing.Platform.Services;
using StackExchange.Redis;
using WebSocketBoilerplate;

namespace Tests;

public class ApiTests : WebApplicationFactory<Program>
{
    private readonly IDatabase _db = RedisConnectionHelper.GetDatabase();
    
    [Fact]
    public async Task Api_Can_Successfully_Add_Connection_To_Redis()
    {
        var client = new WsRequestClient([typeof(ClientWantsToAuthenticateDto).Assembly],
            "ws://localhost:" + Environment.GetEnvironmentVariable("PORT"));
        await client.ConnectAsync();
        await Task.Delay(1000);
        
        

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
        await client.SendMessage<ClientWantsToAuthenticateDto, ServerAuthenticatesClient>(
            new ClientWantsToAuthenticateDto()
            { 
                
                requestId = requestId
            });
        //Assert connection is added top topic subscription

    }
    
}
