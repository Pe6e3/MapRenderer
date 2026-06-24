namespace TrackMapRenderer.Models;

public class RenderOptions
{
    public bool ShowTrack { get; set; } = true;
    public bool ShowMarkers { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public bool AutoFit { get; set; } = true;
    public int Padding { get; set; } = 80;
    public string MapTheme { get; set; } = "light";
}
