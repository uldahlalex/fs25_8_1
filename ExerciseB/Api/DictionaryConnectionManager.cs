using System.Collections.Concurrent;
using System.Text.Json;
using Fleck;
using WebSocketBoilerplate;

namespace Api;

public class DictionaryConnectionManager : IConnectionManager
{
    public ConcurrentDictionary<string /* Connection ID */, ConcurrentDictionary<string /* Socket ID */, IWebSocketConnection> /* Sockets */> ConnectionIdToSockets { get; } = new();
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

    public Task<Dictionary<string, List<string>>> GetAllConnectionIdsWithSocketId()
    {
        return Task.FromResult(
            ConnectionIdToSockets.ToDictionary(
                k => k.Key, 
                v => v.Value.Keys.ToList()
            )
        );
    }
    
    public Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId()
    {
        return Task.FromResult(SocketToConnectionId.ToDictionary(k => k.Key, v => v.Value));
    }

    public async Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null)
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

        await LogCurrentState();
        
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
        _logger.LogInformation($"OnOpen called with clientId: {clientId} and socketId: {socket.ConnectionInfo.Id}");

        // Add or get the dictionary of sockets for this client
        var clientSockets = ConnectionIdToSockets.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, IWebSocketConnection>());
        
        // Add this socket to the client's sockets
        clientSockets.TryAdd(socket.ConnectionInfo.Id.ToString(), socket);
        
        // Add to socket mapping
        SocketToConnectionId.TryAdd(socket.ConnectionInfo.Id.ToString(), clientId);

        _logger.LogInformation($"Client {clientId} now has {clientSockets.Count} active connections");
        
        await LogCurrentState();
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        var socketId = socket.ConnectionInfo.Id.ToString();
        
        // Remove from socket mapping
        SocketToConnectionId.TryRemove(socketId, out _);

        // Remove from client sockets
        if (ConnectionIdToSockets.TryGetValue(clientId, out var clientSockets))
        {
            clientSockets.TryRemove(socketId, out _);
            
            // If this was the last socket for this client, remove the client entirely
            if (clientSockets.IsEmpty)
            {
                ConnectionIdToSockets.TryRemove(clientId, out _);
                
                // Clean up topics
                if (MemberTopics.TryGetValue(clientId, out var topics))
                {
                    foreach (var topic in topics)
                    {
                        await RemoveFromTopic(topic, clientId);
                    }
                }
                MemberTopics.TryRemove(clientId, out _);
            }
        }
    }

    public async Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        _logger.LogInformation($"Starting broadcast to topic: {topic}");
    
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            _logger.LogInformation($"Found {members.Count} members in topic {topic}: {string.Join(", ", members)}");
            
            foreach (var memberId in members.ToList())
            {
                if (ConnectionIdToSockets.TryGetValue(memberId, out var clientSockets))
                {
                    var successfulBroadcast = false;
                    
                    foreach (var socket in clientSockets.Values)
                    {
                        try
                        {
                            if (socket.IsAvailable)
                            {
                                socket.SendDto(message);
                                successfulBroadcast = true;
                                _logger.LogInformation($"Successfully sent message to {memberId} via socket {socket.ConnectionInfo.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send message to socket {socket.ConnectionInfo.Id} for member {memberId}");
                        }
                    }

                    if (!successfulBroadcast)
                    {
                        _logger.LogWarning($"Failed to broadcast to any sockets for member {memberId}");
                        await RemoveFromTopic(topic, memberId);
                    }
                }
                else
                {
                    _logger.LogWarning($"No sockets found for member: {memberId}");
                    await RemoveFromTopic(topic, memberId);
                }
            }
        }
        else
        {
            _logger.LogWarning($"No topic found: {topic}");
        }
    }


    public async Task LogCurrentState()
    {
        _logger.LogInformation("Current state:");
        _logger.LogInformation("ConnectionIdToSocket:");
        _logger.LogInformation(JsonSerializer.Serialize(new
        {
            ConnectionIdToSocket = await GetAllConnectionIdsWithSocketId(),
            SocketToCnnectionId = await GetAllSocketIdsWithConnectionId(),
            TopicsWithMembers = await GetAllTopicsWithMembers(),
            MembersWithTopics = await GetAllMembersWithTopics()
            
        }, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
      
    }


}