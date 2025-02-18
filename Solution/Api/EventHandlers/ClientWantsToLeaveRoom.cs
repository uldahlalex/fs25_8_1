using Api.EventHandlers.Dtos;
using Fleck;
using WebSocketBoilerplate;

namespace Api.EventHandlers;


public class ClientWantsToLeaveRoomEventHandler(IConnectionManager connectionManager) : BaseEventHandler<ClientWantsToleaveRoomDto>
{
    public override async Task Handle(ClientWantsToleaveRoomDto dto, IWebSocketConnection socket)
    {
        var result = connectionManager.SocketToConnectionId.TryGetValue(socket.ConnectionInfo.Id.ToString(), out var clientId);
        if(!result || clientId == null)
        {
            throw new InvalidOperationException("No client ID found for socket " + clientId);
        }
        await connectionManager.RemoveFromTopic(dto.RoomId, 
            clientId);
        var response = new ServerConfirmsJoinRoomDto()
        {
            RoomId = dto.RoomId,
            Success = true,
            requestId = dto.requestId
        };
        socket.SendDto(response);
    }
}