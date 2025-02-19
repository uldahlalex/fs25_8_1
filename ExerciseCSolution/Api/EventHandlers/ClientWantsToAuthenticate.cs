using System.Security.Authentication;
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

        if (!Login(dto)) throw new AuthenticationException("Invalid login!");
        
        await manager.AddToTopic("authenticated", clientId);
        var topics = await manager.GetTopicsFromMemberId(clientId);
        
        var response = new ServerAuthenticatesClientDto { Topics = topics, requestId = dto.requestId};
        socket.SendDto(response);
    }

    private bool Login(ClientWantsToAuthenticateDto dto)
    {
        //imagine there is a login here
        return true;
    }
}