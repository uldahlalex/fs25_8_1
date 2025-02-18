using System.Collections.Concurrent;
using System.Text.Json;
using Api;
using Fleck;
using StackExchange.Redis;
using WebSocketBoilerplate;

public class RedisConnectionManager : IConnectionManager
{
    private readonly IConnectionMultiplexer _redis;
    public ConcurrentDictionary<string /* Client ID */, IWebSocketConnection> ConnectionIdToSocket { get; } = new();
    public ConcurrentDictionary<string /* Socket ID */, string /* Client ID */> SocketToConnectionId { get; } = new();
    private readonly ILogger<RedisConnectionManager> _logger;
    private IDatabase _db;

    public string[] InitialTopicIds = new[] { "device/A", "room/A" };

    public RedisConnectionManager(IConnectionMultiplexer redis, ILogger<RedisConnectionManager> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = _redis.GetDatabase();

        foreach (var topicId in InitialTopicIds)
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
        var keys = server.Keys(pattern: "topic:*").ToList();

        var tasks = keys.Select(async key =>
        {
            var topicId = key.ToString().Replace("topic:", "");
            var members = await _db.SetMembersAsync(key);
            var memberSet = new HashSet<string>(members.Select(m => m.ToString()));
            result.TryAdd(topicId, memberSet);
            return true;
        });

        await Task.WhenAll(tasks);
        return result;
    }

    public async Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics()
    {
        var result = new ConcurrentDictionary<string, HashSet<string>>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "member:*");

        foreach (var key in keys)
        {
            var memberId = key.ToString().Replace("member:", "");
            var topics = await _db.SetMembersAsync(key);
            var topicSet = new HashSet<string>(topics.Select(t => t.ToString()));
            result.TryAdd(memberId, topicSet);
        }

        return result;
    }

    private string ConnectionToSocketKey(string connectionId) => $"connection_to_socket:{connectionId}";
    private string SocketToConnectionKey(string socketId) => $"socket_to_connection:{socketId}";

    public async Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId()
    {
        var result = new Dictionary<string, string>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "connection_to_socket:*").ToList();

        foreach (var key in keys)
        {
            var connectionId = key.ToString().Replace("connection_to_socket:", "");
            var socketId = await _db.StringGetAsync(key);
            if (!socketId.IsNull)
            {
                result[connectionId] = socketId.ToString();
            }
        }

        return result;
    }

    public async Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId()
    {
        var result = new Dictionary<string, string>();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "socket_to_connection:*").ToList();

        foreach (var key in keys)
        {
            var socketId = key.ToString().Replace("socket_to_connection:", "");
            var connectionId = await _db.StringGetAsync(key);
            if (!connectionId.IsNull)
            {
                result[socketId] = connectionId.ToString();
            }
        }

        return result;
    }

    public async Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        expiry ??= TimeSpan.FromDays(1);
        var tx = _db.CreateTransaction();

        await tx.SetAddAsync(TopicKey(topic), memberId);
        await tx.KeyExpireAsync(TopicKey(topic), expiry.Value);
        await tx.SetAddAsync(MemberKey(memberId), topic);
        await tx.KeyExpireAsync(MemberKey(memberId), expiry.Value);

        await tx.ExecuteAsync();
        await LogCurrentState();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var topicKey = TopicKey(topic);
        var memberKey = MemberKey(memberId);

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
        _logger.LogInformation($"OnOpen called with clientId: {clientId} and socketId: {socket.ConnectionInfo.Id}");

        var socketId = socket.ConnectionInfo.Id.ToString();

        // If there's an existing connection for this client, clean it up first
        var oldSocketId = await _db.StringGetAsync(ConnectionToSocketKey(clientId));
        if (!oldSocketId.IsNull)
        {
            await _db.KeyDeleteAsync(SocketToConnectionKey(oldSocketId.ToString()));
            await _db.KeyDeleteAsync(ConnectionToSocketKey(clientId));
            _logger.LogInformation($"Removed old connection {oldSocketId} for client {clientId}");
        }

        // Add new connection mappings to Redis
        var tx = _db.CreateTransaction();
        tx.StringSetAsync(ConnectionToSocketKey(clientId), socketId);
        tx.StringSetAsync(SocketToConnectionKey(socketId), clientId);
        await tx.ExecuteAsync();

        // Update in-memory collections
        ConnectionIdToSocket[clientId] = socket;
        SocketToConnectionId[socketId] = clientId;

        _logger.LogInformation($"Added new connection {socketId} for client {clientId}");
        await LogCurrentState();
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        var socketId = socket.ConnectionInfo.Id.ToString();

        // Only remove if this is the current socket for this client
        var currentSocketId = await _db.StringGetAsync(ConnectionToSocketKey(clientId));
        if (!currentSocketId.IsNull && currentSocketId.ToString() == socketId)
        {
            // Remove from Redis
            var tx = _db.CreateTransaction();
            tx.KeyDeleteAsync(ConnectionToSocketKey(clientId));
            tx.KeyDeleteAsync(SocketToConnectionKey(socketId));
            await tx.ExecuteAsync();

            // Remove from in-memory collections
            ConnectionIdToSocket.TryRemove(clientId, out _);
            _logger.LogInformation($"Removed connection for client {clientId}");
        }

        SocketToConnectionId.TryRemove(socketId, out _);

        // Clean up topics
        var topics = await GetTopicsFromMemberId(clientId);
        foreach (var topic in topics)
        {
            await RemoveFromTopic(topic, clientId);
        }

        await _db.KeyDeleteAsync(MemberKey(clientId));
    }

    public async Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        _logger.LogInformation($"Starting broadcast to topic: {topic}");

        var members = await GetMembersFromTopicId(topic);
        _logger.LogInformation($"Found {members.Count} members in topic {topic}: {string.Join(", ", members)}");

        foreach (var memberId in members)
        {
            if (ConnectionIdToSocket.TryGetValue(memberId, out var socket))
            {
                try
                {
                    if (socket.IsAvailable)
                    {
                        socket.SendDto(message);
                        _logger.LogInformation($"Successfully sent message to {memberId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Socket not available for {memberId}");
                        await RemoveFromTopic(topic, memberId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send message to {memberId}");
                    await RemoveFromTopic(topic, memberId);
                }
            }
            else
            {
                _logger.LogWarning($"No socket found for member: {memberId}");
                await RemoveFromTopic(topic, memberId);
            }
        }
    }

    public async Task LogCurrentState()
    {
        _logger.LogInformation("Current state:");
        _logger.LogInformation("ConnectionIdToSocket:");
        _logger.LogInformation(JsonSerializer.Serialize(new
        {
            ConnectionIdToSocket = await GetAllConnectionIdsWithSocketId(),
            SocketToConnectionId = await GetAllSocketIdsWithConnectionId(),
            TopicsWithMembers = await GetAllTopicsWithMembers(),
            MembersWithTopics = await GetAllMembersWithTopics()
        }, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
    }
}