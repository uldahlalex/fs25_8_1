using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ClientWantsToAuthenticateDto : BaseDto
{
  public string Username { get; set; }
}

public class ServerAuthenticatesClient : BaseDto
{
    
}

public class ClientWantsToAuthenticate(ConnectionManager manager) : BaseEventHandler<ClientWantsToAuthenticateDto> 
{
    public override async Task Handle(ClientWantsToAuthenticateDto dto, IWebSocketConnection socket)
    {
        var uid = Guid.NewGuid().ToString();
        var clientId = await manager.LookupBySocketId(socket.ConnectionInfo.Id.ToString());
        if (clientId != null)
        {
            await manager.Subscribe(ConnectionManager.User(uid), clientId);
        }
    }
}