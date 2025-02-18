using Api.EventHandlers.Dtos;
using Fleck;
using WebSocketBoilerplate;

namespace Api.EventHandlers;

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