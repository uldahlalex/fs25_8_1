using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class ServerBroadcastsDrawingDto : BaseDto
{
    public string RoomId { get; set; }
    public DrawingAction Action { get; set; }
}

public class ClientWantsToDrawDto : BaseDto
{
    [MinLength(1)]
    public string RoomId { get; set; }
    [Required]
    public DrawingAction Action { get; set; }
}

public class ServerConfirmsDrawDto : BaseDto
{
}

public class ClientWantsToDrawEventHandler(IConnectionManager connectionManager)
    : BaseEventHandler<ClientWantsToDrawDto>
{
    public override async Task Handle(ClientWantsToDrawDto dto, IWebSocketConnection socket)
    {
        var broadcast = new ServerBroadcastsDrawingDto()
        {
            Action = dto.Action,
            RoomId = dto.RoomId
        };
        await connectionManager.BroadcastToTopic(dto.RoomId, broadcast);
        var confirm = new ServerConfirmsDrawDto()
        {
            requestId = dto.requestId
        };
        socket.SendDto(confirm);

    }
}