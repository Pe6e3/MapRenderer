using Microsoft.Playwright;

namespace TrackMapRenderer.Services;

public class PlaywrightService : IAsyncDisposable
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly SemaphoreSlim _semaphore = new(4, 4);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized;
    private readonly object _lock = new();

    public PlaywrightService(ILogger<PlaywrightService> logger)
    {
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-gpu" }
        });

        _initialized = true;
        _logger.LogInformation("Playwright browser initialized");
    }

    public async Task<byte[]> CaptureScreenshotAsync(string url, int width, int height, int timeoutMs = 60000)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync();

        IPage? page = null;
        try
        {
            page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });

            page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                    _logger.LogWarning("Browser console error: {Text}", msg.Text);
                else
                    _logger.LogDebug("Browser console [{Type}]: {Text}", msg.Type, msg.Text);
            };

            page.PageError += (_, error) =>
            {
                _logger.LogError("Browser page error: {Error}", error);
            };

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = timeoutMs
            });

            _logger.LogDebug("Page loaded, waiting for map ready signal...");

            await page.WaitForFunctionAsync("window.__mapReady === true", new PageWaitForFunctionOptions
            {
                Timeout = timeoutMs
            });

            _logger.LogDebug("Map ready, taking screenshot...");

            await page.WaitForTimeoutAsync(500);

            var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = false
            });

            return screenshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Screenshot capture failed for {Url}", url);

            if (page != null)
            {
                try
                {
                    var debugHtml = await page.EvaluateAsync<string>("document.documentElement.outerHTML");
                    _logger.LogDebug("Page HTML at failure:\n{Html}", debugHtml[..Math.Min(debugHtml.Length, 2000)]);
                }
                catch { /* ignore debug errors */ }
            }

            throw;
        }
        finally
        {
            if (page != null)
                await page.CloseAsync();
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
        _semaphore.Dispose();
    }
}
