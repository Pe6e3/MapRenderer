namespace TrackMapRenderer.Configuration;

public class MapRenderOptions
{
    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public string DarkTileUrl { get; set; } = "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png";
    public string TileCachePath { get; set; } = "./tile-cache";
    public int TileCacheTtlDays { get; set; } = 30;
    public bool EnableTileCache { get; set; } = true;
}
