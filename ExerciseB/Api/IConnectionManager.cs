using System.Collections.Concurrent;
using Fleck;

namespace Api;

public interface IConnectionManager
{
    ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; }
    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers();
    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics();
    
    Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null);
    Task RemoveFromTopic(string topic, string memberId);
    Task<List<string>> GetMembersFromTopicId(string topic);
    Task<List<string>> GetTopicsFromMemberId(string memberId);
    Task OnOpen(IWebSocketConnection socket, string clientId);
    Task OnClose(IWebSocketConnection socket, string clientId);
    Task BroadcastToTopic(string topic, string message);
}