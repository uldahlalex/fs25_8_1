using System.Collections.Concurrent;
using Fleck;
using StackExchange.Redis;

namespace Api;

public class ConnectionManager(IDatabase redis)
{
    public ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; } = new();

    private const string TOPIC_PREFIX = "topic:";
    private const string MEMBER_TOPICS = "member:topics:";

    public static string Topic(string name) => $"{TOPIC_PREFIX}{name}";
    public static string MemberTopics(string memberId) => $"{MEMBER_TOPICS}{memberId}";

    public static string Room(string roomId) => Topic($"room:{roomId}");
    public static string Device(string deviceId) => Topic($"device:{deviceId}");
    public static string Role(string role) => Topic($"role:{role}");
    public static string Socket(string socketId) => Topic($"socket:{socketId}");

    public static string User(string userId) => Topic($"user:{userId}");  

    public async Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        expiry ??= TimeSpan.FromDays(1);

        var tx = redis.CreateTransaction();
        
        var topicKey = Topic(topic);
        await tx.SetAddAsync(topicKey, memberId);
        await tx.KeyExpireAsync(topicKey, expiry.Value);

        var memberTopicsKey = MemberTopics(memberId);
        await tx.SetAddAsync(memberTopicsKey, topic);
        await tx.KeyExpireAsync(memberTopicsKey, expiry.Value);
        
        await tx.ExecuteAsync();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var tx = redis.CreateTransaction();
        
        var topicKey = Topic(topic);
        await tx.SetRemoveAsync(topicKey, memberId);
        
        var memberTopicsKey = MemberTopics(memberId);
        await tx.SetRemoveAsync(memberTopicsKey, topic);
        
        await tx.ExecuteAsync();
    }

    public async Task<HashSet<string>> GetMembersFromTopicId(string topic)
    {
        var members = await redis.SetMembersAsync(Topic(topic));
        return members.Select(m => m.ToString()).ToHashSet();
    }

    public async Task<HashSet<string>> GetTopicsFromMemberId(string memberId)
    {
        var topics = await redis.SetMembersAsync(MemberTopics(memberId));
        return topics.Select(t => t.ToString()).ToHashSet();
    }

    public async Task<bool> OnOpen(IWebSocketConnection socket, string clientId)
    {
        if (Sockets.TryGetValue(clientId, out var existingSocket))
        {
            existingSocket.Close();
        }

        await AddToTopic($"socket:{socket.ConnectionInfo.Id}", clientId, TimeSpan.FromDays(1));

        return Sockets.TryAdd(clientId, socket);
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);

        await RemoveFromTopic($"socket:{socket.ConnectionInfo.Id}", clientId);

        var topics = await GetTopicsFromMemberId(clientId);

        var tx = redis.CreateTransaction();
        foreach (var topic in topics)
        {
            await tx.SetRemoveAsync(Topic(topic), clientId);
        }

        await tx.KeyDeleteAsync(MemberTopics(clientId));

        await tx.ExecuteAsync();
    }

    public async Task<string?> LookupBySocketId(string socketId)
    {
        var members = await GetMembersFromTopicId($"socket:{socketId}");
        return members.FirstOrDefault();
    }

    public async Task BroadcastToTopic(string topic, string message)
    {
        var members = await GetMembersFromTopicId(topic);

        foreach (var memberId in members)
        {
            if (Sockets.TryGetValue(memberId, out var socket))
            {
                await socket.Send(message);
            }
        }
    }

    public Task Subscribe(string topic, string memberId) =>
        AddToTopic(topic, memberId, TimeSpan.FromDays(1));

    public Task Unsubscribe(string topic, string memberId) =>
        RemoveFromTopic(topic, memberId);

    public async Task<bool> IsInTopic(string topic, string memberId)
    {
        return await redis.SetContainsAsync(Topic(topic), memberId);
    }
    
}