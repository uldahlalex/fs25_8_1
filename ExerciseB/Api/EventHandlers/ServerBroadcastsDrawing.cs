using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
using Fleck;
using WebSocketBoilerplate;

namespace ExerciseA.EventHandlers;

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DrawingTool
{
    [JsonPropertyName("pencil")]
    Pencil,
    
    [JsonPropertyName("circle")]
    Circle,
    
    [JsonPropertyName("square")]
    Square,
    
    [JsonPropertyName("text")]
    Text,
    
    [JsonPropertyName("eraser")]
    Eraser
}

public class DrawingAction
{
    public DrawingTool Tool { get; set; }
    public string Color { get; set; }
    public double LineWidth { get; set; }
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
}

public class ClientWantsToDrawDto : BaseDto
{
    public string RoomId { get; set; }
    public DrawingAction Action { get; set; }
}

public class ServerBroadcastsDrawingDto : BaseDto
{
    public string RoomId { get; set; }
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