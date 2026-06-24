using Microsoft.AspNetCore.Mvc;
using TrackMapRenderer.Models;
using TrackMapRenderer.Services;

namespace TrackMapRenderer.Controllers;

[ApiController]
public class MapController : ControllerBase
{
    private readonly MapRenderService _renderService;

    public MapController(MapRenderService renderService)
    {
        _renderService = renderService;
    }

    [HttpPost("api/map/render")]
    public async Task<IActionResult> Render([FromBody] RenderRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var imageBytes = await _renderService.RenderAsync(request, baseUrl);
        return File(imageBytes, "image/png");
    }

    [HttpPost("api/map/render/test")]
    public async Task<IActionResult> RenderTest()
    {
        var request = new RenderRequest
        {
            Title = "Seal 8252595211",
            Subtitle = "Russia → Mozambique",
            Width = 900,
            Height = 600,
            Points = new List<RoutePoint>
            {
                new() { Lat = 51.563571, Lon = 38.396241, Label = "Russia", Type = "start" },
                new() { Lat = -14.083181, Lon = 36.419055, Label = "Mozambique", Type = "finish" }
            },
            Options = new RenderOptions()
        };

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var imageBytes = await _renderService.RenderAsync(request, baseUrl);
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
}
