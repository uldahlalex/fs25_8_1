using System.Text.Json.Serialization;

namespace Api.Entities;

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