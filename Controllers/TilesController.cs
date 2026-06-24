using Microsoft.AspNetCore.Mvc;
using TrackMapRenderer.Services;

namespace TrackMapRenderer.Controllers;

[ApiController]
public class TilesController : ControllerBase
{
    private readonly TileProxyService _tileProxy;

    public TilesController(TileProxyService tileProxy)
    {
        _tileProxy = tileProxy;
    }

    [HttpGet("api/tiles/{z}/{x}/{y}.png")]
    public async Task<IActionResult> GetTile(int z, int x, int y)
    {
        if (!TileProxyService.ValidateTileCoords(z, x, y))
        {
            return BadRequest(new { error = "Invalid tile coordinates" });
        }

        var tile = await _tileProxy.GetTileAsync(z, x, y);
        return File(tile, "image/png");
    }
}
