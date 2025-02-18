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
        // Get the client ID from the socket's topic
        var members = await manager.GetTopicsFromMemberId(socket.ConnectionInfo.Id.ToString());
        var clientId = members.FirstOrDefault() ?? 
                       throw new InvalidOperationException($"No client ID found for socket {socket.ConnectionInfo.Id}");

        await manager.AddToTopic("authenticated", clientId);
        var topics = await manager.GetTopicsFromMemberId(clientId);
        
        logger.LogInformation(JsonSerializer.Serialize(manager.GetAllMembersWithTopics()));
        logger.LogInformation(JsonSerializer.Serialize(manager.GetAllTopicsWithMembers()));
        
        // Send response
        var response = new ServerAuthenticatesClientDto { Topics = topics, requestId = dto.requestId};
        socket.SendDto(response);
    }
}