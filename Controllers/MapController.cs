using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TrackMapRenderer.Configuration;
using TrackMapRenderer.Models;
using TrackMapRenderer.Services;

namespace TrackMapRenderer.Controllers;

[ApiController]
public class MapController : ControllerBase
{
    private readonly MapRenderService _renderService;
    private readonly MapRenderOptions _options;

    public MapController(MapRenderService renderService, IOptions<MapRenderOptions> options)
    {
        _renderService = renderService;
        _options = options.Value;
    }

    [HttpGet("api/map/render")]
    public async Task<IActionResult> Render(
        [FromQuery] double fromLat,
        [FromQuery] double fromLon,
        [FromQuery] double toLat,
        [FromQuery] double toLon,
        [FromQuery] string? fromLabel,
        [FromQuery] string? fromTs,
        [FromQuery] string? toLabel,
        [FromQuery] string? toTs,
        [FromQuery] string[]? mid)
    {
        var points = new List<RoutePoint>
        {
            new() { Lat = fromLat, Lon = fromLon, Label = fromLabel, Type = "start", Timestamp = fromTs }
        };

        if (mid != null)
        {
            foreach (var m in mid)
            {
                var parts = m.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 2) continue;

                if (!double.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var lon))
                    continue;

                points.Add(new RoutePoint
                {
                    Lat = lat,
                    Lon = lon,
                    Label = parts.Length > 2 ? parts[2] : null,
                    Type = "middle"
                });
            }
        }

        points.Add(new() { Lat = toLat, Lon = toLon, Label = toLabel, Type = "finish", Timestamp = toTs });

        var title = BuildTitle(fromLabel, toLabel);

        var request = new RenderRequest
        {
            Title = title,
            Points = points,
            Width = _options.Width,
            Height = _options.Height,
            Options = new RenderOptions
            {
                ShowTrack = _options.ShowTrack,
                ShowMarkers = _options.ShowMarkers,
                ShowLabels = _options.ShowLabels,
                AutoFit = _options.AutoFit,
                Padding = _options.Padding,
                MapTheme = _options.MapTheme
            }
        };

        var imageBytes = await _renderService.RenderAsync(request, _options.InternalBaseUrl ?? "http://localhost:5000");
        return File(imageBytes, "image/png");
    }

    [HttpGet("_internal/render/{renderId}")]
    public IActionResult GetRenderPage(string renderId)
    {
        var html = MapRenderService.GetRenderedHtml(renderId);
        if (html == null)
            return NotFound(new { error = "Render session not found or expired" });

        return Content(html, "text/html");
    }

    [HttpGet("debug/template")]
    public async Task<IActionResult> DebugTemplate()
    {
        var request = new RenderRequest
        {
            Title = "Kazakhstan → China",
            Points = new List<RoutePoint>
            {
                new() { Lat = 43.254841666666664, Lon = 76.85664166666666, Label = "Kazakhstan", Type = "start", Timestamp = "24.06.2026 10:33:50" },
                new() { Lat = 30.58, Lon = 103.98, Label = "China", Type = "finish", Timestamp = "24.06.2026 11:33:50" }
            },
            Width = _options.Width,
            Height = _options.Height,
            Options = new RenderOptions
            {
                ShowTrack = _options.ShowTrack,
                ShowMarkers = _options.ShowMarkers,
                ShowLabels = _options.ShowLabels,
                AutoFit = _options.AutoFit,
                Padding = _options.Padding,
                MapTheme = _options.MapTheme
            }
        };

        var html = await _renderService.BuildHtmlAsync(request);
        return Content(html, "text/html");
    }

    private static string BuildTitle(string? fromLabel, string? toLabel)
    {
        if (!string.IsNullOrEmpty(fromLabel) && !string.IsNullOrEmpty(toLabel))
            return $"{fromLabel} → {toLabel}";
        if (!string.IsNullOrEmpty(fromLabel))
            return fromLabel;
        if (!string.IsNullOrEmpty(toLabel))
            return toLabel;
        return "";
    }
}
