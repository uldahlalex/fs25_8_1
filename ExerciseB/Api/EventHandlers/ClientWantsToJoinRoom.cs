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
        var members = await connectionManager
            .GetMembersFromTopicId(socket.ConnectionInfo.Id.ToString());
        var clientId = members.FirstOrDefault() ?? "Could not find member ID";
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
