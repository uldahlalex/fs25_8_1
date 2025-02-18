using System.Collections.Concurrent;
using System.Text.Json;
using Fleck;
using WebSocketBoilerplate;

namespace Api;

public class DictionaryConnectionManager : IConnectionManager
{
    public ConcurrentDictionary<string /* Connection ID */, IWebSocketConnection /* Sockets */> Sockets { get; } = new();


    /// <summary>
    /// Lookup(key) = Topic ID, value = hashset of Connection IDs 
    /// </summary>
    public ConcurrentDictionary<string /* Topic ID */, HashSet<string> /* All Connection IDs connected to topic */> TopicMembers { get; set; } = new();
   
    /// <summary>
    /// Lookup (key) = Connection ID, value = hashset of topic IDs
    /// </summary>
    public ConcurrentDictionary<string /* Connection ID */, HashSet<string> /* all the topic ID's they are connected to */> MemberTopics { get; set; } = new();

    private string[] InitialTopicIds = new[] { "device/A", "room/A" }; //could be persisted in a database
    private readonly ILogger<DictionaryConnectionManager> _logger;

    public DictionaryConnectionManager(ILogger<DictionaryConnectionManager> logger)
    {
        _logger = logger;
        foreach (var topicId in InitialTopicIds)
        {
            TopicMembers.TryAdd(topicId, new HashSet<string>());
        }

    }
    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers()
    {
        return Task.FromResult(TopicMembers);
    }

    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics()
    {
        return Task.FromResult(MemberTopics);
    }

    public Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        TopicMembers.AddOrUpdate(
            topic,
            new HashSet<string> { memberId },
            (_, existing) =>
            {
                existing.Add(memberId);
                return existing;
            });

        MemberTopics.AddOrUpdate(
            memberId,
            new HashSet<string> { topic },
            (_, existing) =>
            {
                existing.Add(topic);
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task RemoveFromTopic(string topic, string memberId)
    {
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            members.Remove(memberId);
        }

        if (MemberTopics.TryGetValue(memberId, out var topics))
        {
            topics.Remove(topic);
        }

        return Task.CompletedTask;
    }

    public Task<List<string>> GetMembersFromTopicId(string topic)
    {
        return Task.FromResult(
            TopicMembers.TryGetValue(topic, out var members) 
                ? members.ToList() 
                : new List<string>());
    }

    public Task<List<string>> GetTopicsFromMemberId(string memberId)
    {
        return Task.FromResult(
            MemberTopics.TryGetValue(memberId, out var topics) 
                ? topics.ToList() 
                : new List<string>());
    }

    public Task OnOpen(IWebSocketConnection socket, string clientId)
    {
        Sockets.AddOrUpdate(clientId, socket, (_, _) => socket);
        AddToTopic(socket.ConnectionInfo.Id.ToString(), clientId);
        AddToTopic(clientId, socket.ConnectionInfo.Id.ToString());
        _logger.LogInformation("Connected with client ID " + clientId + " and socket ID " + socket.ConnectionInfo.Id);
        return Task.CompletedTask;
    }
    

    public Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);
        
        if (MemberTopics.TryGetValue(clientId, out var topics))
        {
            foreach (var topic in topics)
            {
                RemoveFromTopic(topic, clientId);
            }
        }

        MemberTopics.TryRemove(clientId, out _);
        return Task.CompletedTask;
    }

 

    public Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        _logger.LogInformation("Topics with members: "+JsonSerializer.Serialize(GetAllTopicsWithMembers(), new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            _logger.LogInformation("Found members: "+JsonSerializer.Serialize(members, new JsonSerializerOptions()
            {
                WriteIndented = true
            }));
            foreach (var memberId in members)
            {
                if (Sockets.TryGetValue(memberId, out var socket))
                {
                    Console.WriteLine(memberId + " and socket: "+socket);
                    socket.SendDto(message);
                }
            }
        }
        return Task.CompletedTask;
    }


}