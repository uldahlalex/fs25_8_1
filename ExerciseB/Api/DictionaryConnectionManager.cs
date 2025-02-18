using System.Collections.Concurrent;
using System.Text.Json;
using Fleck;
using WebSocketBoilerplate;

namespace Api;

public class DictionaryConnectionManager : IConnectionManager
{
    public ConcurrentDictionary<string /* Client ID */, IWebSocketConnection> ConnectionIdToSocket { get; } = new();
    public ConcurrentDictionary<string /* Socket ID */, string /* Client ID */> SocketToConnectionId { get; } = new();
    public ConcurrentDictionary<string, HashSet<string>> TopicMembers { get; set; } = new();
    public ConcurrentDictionary<string, HashSet<string>> MemberTopics { get; set; } = new();


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
        return Task.FromResult(ConnectionIdToSocket.ToDictionary(k => k.Key, v => v.Value.ConnectionInfo.Id.ToString())
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

        // If there's an existing connection for this client, clean it up first
        if (ConnectionIdToSocket.TryRemove(clientId, out var oldSocket))
        {
            var oldSocketId = oldSocket.ConnectionInfo.Id.ToString();
            SocketToConnectionId.TryRemove(oldSocketId, out _);
            _logger.LogInformation($"Removed old connection {oldSocketId} for client {clientId}");
        }

        // Add new connection
        ConnectionIdToSocket[clientId] = socket;
        SocketToConnectionId[socket.ConnectionInfo.Id.ToString()] = clientId;

        _logger.LogInformation($"Added new connection {socket.ConnectionInfo.Id} for client {clientId}");
        await LogCurrentState();
    }

    public async Task OnClose(IWebSocketConnection socket, string clientId)
    {
        var socketId = socket.ConnectionInfo.Id.ToString();
        
        // Only remove if this is the current socket for this client
        if (ConnectionIdToSocket.TryGetValue(clientId, out var currentSocket) && 
            currentSocket.ConnectionInfo.Id.ToString() == socketId)
        {
            ConnectionIdToSocket.TryRemove(clientId, out _);
            _logger.LogInformation($"Removed connection for client {clientId}");
        }

        SocketToConnectionId.TryRemove(socketId, out _);

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

    public async Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto
    {
        _logger.LogInformation($"Starting broadcast to topic: {topic}");
    
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            _logger.LogInformation($"Found {members.Count} members in topic {topic}: {string.Join(", ", members)}");
            
            foreach (var memberId in members.ToList())
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