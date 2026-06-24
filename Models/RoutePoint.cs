namespace TrackMapRenderer.Models;

public class RoutePoint
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? Label { get; set; }
    public string Type { get; set; } = "middle";
    public string? Timestamp { get; set; }
}
