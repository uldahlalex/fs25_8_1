using System.Collections.Concurrent;
using Fleck;
using StackExchange.Redis;

namespace Api;

public class ConnectionManager(IDatabase redis)
{
    public ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; } = new();
    
    private const string TOPIC_PREFIX = "topic:";
    private const string MEMBER_TOPICS = "member:topics:";

    // Topic key generators - all topics follow the same pattern
    public static string Topic(string name) => $"{TOPIC_PREFIX}{name}";
    public static string MemberTopics(string memberId) => $"{MEMBER_TOPICS}{memberId}";
    
    // Common operations
    public async Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        var topicKey = Topic(topic);
        var memberTopicsKey = MemberTopics(memberId);

        var tx = redis.CreateTransaction();
        tx.SetAddAsync(topicKey, memberId);
        tx.SetAddAsync(memberTopicsKey, topic);
        
        if (expiry.HasValue)
        {
            tx.KeyExpireAsync(topicKey, expiry.Value);
            tx.KeyExpireAsync(memberTopicsKey, expiry.Value);
        }
        
        await tx.ExecuteAsync();
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var topicKey = Topic(topic);
        var memberTopicsKey = MemberTopics(memberId);

        var tx = redis.CreateTransaction();
        tx.SetRemoveAsync(topicKey, memberId);
        tx.SetRemoveAsync(memberTopicsKey, topic);
        await tx.ExecuteAsync();
    }

    public async Task<HashSet<string>> GetTopicMembers(string topic)
    {
        var members = await redis.SetMembersAsync(Topic(topic));
        return members.Select(m => m.ToString()).ToHashSet();
    }

    public async Task<HashSet<string>> GetMemberTopics(string memberId)
    {
        var topics = await redis.SetMembersAsync(MemberTopics(memberId));
        return topics.Select(t => t.ToString()).ToHashSet();
    }

    // Connection management
    public async Task<bool> OnOpen(IWebSocketConnection socket, string clientId)
    {
        if (Sockets.TryGetValue(clientId, out var existingSocket))
        {
            try { existingSocket.Close(); } catch { }
        }

        // Map socket.Id to clientId
        await AddToTopic($"socket:{socket.ConnectionInfo.Id}", clientId, TimeSpan.FromDays(1));
        
        return Sockets.TryAdd(clientId, socket);
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);
        
        // Clean up socket mapping
        await RemoveFromTopic($"socket:{socket.ConnectionInfo.Id}", clientId);
        
        // Get all topics this member was part of
        var topics = await GetMemberTopics(clientId);
        
        // Remove member from all topics
        var tx = redis.CreateTransaction();
        foreach (var topic in topics)
        {
            tx.SetRemoveAsync(Topic(topic), clientId);
        }
        tx.KeyDeleteAsync(MemberTopics(clientId));
        
        await tx.ExecuteAsync();
    }

    // Lookup operations
    public async Task<string?> LookupBySocketId(string socketId)
    {
        var members = await GetTopicMembers($"socket:{socketId}");
        return members.FirstOrDefault();
    }

    // Broadcasting
    public async Task BroadcastToTopic(string topic, string message)
    {
        var members = await GetTopicMembers(topic);
        
        foreach (var memberId in members)
        {
            if (Sockets.TryGetValue(memberId, out var socket))
            {
                await socket.Send(message);
            }
        }
    }

    // Subscription management
    public Task Subscribe(string topic, string memberId) =>
        AddToTopic(topic, memberId, TimeSpan.FromDays(1));

    public Task Unsubscribe(string topic, string memberId) =>
        RemoveFromTopic(topic, memberId);

    // Utility methods for checking membership
    public async Task<bool> IsInTopic(string topic, string memberId)
    {
        return await redis.SetContainsAsync(Topic(topic), memberId);
    }

    public async Task<Dictionary<string, HashSet<string>>> GetAllTopicMembers(string[] topics)
    {
        var result = new Dictionary<string, HashSet<string>>();
        var tasks = topics.Select(async topic =>
        {
            result[topic] = await GetTopicMembers(topic);
        });
        
        await Task.WhenAll(tasks);
        return result;
    }
}