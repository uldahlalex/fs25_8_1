using System.ComponentModel.DataAnnotations;
using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ClientWantsToJoinRoomDto : BaseDto
{
    [MinLength(1)]
    public string RoomId { get; set; }   
}

public class ServerConfirmsJoinRoomDto : BaseDto
{
    public string RoomId { get; set; }
    public bool Success { get; set; }
}


public class ClientWantsToJoinRoomEventHandler(IConnectionManager connectionManager) : BaseEventHandler<ClientWantsToJoinRoomDto>
{
    public override async Task Handle(ClientWantsToJoinRoomDto dto, IWebSocketConnection socket)
    {
        var result = connectionManager.SocketToConnectionId.TryGetValue(socket.ConnectionInfo.Id.ToString(), out var clientId);
        if(!result || clientId == null)
        {
            throw new InvalidOperationException("No client ID found for socket " + clientId);
        }
        await connectionManager.AddToTopic(dto.RoomId, 
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
