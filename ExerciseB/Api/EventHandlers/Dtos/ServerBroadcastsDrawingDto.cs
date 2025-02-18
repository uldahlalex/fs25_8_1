using Api.Entities;
using WebSocketBoilerplate;

namespace Api.EventHandlers.Dtos;

public class ServerBroadcastsDrawingDto : BaseDto
{
    public string RoomId { get; set; }
    public DrawingAction Action { get; set; }
}