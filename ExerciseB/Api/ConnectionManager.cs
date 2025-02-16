using System.Collections.Concurrent;
using Fleck;
using StackExchange.Redis;

namespace Api;

public class ConnectionManager(IDatabase redis)
{
    public ConcurrentDictionary<string, IWebSocketConnection> Sockets { get; set; }
    
    public async Task<object> OnOpen(IWebSocketConnection socket, string customConnectionId)
    {
        Sockets.TryAdd(socket.ConnectionInfo.Id.ToString(), socket);
        
    }

    public async Task<object> OnClose(IWebSocketConnection socket, string customConnectionId)
    {
        throw new NotImplementedException();
    }

    public async Task Subscribe(IWebSocketConnection socket, string topic)
    {
        throw new NotImplementedException();
    }
    
    public async Task Unsubscribe(IWebSocketConnection socket, string topic)
    {
        throw new NotImplementedException();
    }
    
    public async Task BroadcastToAllTopicSubscribers(string topic)
    {
        throw new NotImplementedException();
    }
}