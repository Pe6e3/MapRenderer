using Microsoft.Extensions.Options;
using TrackMapRenderer.Configuration;

namespace TrackMapRenderer.Services;

public class TileCacheService
{
    private readonly MapRenderOptions _options;
    private readonly ILogger<TileCacheService> _logger;

    public TileCacheService(IOptions<MapRenderOptions> options, ILogger<TileCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetCachePath(int z, int x, int y)
    {
        return Path.Combine(_options.TileCachePath, z.ToString(), x.ToString(), $"{y}.png");
    }

    public byte[]? GetCachedTile(int z, int x, int y)
    {
        if (!_options.EnableTileCache)
            return null;

        var path = GetCachePath(z, x, y);
        if (!File.Exists(path))
            return null;

        var fileInfo = new FileInfo(path);
        var age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
        if (age.TotalDays > _options.TileCacheTtlDays)
        {
            _logger.LogDebug("Tile cache expired for {Z}/{X}/{Y}", z, x, y);
            return null;
        }

        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cached tile {Z}/{X}/{Y}", z, x, y);
            return null;
        }
    }

    public void SaveTile(int z, int x, int y, byte[] data)
    {
        if (!_options.EnableTileCache)
            return;

        var path = GetCachePath(z, x, y);
        var dir = Path.GetDirectoryName(path)!;

        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save tile to cache {Z}/{X}/{Y}", z, x, y);
        }
    }
}
