using System.Collections.Concurrent;
using Fleck;

namespace Api;

public class DictionaryConnectionManager : IConnectionManager
{
    public ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; } = new();
    public ConcurrentDictionary<string, HashSet<string>> TopicMembers { get; set; } = new();
    public ConcurrentDictionary<string, HashSet<string>> MemberTopics { get; set; } = new();
    
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
        Sockets.TryAdd(clientId, socket);
        socket.Send("Connected");
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

    public Task<string?> LookupBySocketId(string socketId)
    {
        var pair = Sockets.FirstOrDefault(kvp => 
            kvp.Value.ConnectionInfo.Id.ToString() == socketId);
        return Task.FromResult(pair.Key);
    }

    public async Task BroadcastToTopic(string topic, string message)
    {
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            foreach (var memberId in members)
            {
                if (Sockets.TryGetValue(memberId, out var socket))
                {
                    socket.Send(message);
                }
            }
        }
    }

    public Task<bool> IsInTopic(string topic, string memberId)
    {
        return Task.FromResult(
            TopicMembers.TryGetValue(topic, out var members) && 
            members.Contains(memberId));
    }
}