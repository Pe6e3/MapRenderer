using TrackMapRenderer.Configuration;
using TrackMapRenderer.Middleware;
using TrackMapRenderer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MapRenderOptions>(builder.Configuration.GetSection("MapRender"));

builder.Services.AddHttpClient("TileDownloader", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TrackMapRenderer/1.0");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<PlaywrightService>();
builder.Services.AddSingleton<TileCacheService>();
builder.Services.AddScoped<TileProxyService>();
builder.Services.AddScoped<LeafletHtmlBuilder>();
builder.Services.AddScoped<MapRenderService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
