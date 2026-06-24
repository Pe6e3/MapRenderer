namespace TrackMapRenderer.Models;

public class RenderRequest
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 600;
    public List<RoutePoint> Points { get; set; } = new();
    public RenderOptions Options { get; set; } = new();
}
