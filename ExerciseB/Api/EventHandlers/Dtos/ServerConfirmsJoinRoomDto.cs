using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ServerConfirmsJoinRoomDto : BaseDto
{
    public string RoomId { get; set; }
    public bool Success { get; set; }
}