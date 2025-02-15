using Fleck;

namespace ExerciseA;

public class ConnectionManager
{
    public async Task<object> OnOpen(IWebSocketConnection socket)
    {
        throw new NotImplementedException();
    }

    public async Task<object> OnClose(IWebSocketConnection socket)
    {
        throw new NotImplementedException();
    }

    public async Task Subscribe(IWebSocketConnection socket, string dtoTopic)
    {
        throw new NotImplementedException();
    }
}