using ExerciseA;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

public static class RedisConnectionHelper
{
    private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
    {
        
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        var options = new AppOptions();
        config.GetSection(nameof(AppOptions)).Bind(options);
        
        var redisConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            Ssl = true,
            DefaultDatabase = 0,
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            EndPoints = { { options.REDIS_HOST, 6379 } },
            User = options.REDIS_USERNAME,
            Password = options.REDIS_PASSWORD
        };
        return ConnectionMultiplexer.Connect(redisConfig);
    });

    public static ConnectionMultiplexer Connection => lazyConnection.Value;

    public static IDatabase GetDatabase(int db = 0) => Connection.GetDatabase(db);
}