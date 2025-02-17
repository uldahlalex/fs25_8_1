using System.Text.Json;
using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ClientWantsToAuthenticateDto : BaseDto
{
  public string Username { get; set; }
}

public class ServerAuthenticatesClientDto : BaseDto
{
    public List<string> Topics { get; set; } = new List<string>();
}

public class ClientWantsToAuthenticate(IConnectionManager manager) : BaseEventHandler<ClientWantsToAuthenticateDto> 
{
    public override async Task Handle(ClientWantsToAuthenticateDto dto, IWebSocketConnection socket)
    {
        var clientId = await manager.LookupBySocketId(socket.ConnectionInfo.Id.ToString());
        if (clientId != null)
        {
            await manager.AddToTopic(ConnectionManager.Device("A"), clientId);
        }
        socket.SendDto(new ServerAuthenticatesClientDto()
        {
            Topics = await manager.GetTopicsFromMemberId(clientId)
            
        });
        await manager.BroadcastToTopic("user", JsonSerializer.Serialize(new ServerAuthenticatesClientDto() { }));
    }
}