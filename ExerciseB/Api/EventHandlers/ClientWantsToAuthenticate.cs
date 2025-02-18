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

public class ClientWantsToAuthenticate(IConnectionManager manager, ILogger<ClientWantsToAuthenticate> logger) 
    : BaseEventHandler<ClientWantsToAuthenticateDto> 
{
    public override async Task Handle(ClientWantsToAuthenticateDto dto, IWebSocketConnection socket)
    {
        var result = manager.SocketToConnectionId.TryGetValue(socket.ConnectionInfo.Id.ToString(), out var clientId);
        if(!result || clientId == null)
        {
            throw new InvalidOperationException("No client ID found for socket");
        }
        await manager.AddToTopic("authenticated", clientId);
        var topics = await manager.GetTopicsFromMemberId(clientId);
        
        logger.LogInformation(JsonSerializer.Serialize(manager.GetAllMembersWithTopics()));
        logger.LogInformation(JsonSerializer.Serialize(manager.GetAllTopicsWithMembers()));
        
        // Send response
        var response = new ServerAuthenticatesClientDto { Topics = topics, requestId = dto.requestId};
        socket.SendDto(response);
    }
}