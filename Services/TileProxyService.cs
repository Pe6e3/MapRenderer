using System.Net;
using Microsoft.Extensions.Options;
using TrackMapRenderer.Configuration;

namespace TrackMapRenderer.Services;

public class TileProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TileCacheService _cacheService;
    private readonly MapRenderOptions _options;
    private readonly ILogger<TileProxyService> _logger;

    private static readonly byte[] FallbackTile = GenerateFallbackTile();

    public TileProxyService(
        IHttpClientFactory httpClientFactory,
        TileCacheService cacheService,
        IOptions<MapRenderOptions> options,
        ILogger<TileProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cacheService = cacheService;
        _options = options.Value;
        _logger = logger;
    }

    public static bool ValidateTileCoords(int z, int x, int y)
    {
        if (z < 0 || z > 19) return false;
        var max = (int)Math.Pow(2, z);
        return x >= 0 && x < max && y >= 0 && y < max;
    }

    public async Task<byte[]> GetTileAsync(int z, int x, int y)
    {
        var cached = _cacheService.GetCachedTile(z, x, y);
        if (cached != null)
            return cached;

        try
        {
            var tileUrl = _options.TileUrl
                .Replace("{z}", z.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString());

            var client = _httpClientFactory.CreateClient("TileDownloader");
            var response = await client.GetAsync(tileUrl);

            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Tile download rate-limited for {Z}/{X}/{Y}: {Status}", z, x, y, response.StatusCode);
                return FallbackTile;
            }

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();

            _cacheService.SaveTile(z, x, y, data);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download tile {Z}/{X}/{Y}", z, x, y);
            return FallbackTile;
        }
    }

    private static byte[] GenerateFallbackTile()
    {
        // Minimal valid 256x256 transparent PNG
        // PNG header + IHDR + transparent IDAT + IEND
        var header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var ihdr = new byte[] {
            0x00, 0x00, 0x00, 0x0D, // length
            0x49, 0x48, 0x44, 0x52, // type
            0x00, 0x00, 0x01, 0x00, // width 256
            0x00, 0x00, 0x01, 0x00, // height 256
            0x08, // bit depth
            0x06, // color type (RGBA)
            0x00, // compression
            0x00, // filter
            0x00, // interlace
            0x5B, 0x93, 0x4B, 0x3E  // CRC
        };

        // For simplicity, return a 1x1 transparent PNG and let the browser scale it
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVQI12NgAAIABQABNjN9GQAAAAlwSFlzAAAWJQAAFiUBSVIk8AAAAA0lEQVQI12P4z8BQDwAEgAF/QualzQAAAABJRU5ErkJggg==");
    }
}
