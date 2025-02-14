using System.Text.Json;
using Fleck;
using StackExchange.Redis;
using WebSocketBoilerplate;

namespace ExerciseA;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

public class RedisExercises
{
    private readonly IDatabase _db;


    public RedisExercises()
    {
        _db = RedisConnectionHelper.GetDatabase();
    }

    /// <summary>
    /// Store and retrieve a simple string value in Redis.
    /// Store the provided value with the given key and then retrieve it.
    /// </summary>
    public async Task<string> StoreAndRetrieveString(string key, string value)
    {
        _db.StringSet(key, value);
        return "hi";
    }

    [Fact]
    public async Task StoreAndRetrieveString_WorksCorrectly()
    {
        #region Test //-

        // Arrange
        string key = "test:string";
        string value = "Hello Redis!";

        // Act
        string result = await StoreAndRetrieveString(key, value);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal(value, await _db.StringGetAsync(key));

        #endregion
    }

    /// <summary>
    /// Store a string value with expiration time.
    /// Store the provided value with the given key and make it expire after the specified seconds.
    /// Return true if storage was successful.
    /// </summary>
    public async Task<bool> StoreWithExpiration(string key, string value, int expirationSeconds)
    {
        return _db.StringSet(key, value, TimeSpan.FromSeconds(expirationSeconds));
    }
    
    [Fact]
    public async Task StoreWithExpiration_WorksCorrectly()
    {
        // Arrange
        string key = "test:expiring";
        string value = "This will expire";
        int expirationSeconds = 1;

        // Act
        bool stored = await StoreWithExpiration(key, value, expirationSeconds);
        bool existsBeforeExpiration = await _db.KeyExistsAsync(key);
        await Task.Delay(2000); // Wait for expiration
        bool existsAfterExpiration = await _db.KeyExistsAsync(key);

        // Assert
        Assert.True(stored);
        Assert.True(existsBeforeExpiration);
        Assert.False(existsAfterExpiration);
    }

    
    /// <summary>
    ///  Store and retrieve a User object as JSON.
    /// Serialize the user object to JSON, store it in Redis, and then retrieve and deserialize it.
    /// Return null if the user doesn't exist.
    /// </summary>
    public async Task<User> StoreAndRetrieveUser(string key, User user)
    {
        var json = JsonSerializer.Serialize(user);
        var result =_db.StringSet(key, json);
        var retrieved = _db.StringGet(key);
        var deserialized = JsonSerializer.Deserialize<User>(retrieved.ToString());
        return deserialized;
    }
    
    [Fact]
    public async Task StoreAndRetrieveUser_WorksCorrectly()
    {
        // Arrange
        string key = "test:user";
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Age = 30 };

        // Act
        var result = await StoreAndRetrieveUser(key, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(user.Age, result.Age);
    }



    /// <summary>
    /// Store multiple fields of a User object as a hash.
    /// Store each property of the User object as a hash field.
    /// </summary>
    public async Task StoreUserAsHash(string key, User user)
    {
        var insertionObject = new HashEntry[]
        {
            new HashEntry(nameof(User.Age), user.Age),
            new HashEntry(nameof(User.FirstName), user.FirstName),
            new HashEntry(nameof(User.LastName), user.LastName),
            new HashEntry(nameof(User.Id), user.Id),
        };
        _db.HashSet(key, insertionObject);
        
    }
    [Fact]
    public async Task StoreUserAsHash_WorksCorrectly()
    {
        // Arrange
        string key = "test:user:hash";
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Age = 30 };

        // Act
        await StoreUserAsHash(key, user);

        // Assert
        Assert.Equal("1", await _db.HashGetAsync(key, "Id"));
        Assert.Equal("John", await _db.HashGetAsync(key, "FirstName"));
        Assert.Equal("Doe", await _db.HashGetAsync(key, "LastName"));
        Assert.Equal("30", await _db.HashGetAsync(key, "Age"));
    }

    /// <summary>
    /// Check if a key exists and delete it if it does.
    /// Return true if the key was found and deleted, false if it didn't exist.
    /// </summary>
    public async Task<bool> CheckAndDelete(string key)
    {
        var exists = _db.KeyExists(key);
        if (exists)
        {
                        _db.KeyDelete(key);
                        return true;
        }

        return false;
    }

    [Fact]
    public async Task CheckAndDelete_WorksCorrectly()
    {
        // Arrange
        string key = "test:delete";
        await _db.StringSetAsync(key, "value");

        // Act
        bool resultExisting = await CheckAndDelete(key);
        bool resultNonExisting = await CheckAndDelete("nonexistent:key");

        // Assert
        Assert.True(resultExisting);
        Assert.False(resultNonExisting);
        Assert.False(await _db.KeyExistsAsync(key));
    }
    

    /// <summary>
    /// Implement a basic caching system with automatic expiration.
    /// Store the value IF the key doesn't exist.
    /// Return the value whether it was just stored or already existed.
    /// </summary>
    public async Task<string> GetOrSetCache(string key, string value, int expirationSeconds)
    {
        if (!_db.KeyExists(key))
        {
            _db.StringSet(key, value, TimeSpan.FromSeconds(expirationSeconds));
            return value;
        }

        return _db.StringGet(key);


    }


    [Fact]
    public async Task GetOrSetCache_WorksCorrectly()
    {
        // Arrange
        string key = "test:cache";
        string initialValue = "cached value";
        string newValue = "new value";
        int expirationSeconds = 1;

        // Act & Assert
        // First call should set the value
        string result1 = await GetOrSetCache(key, initialValue, expirationSeconds);
        Assert.Equal(initialValue, result1);

        // Second call should return the cached value, not set new one
        string result2 = await GetOrSetCache(key, newValue, expirationSeconds);
        Assert.Equal(initialValue, result2);

        // Wait for expiration
        await Task.Delay(expirationSeconds * 2000);

        // After expiration, should set new value
        string result3 = await GetOrSetCache(key, newValue, expirationSeconds);
        Assert.Equal(newValue, result3);

        // Verify TTL was set
        var ttl = await _db.KeyTimeToLiveAsync(key);
        Assert.True(ttl.HasValue && ttl.Value.TotalSeconds <= expirationSeconds);
    }


    public async Task StoreWebSocketConnection()
    {
        var server = new CustomWebSocketServer(async socket =>
        {

            
        }, async socket =>
        {

        });

        var wsClient = new WsRequestClient([  typeof(RequestDto).Assembly,], url: "ws://localhost:"+Environment.GetEnvironmentVariable("PORT"));

        await wsClient.ConnectAsync();
    }

    [Fact]
    public async Task WebSocketConnection_Storing_Correctly_Stores()
    {

    }
}