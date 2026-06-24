using Microsoft.AspNetCore.Mvc;
using TrackMapRenderer.Models;
using TrackMapRenderer.Services;

namespace TrackMapRenderer.Controllers;

[ApiController]
public class MapController : ControllerBase
{
    private readonly MapRenderService _renderService;
    private readonly IConfiguration _configuration;

    public MapController(MapRenderService renderService, IConfiguration configuration)
    {
        _renderService = renderService;
        _configuration = configuration;
    }

    private string GetInternalBaseUrl()
    {
        return _configuration.GetValue<string>("MapRender:InternalBaseUrl")
            ?? "http://localhost:5000";
    }

    [HttpPost("api/map/render")]
    public async Task<IActionResult> Render([FromBody] RenderRequest request)
    {
        var imageBytes = await _renderService.RenderAsync(request, GetInternalBaseUrl());
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

        var imageBytes = await _renderService.RenderAsync(request, GetInternalBaseUrl());
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
            Title = "Debug: Kazakhstan → China",
            Subtitle = "Template rendering test",
            Width = 900,
            Height = 600,
            Points = new List<RoutePoint>
            {
                new() { Lat = 43.254841666666664, Lon = 76.85664166666666, Label = "Казахстан", Type = "start" },
                new() { Lat = 30.58, Lon = 103.98, Label = "Китай", Type = "finish" }
            },
            Options = new RenderOptions()
        };

        var html = await _renderService.BuildHtmlAsync(request);
        return Content(html, "text/html");
    }
}
