using System.Collections.Concurrent;
using System.Text.Json;
using Fleck;
using StackExchange.Redis;
using WebSocketBoilerplate;

namespace Api;

public class ConnectionManager(IConnectionMultiplexer redis)
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

        try
        {

            var tx = redis.GetDatabase().CreateTransaction();

            var topicKey = Topic(topic);
             tx.SetAddAsync(topicKey, memberId);
             tx.KeyExpireAsync(topicKey, expiry.Value);

            var memberTopicsKey = MemberTopics(memberId);
             tx.SetAddAsync(memberTopicsKey, topic);
             tx.KeyExpireAsync(memberTopicsKey, expiry.Value);

             await tx.ExecuteAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            throw;
        }
    }

    public async Task RemoveFromTopic(string topic, string memberId)
    {
        var tx = redis.GetDatabase().CreateTransaction();
        
        var topicKey = Topic(topic);
        tx.SetRemoveAsync(topicKey, memberId);
        
        var memberTopicsKey = MemberTopics(memberId);
         tx.SetRemoveAsync(memberTopicsKey, topic);
        
        await tx.ExecuteAsync();
    }

    public async Task<List<string>> GetMembersFromTopicId(string topic)
    {
        var members = await redis.GetDatabase().SetMembersAsync(Topic(topic));
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<List<string>> GetTopicsFromMemberId(string memberId)
    {
        var topics = await redis.GetDatabase().SetMembersAsync(MemberTopics(memberId));
        return topics.Select(t => t.ToString()).ToList();
    }

    public async Task OnOpen(IWebSocketConnection socket, string clientId)
    {
        await AddToTopic(Socket(socket.ConnectionInfo.Id.ToString()), clientId, TimeSpan.FromDays(1));
        Sockets.TryAdd(clientId, socket);
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);

        await RemoveFromTopic(Socket(socket.ConnectionInfo.Id.ToString()), clientId);

        var topics = await GetTopicsFromMemberId(clientId);

        var tx = redis.GetDatabase().CreateTransaction();
        foreach (var topic in topics)
        {
             tx.SetRemoveAsync(Topic(topic), clientId);
        }

        await tx.KeyDeleteAsync(MemberTopics(clientId));

        await tx.ExecuteAsync();
    }

    public async Task<string?> LookupBySocketId(string socketId)
    {
        var members = await GetMembersFromTopicId(Socket(socketId));
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
    

    public async Task<bool> IsInTopic(string topic, string memberId)
    {
        return await redis.GetDatabase().SetContainsAsync(Topic(topic), memberId);
    }
    
}

public class ServerHasAddedConnection() : BaseDto
{
    public string Message { get; set; }
}