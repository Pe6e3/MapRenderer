using System.Collections.Concurrent;
using TrackMapRenderer.Models;

namespace TrackMapRenderer.Services;

public class MapRenderService
{
    private readonly LeafletHtmlBuilder _htmlBuilder;
    private readonly PlaywrightService _playwright;
    private readonly ILogger<MapRenderService> _logger;

    private static readonly ConcurrentDictionary<string, RenderedPage> _renderedPages = new();

    public MapRenderService(LeafletHtmlBuilder htmlBuilder, PlaywrightService playwright, ILogger<MapRenderService> logger)
    {
        _htmlBuilder = htmlBuilder;
        _playwright = playwright;
        _logger = logger;
    }

    public void ValidateRequest(RenderRequest request)
    {
        if (request.Points.Count < 2)
            throw new ArgumentException("At least 2 points are required");
        if (request.Points.Count > 100)
            throw new ArgumentException("Maximum 100 points allowed");

        foreach (var point in request.Points)
        {
            if (point.Lat < -90 || point.Lat > 90)
                throw new ArgumentException($"Invalid latitude: {point.Lat}. Must be between -90 and 90");
            if (point.Lon < -180 || point.Lon > 180)
                throw new ArgumentException($"Invalid longitude: {point.Lon}. Must be between -180 and 180");
            if (point.Label?.Length > 100)
                throw new ArgumentException("Label maximum length is 100 characters");
        }
    }

    public Task<string> BuildHtmlAsync(RenderRequest request)
    {
        return _htmlBuilder.BuildHtmlAsync(request);
    }

    public async Task<byte[]> RenderAsync(RenderRequest request, string internalBaseUrl)
    {
        ValidateRequest(request);

        var renderId = Guid.NewGuid().ToString("N");
        var html = await _htmlBuilder.BuildHtmlAsync(request);

        _renderedPages[renderId] = new RenderedPage(html, DateTime.UtcNow);

        try
        {
            var url = $"{internalBaseUrl}/_internal/render/{renderId}";
            _logger.LogInformation("Rendering map {RenderId} at {Url}", renderId, url);

            var screenshot = await _playwright.CaptureScreenshotAsync(url, request.Width, request.Height);
            return screenshot;
        }
        finally
        {
            _renderedPages.TryRemove(renderId, out _);
        }
    }

    public static string? GetRenderedHtml(string renderId)
    {
        if (_renderedPages.TryGetValue(renderId, out var page))
        {
            if (DateTime.UtcNow - page.CreatedAt < TimeSpan.FromSeconds(30))
                return page.Html;

            _renderedPages.TryRemove(renderId, out _);
        }
        return null;
    }

    private record RenderedPage(string Html, DateTime CreatedAt);
}
