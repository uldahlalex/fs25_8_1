namespace Api.Entities;

public class DrawingAction
{
    public DrawingTool Tool { get; set; }
    public string Color { get; set; }
    public double LineWidth { get; set; }
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
}