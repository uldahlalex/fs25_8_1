using System.Collections.Concurrent;
using System.Text.Json;
using Api;
using Fleck;
using StackExchange.Redis;
using WebSocketBoilerplate;

public class RedisConnectionManager : IConnectionManager
{
    private readonly IConnectionMultiplexer _redis;
    public ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; } = new();


    private readonly ILogger<RedisConnectionManager> _logger;
    private IDatabase _db;

    public string[] TopicIds = new[] { "device/A", "room/A" };

    public RedisConnectionManager(IConnectionMultiplexer redis, ILogger<RedisConnectionManager> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = _redis.GetDatabase();
        
        foreach (var topicId in TopicIds)
        {
            _db.SetAdd($"topic:{topicId}", Array.Empty<RedisValue>());
        }
    }

    private string TopicKey(string topic) => $"topic:{topic}";
    private string MemberKey(string memberId) => $"member:{memberId}";

    public async Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers()
    {
        var result = new ConcurrentDictionary<string, HashSet<string>>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
    
        // Get keys synchronously to avoid potential timeout issues
        var keys = server.Keys(pattern: "topic:*").ToList();
    
        // Get members for all keys in parallel
        var tasks = keys.Select(async key => {
            var topicId = key.ToString().Replace("topic:", "");
            var members = await _db.SetMembersAsync(key);
            var memberSet = new HashSet<string>(members.Select(m => m.ToString()));
        
            if (memberSet.Any())
            {
                result.TryAdd(topicId, memberSet);
            }
        
            return true;
        });
    
        await Task.WhenAll(tasks);
        return result;
    }

    public async Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics()
    {
        var result = new ConcurrentDictionary<string, HashSet<string>>();
    
        // Get all keys matching member:*
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "member:*");
    
        foreach (var key in keys)
        {
            var memberId = key.ToString().Replace("member:", "");
            var topics = await _db.SetMembersAsync(key);
            var topicSet = new HashSet<string>(topics.Select(t => t.ToString()));
        
            if (topicSet.Any())
            {
                result.TryAdd(memberId, topicSet);
            }
        }
    
        return result;
    }
    public async Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        expiry ??= TimeSpan.FromDays(1);
        var tx = _db.CreateTransaction();

        // Equivalent to TopicMembers dictionary
        var topicTask = tx.SetAddAsync(TopicKey(topic), memberId);
        var topicExpiryTask = tx.KeyExpireAsync(TopicKey(topic), expiry.Value);

        // Equivalent to MemberTopics dictionary
        var memberTask = tx.SetAddAsync(MemberKey(memberId), topic);
        var memberExpiryTask = tx.KeyExpireAsync(MemberKey(memberId), expiry.Value);

        await tx.ExecuteAsync();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var topicKey = TopicKey(topic);
        var memberKey = MemberKey(memberId);
    
        // Execute commands directly instead of using transaction
        await Task.WhenAll(
            _db.SetRemoveAsync(topicKey, memberId),
            _db.SetRemoveAsync(memberKey, topic)
        );
    }

    public async Task<List<string>> GetMembersFromTopicId(string topic)
    {
        var members = await _db.SetMembersAsync(TopicKey(topic));
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<List<string>> GetTopicsFromMemberId(string memberId)
    {
        var topics = await _db.SetMembersAsync(MemberKey(memberId));
        return topics.Select(t => t.ToString()).ToList();
    }

    public async Task OnOpen(IWebSocketConnection socket, string clientId)
    {
        var success = Sockets.TryAdd(clientId, socket);
        if (!success)
            throw new Exception($"Failed to add socket {socket.ConnectionInfo.Id} to dictionary with client ID key {clientId}");
            
        await AddToTopic(socket.ConnectionInfo.Id.ToString(), clientId);
        await AddToTopic(clientId, socket.ConnectionInfo.Id.ToString());
        _logger.LogInformation($"Connected with client ID {clientId} and socket ID {socket.ConnectionInfo.Id}");
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);
    
        await RemoveFromTopic(socket.ConnectionInfo.Id.ToString(), clientId);
        await RemoveFromTopic(clientId, socket.ConnectionInfo.Id.ToString());
    }
    
    
    public async Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        var members = await GetMembersFromTopicId(topic);
        foreach (var memberId in members)
        {
            if (Sockets.TryGetValue(memberId, out var socket))
            {
                _logger.LogInformation("Sending message to socket: "+socket);

                socket.SendDto(message);
            }
        }
    }
}