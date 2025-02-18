using WebSocketBoilerplate;

namespace Api.EventHandlers;

public class ClientWantsToleaveRoomDto : BaseDto
{
    public string RoomId { get; set; }
}