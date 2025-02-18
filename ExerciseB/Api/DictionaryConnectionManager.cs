using System.Collections.Concurrent;
using System.Text.Json;
using Fleck;
using WebSocketBoilerplate;

namespace Api;

public class DictionaryConnectionManager : IConnectionManager
{
    public ConcurrentDictionary<string /* Connection ID */, IWebSocketConnection /* Sockets */> ConnectionIdToSocket { get; } = new();
    public ConcurrentDictionary<string /* Socket ID */, string /* Connection ID */> SocketToConnectionId { get; } = new();

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

    public Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId()
    {
        return Task.FromResult(ConnectionIdToSocket.ToDictionary(k => k.Key, v => v.Value.ConnectionInfo.Id.ToString()));
    }

    public Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
    {
        TopicMembers.AddOrUpdate(
            topic,
            _ => new HashSet<string> { memberId },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(memberId);
                    return existing;
                }
            });

        MemberTopics.AddOrUpdate(
            memberId,
            _ => new HashSet<string> { topic },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(topic);
                    return existing;
                }
            });

        _logger.LogInformation($"Added member {memberId} to topic {topic}");
        return Task.CompletedTask;
    }

    public Task RemoveFromTopic(string topic, string memberId)
    {
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            lock (members)
            {
                members.Remove(memberId);
            }
        }

        if (MemberTopics.TryGetValue(memberId, out var topics))
        {
            lock (topics)
            {
                topics.Remove(topic);
            }
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

    public async Task OnOpen(IWebSocketConnection socket, string clientId)
    {
        ConnectionIdToSocket.AddOrUpdate(clientId, socket, (_, _) => socket);
        SocketToConnectionId.AddOrUpdate(socket.ConnectionInfo.Id.ToString(), clientId, (_, _) => clientId);
        _logger.LogInformation("Connected with client ID " + clientId + " and socket ID " + socket.ConnectionInfo.Id);
        _logger.LogInformation(JsonSerializer.Serialize(await GetAllConnectionIdsWithSocketId(), new JsonSerializerOptions()
        {
            WriteIndented = true
        }));

    }
    

    public Task OnClose(IWebSocketConnection socket, string clientId)
    {
        ConnectionIdToSocket.TryRemove(clientId, out _);
        SocketToConnectionId.TryRemove(socket.ConnectionInfo.Id.ToString(), out _);
        
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

 

    public async Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        _logger.LogInformation($"Attempting to broadcast to topic: {topic}");

        if (TopicMembers.TryGetValue(topic, out var members))
        {
            _logger.LogInformation($"Found {members.Count} members in topic {topic}");
        
            // Create a safe copy of members to iterate over
            List<string> membersList;
            lock (members)
            {
                membersList = members.ToList();
            }

            foreach (var memberId in membersList)
            {
                _logger.LogInformation($"Attempting to send to member: {memberId}");
            
                if (ConnectionIdToSocket.TryGetValue(memberId, out var socket))
                {
                    try
                    {
                        if (socket.IsAvailable) // Check if socket is still connected
                        {
                            socket.SendDto(message);
                            _logger.LogInformation($"Successfully sent message to {memberId}");
                        }
                        else
                        {
                            _logger.LogWarning($"Socket for {memberId} is no longer available");
                            // Clean up dead connection
                            await OnClose(socket, memberId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send message to {memberId}");
                        // Clean up failed connection
                        await OnClose(socket, memberId);
                    }
                }
                else
                {
                    _logger.LogWarning($"No socket found for member {memberId}");
                    // Clean up orphaned member
                    await RemoveFromTopic(topic, memberId);
                }
            }
        }
        else
        {
            _logger.LogWarning($"No members found for topic {topic}");
        }
    }


}