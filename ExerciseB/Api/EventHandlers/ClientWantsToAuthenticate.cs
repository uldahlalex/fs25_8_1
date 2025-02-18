using System.Text.Json;
using Api.EventHandlers.Dtos;
using Fleck;
using WebSocketBoilerplate;

namespace Api.EventHandlers;

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
        
        var response = new ServerAuthenticatesClientDto { Topics = topics, requestId = dto.requestId};
        socket.SendDto(response);
    }
}