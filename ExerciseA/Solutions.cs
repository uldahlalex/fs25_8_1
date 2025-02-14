using StackExchange.Redis;

namespace ExerciseA;
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class VisitorStats
{
    public long TotalVisits { get; set; }
    public DateTime LastVisit { get; set; }
}
public class RedisExercises
{
    private readonly IDatabase _db;


    public RedisExercises()
    {
        _db = RedisConnectionHelper.GetDatabase();
    }

    /// <summary>
    /// Exercise 1: Store and retrieve a simple string value in Redis.
    /// Store the provided value with the given key and then retrieve it.
    /// </summary>
    public async Task<string> StoreAndRetrieveString(string key, string value)
    {
        _db.StringSet(key, value);
        return "hi";
    }
    
    
    [Fact]
    public async Task Exercise1_StoreAndRetrieveString_WorksCorrectly()
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
    /// Exercise 2: Store a string value with expiration time.
    /// Store the provided value with the given key and make it expire after the specified seconds.
    /// Return true if storage was successful.
    /// </summary>
    public async Task<bool> StoreWithExpiration(string key, string value, int expirationSeconds)
    {
        return _db.StringSet(key, value, TimeSpan.FromSeconds(expirationSeconds));
    }
    
    [Fact]
    public async Task Exercise2_StoreWithExpiration_WorksCorrectly()
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
    /// Exercise 3: Implement a counter that can be incremented and decremented.
    /// If the counter doesn't exist, initialize it with the given startValue.
    /// Return the final counter value after performing the operation.
    /// </summary>
    public async Task<long> ManageCounter(string counterKey, long startValue, bool increment)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 4: Store and retrieve a User object as JSON.
    /// Serialize the user object to JSON, store it in Redis, and then retrieve and deserialize it.
    /// Return null if the user doesn't exist.
    /// </summary>
    public async Task<User> StoreAndRetrieveUser(string key, User user)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 5: Store multiple fields of a User object as a hash.
    /// Store each property of the User object as a hash field.
    /// Return true if storage was successful.
    /// </summary>
    public async Task<bool> StoreUserAsHash(string key, User user)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 6: Check if a key exists and delete it if it does.
    /// Return true if the key was found and deleted, false if it didn't exist.
    /// </summary>
    public async Task<bool> CheckAndDelete(string key)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 7: Implement a simple rate limiter.
    /// Return true if the action is allowed (less than maxAttempts in the time window).
    /// Return false if the rate limit has been exceeded.
    /// </summary>
    public async Task<bool> CheckRateLimit(string key, int maxAttempts, int windowSeconds)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 8: Implement a visitor tracking system.
    /// Track total visits and last visit time for a given page.
    /// Return the updated stats.
    /// </summary>
    public async Task<VisitorStats> TrackPageVisit(string pageKey)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 9: Implement a simple product inventory system.
    /// Decrease the product quantity by the specified amount.
    /// Return true if there was sufficient quantity, false if not.
    /// </summary>
    public async Task<bool> DecrementProductQuantity(string productKey, int quantity)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Exercise 10: Implement a basic caching system with automatic expiration.
    /// Store the value only if the key doesn't exist (implement Cache-Aside pattern).
    /// Return the value whether it was just stored or already existed.
    /// </summary>
    public async Task<string> GetOrSetCache(string key, string value, int expirationSeconds)
    {
        throw new NotImplementedException();
    }
}