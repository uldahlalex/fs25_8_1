using System.Text.Json;
using Api;
using ExerciseA;
using Fleck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Tests;

public class ConnectionManagerTests
{
    private readonly DictionaryConnectionManager _dictionaryManager;
    private readonly RedisConnectionManager _redisManager;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppOptions _appOptions;

    public ConnectionManagerTests()
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()  // This will override appsettings.json values
            .Build();

        // Set up options
        var services = new ServiceCollection();
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(nameof(AppOptions)))
            .ValidateOnStart();
        
        var serviceProvider = services.BuildServiceProvider();
        _appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

        // Set up logging
        var loggerFactory = new LoggerFactory();
        var dictionaryLogger = new Logger<DictionaryConnectionManager>(loggerFactory);
        var redisLogger = new Logger<RedisConnectionManager>(loggerFactory);
        
        _dictionaryManager = new DictionaryConnectionManager(dictionaryLogger);
        
        // Configure Redis
        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            Ssl = true,
            DefaultDatabase = 0,
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            EndPoints = { { _appOptions.REDIS_HOST, 6379 } },
            User = _appOptions.REDIS_USERNAME,
            Password = _appOptions.REDIS_PASSWORD
        };

        _redis = ConnectionMultiplexer.Connect(redisConfig);
        _redisManager = new RedisConnectionManager(_redis, redisLogger);
        
    }

    [Theory]
    [InlineData(true)]  
    [InlineData(false)] 
    public async Task OnConnect_Can_Add_Socket_And_Client_To_Storage(bool useDictionary)
    {
        // arrange
        var manager = useDictionary ? 
            (IConnectionManager)_dictionaryManager : 
            (IConnectionManager)_redisManager;
            
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        
        // act
        await manager.OnOpen(ws, connectionId);
        
        // assert
        Assert.Equal(manager.Sockets.Values.First(), ws);
        
        var allTopics = await manager.GetAllTopicsWithMembers();
        var allMembers = await manager.GetAllMembersWithTopics();
        
        if (!allTopics[socketId.ToString()].Contains(connectionId))
            throw new Exception($"Expected client id {connectionId} to be in hash set(value) of key {socketId}" +
                              $"Values found: {JsonSerializer.Serialize(allTopics)}");
        
        if (!allMembers[connectionId].Contains(socketId.ToString()))
            throw new Exception($"Expected socket id {socketId} to be in hash set(value) of key {connectionId}" +
                              $"Values found: {JsonSerializer.Serialize(allMembers)}");
    }
    
    [Theory]
    [InlineData(true)]  // true for dictionary manager
    [InlineData(false)] // false for redis manager
    public async Task OnClose_Can_Remove_Socket_And_Client_From_Storage(bool useDictionary)
    {
        // arrange
        var manager = useDictionary ? 
            (IConnectionManager)_dictionaryManager : 
            (IConnectionManager)_redisManager;
            
        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        await manager.OnOpen(ws, connectionId);
        
        // act
        await manager.OnClose(ws, connectionId);
        
        // assert
        Assert.DoesNotContain(manager.Sockets.Values, s => s.ConnectionInfo.Id == socketId);
    
        var allTopics = await manager.GetAllTopicsWithMembers();
        Assert.False(allTopics.ContainsKey(socketId.ToString()) && 
                     allTopics[socketId.ToString()].Contains(connectionId));
    }

    public async Task Client_Reconnection_Successfully_Associates_With_Existing_State()
    {
        throw new NotImplementedException();
    }

    public async Task Server_Restart_With_Reconnecting_Clients_And_Redis_Allows_For_Reestablshing_State()
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {

        var server = _redis.GetServer(_redis.GetEndPoints().First());
        server.FlushDatabase();
        _redis.Dispose();
    }
}