using System.Globalization;
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
        [FromQuery] string fromLat,
        [FromQuery] string fromLon,
        [FromQuery] string toLat,
        [FromQuery] string toLon,
        [FromQuery] string? fromLabel,
        [FromQuery] string? fromTs,
        [FromQuery] string? toLabel,
        [FromQuery] string? toTs,
        [FromQuery] string[]? mid)
    {
        if (!TryParseCoordinate(fromLat, out var fLat) ||
            !TryParseCoordinate(fromLon, out var fLon) ||
            !TryParseCoordinate(toLat, out var tLat) ||
            !TryParseCoordinate(toLon, out var tLon))
        {
            return BadRequest(new { error = "Invalid coordinate format. Use dot (48.0196) or comma (48,0196) as decimal separator." });
        }

        var points = new List<RoutePoint>
        {
            new() { Lat = fLat, Lon = fLon, Label = fromLabel, Type = "start", Timestamp = fromTs }
        };

        if (mid != null)
        {
            foreach (var m in mid)
            {
                var parts = m.Split(',', 3, StringSplitOptions.TrimEntries);
                if (parts.Length < 2) continue;

                if (!TryParseCoordinate(parts[0], out var lat) ||
                    !TryParseCoordinate(parts[1], out var lon))
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

        points.Add(new() { Lat = tLat, Lon = tLon, Label = toLabel, Type = "finish", Timestamp = toTs });

        var request = new RenderRequest
        {
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

    private static bool TryParseCoordinate(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var normalized = value.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
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
}
