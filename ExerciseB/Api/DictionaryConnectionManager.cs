using System.Collections.Concurrent;
using Fleck;

namespace Api;

public class DictionaryConnectionManager// : IConnectionManager
{
    public ConcurrentDictionary<string /* Connection ID */, IWebSocketConnection /* Sockets */> Sockets { get; } = new();
    /// <summary>
    /// Lookup(key) = Topic ID, value = hashset of Connection IDs 
    /// </summary>
    public ConcurrentDictionary<string /* Topic ID */, HashSet<string> /* All Connection IDs connected to topic */> TopicMembers { get; set; } = new();
   
    /// <summary>
    /// Lookup (key) = Connection ID, value = hashset of topic IDs
    /// </summary>
    // public ConcurrentDictionary<string /* Connection ID */, HashSet<string> /* all the topic ID's they are connected to */> MemberTopics { get; set; } = new();

    public string[] TopicIds = new[] { "sockets", "device/A", "room/A" }; //could be persisted in a database

    public DictionaryConnectionManager()
    {
        foreach (var topicId in TopicIds)
        {
            TopicMembers.TryAdd(topicId, new HashSet<string>());
        }

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
        TopicMembers.AddOrUpdate(memberId, new HashSet<string> { topic }, (_, existing) =>
        {
            existing.Add(topic);
            return existing;
        });

        // MemberTopics.AddOrUpdate(
        //     memberId,
        //     new HashSet<string> { topic },
        //     (_, existing) =>
        //     {
        //         existing.Add(topic);
        //         return existing;
        //     });

        return Task.CompletedTask;
    }

    public Task RemoveFromTopic(string topic, string memberId)
    {
        if (TopicMembers.TryGetValue(topic, out var members))
        {
            members.Remove(memberId);
        }

        // if (MemberTopics.TryGetValue(memberId, out var topics))
        // {
        //     topics.Remove(topic);
        // }
        TopicMembers.Remove(memberId, out _);

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
        // return Task.FromResult(
        //     MemberTopics.TryGetValue(memberId, out var topics) 
        //         ? topics.ToList() 
        //         : new List<string>());
        return Task.FromResult(TopicMembers[memberId].ToList());
    }

    public Task OnOpen(IWebSocketConnection socket, string clientId)
    {
        var success = Sockets.TryAdd(clientId, socket);
        if (!success)
            throw new Exception("Failed to add socket " + socket.ConnectionInfo.Id +
                                " to dictionary with client ID key " + clientId);
        AddToTopic("sockets", clientId);
        AddToTopic(socket.ConnectionInfo.Id.ToString(), clientId);
        return Task.CompletedTask;
    }
    

    public Task OnClose(IWebSocketConnection socket, string clientId)
    {
        Sockets.TryRemove(clientId, out _);
        //
        // if (MemberTopics.TryGetValue(clientId, out var topics))
        // {
        //     foreach (var topic in topics)
        //     {
        //         RemoveFromTopic(topic, clientId);
        //     }
        // }
        //
        // MemberTopics.TryRemove(clientId, out _);
        TopicMembers.TryRemove(clientId, out _);
        TopicMembers.Keys.ToList().ForEach(topic =>
        {
            TopicMembers[topic].Remove(clientId);
        });
        return Task.CompletedTask;
    }

    // public Task<string?> LookupBySocketId(string socketId)
    // {
    //     var pair = Sockets.FirstOrDefault(kvp => 
    //         kvp.Value.ConnectionInfo.Id.ToString() == socketId);
    //     return Task.FromResult(pair.Key);
    // }

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

    // public Task<bool> IsInTopic(string topic, string memberId)
    // {
    //     return Task.FromResult(
    //         TopicMembers.TryGetValue(topic, out var members) && 
    //         members.Contains(memberId));
    // }
}