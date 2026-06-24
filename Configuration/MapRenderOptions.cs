namespace TrackMapRenderer.Configuration;

public class MapRenderOptions
{
    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public string DarkTileUrl { get; set; } = "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png";
    public string TileCachePath { get; set; } = "./tile-cache";
    public int TileCacheTtlDays { get; set; } = 30;
    public bool EnableTileCache { get; set; } = true;
    public string? InternalBaseUrl { get; set; }
    public int Width { get; set; } = 900;
    public int Height { get; set; } = 600;
    public bool ShowTrack { get; set; } = true;
    public bool ShowMarkers { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public bool AutoFit { get; set; } = true;
    public int Padding { get; set; } = 80;
    public string MapTheme { get; set; } = "light";
}
