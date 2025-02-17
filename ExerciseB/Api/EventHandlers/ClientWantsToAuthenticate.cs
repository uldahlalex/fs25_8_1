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

public class ClientWantsToAuthenticate(DictionaryConnectionManager manager) : BaseEventHandler<ClientWantsToAuthenticateDto> 
{
    public override async Task Handle(ClientWantsToAuthenticateDto dto, IWebSocketConnection socket)
    {
        var clientId = manager.TopicMembers[socket.ConnectionInfo.Id.ToString()].First();
        await manager.AddToTopic("authenticated", clientId);
        
        var topics = await manager.GetTopicsFromMemberId(clientId);
        var response = new ServerAuthenticatesClientDto { Topics = topics };
        socket.SendDto(response);
    }
}