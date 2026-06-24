using System.Text;
using System.Text.Json;
using TrackMapRenderer.Models;

namespace TrackMapRenderer.Services;

public class LeafletHtmlBuilder
{
    private readonly string _templatePath;

    public LeafletHtmlBuilder(IWebHostEnvironment env)
    {
        _templatePath = Path.Combine(env.ContentRootPath, "Templates", "map-template.html");
    }

    public async Task<string> BuildHtmlAsync(RenderRequest request, string tileApiPath = "/api/tiles")
    {
        var template = await File.ReadAllTextAsync(_templatePath);

        var pointsJson = JsonSerializer.Serialize(request.Points.Select(p => new
        {
            lat = p.Lat,
            lon = p.Lon,
            label = p.Label ?? "",
            type = p.Type,
            timestamp = p.Timestamp ?? ""
        }));

        var sb = new StringBuilder();
        sb.Append("var config = {");
        sb.Append($"points: {pointsJson},");
        sb.Append($"showTrack: {JsonSerializer.Serialize(request.Options.ShowTrack)},");
        sb.Append($"showMarkers: {JsonSerializer.Serialize(request.Options.ShowMarkers)},");
        sb.Append($"showLabels: {JsonSerializer.Serialize(request.Options.ShowLabels)},");
        sb.Append($"autoFit: {JsonSerializer.Serialize(request.Options.AutoFit)},");
        sb.Append($"padding: {request.Options.Padding},");
        sb.Append($"mapTheme: {JsonSerializer.Serialize(request.Options.MapTheme)},");
        sb.Append($"tileUrl: {JsonSerializer.Serialize(tileApiPath + "/{z}/{x}/{y}.png")},");
        sb.Append($"title: {JsonSerializer.Serialize(request.Title ?? "")},");
        sb.Append($"subtitle: {JsonSerializer.Serialize(request.Subtitle ?? "")}");
        sb.Append("};");

        return template.Replace("/*CONFIG_PLACEHOLDER*/", sb.ToString());
    }
}
