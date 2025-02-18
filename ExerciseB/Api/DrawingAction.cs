namespace Api;
using System.Drawing;

public class DrawingAction
{
    DrawingTool Tool { get; set; }
    string Color { get; set; }
    float LineWidth { get; set; }
    Point StartPoint { get; set; }
    Point EndPoint { get; set; }
}

public record DrawingTool
{
    private DrawingTool(string value) => Value = value;
    public string Value { get; }

    public static DrawingTool Pencil => new("pencil");
    public static DrawingTool Circle => new("circle");
    public static DrawingTool Square => new("square");
    public static DrawingTool Text => new("text");
    public static DrawingTool Eraser => new("eraser");
}

public class Point
{
    public int x { get; set; }
    public int y { get; set; }
}