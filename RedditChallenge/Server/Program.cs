using RedditChallenge.Shared.Repositories;
using RedditChallenge.Shared.Services;
using RedditChallenge.Server.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Optional: Log to console
    .WriteTo.File("Logs/app-log-.txt",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 5, // Keep up to 5 log files
        shared: true)
    .CreateLogger();

builder.Logging.ClearProviders(); // Remove default logging providers
builder.Host.UseSerilog(); 

builder.Services.AddSignalR();

builder.Services.AddRazorPages();

builder.Services.AddHttpClient("RedditTokenClient",httpclient => {
   httpclient.BaseAddress = new Uri("https://www.reddit.com/"); 
   httpclient.DefaultRequestHeaders.UserAgent.ParseAdd("RedditChallenge/1.0 (by /u/Dependent-Bar-8662)");
});

builder.Services.AddSingleton<IRedditAuthRepository,RedditAuthRepository>();
builder.Services.AddSingleton<BroadcastService>();
builder.Services.AddSingleton<IRedditStatsService,RedditStatsService>();
builder.Services.AddScoped<ISubredditRepository,SubredditRepository>();
builder.Services.AddSingleton<IApiRateLimiter,ApiRateLimiter>();
builder.Services.AddSingleton<IApiMonitor,ApiMonitor>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

var api = app.MapGroup("/api/v1/");

api.MapGet("/redditconfig", (IRedditAuthRepository repo) =>
{    
    return repo.GetConfig();
});

api.MapGet("/reddittoken", async (IRedditAuthRepository repo) =>
{    
    return await repo.GetAuthToken();
});

api.MapGet("/subreddit/{subreddit}", async (ISubredditRepository repo, string subreddit) =>
{    
    return await repo.GetSubreddit(subreddit);
});


api.MapGet("/subreddit/{subreddit}/start", async (IApiMonitor monitor, string subreddit) =>
{
    if (monitor.Status().IsRunning)
    {
        return Results.BadRequest("The loop is already running.");
    }

    await monitor.StartAsync(async (sp) =>
    {
        var scope = sp.CreateScope();
        
        var repo = scope.ServiceProvider.GetRequiredService<ISubredditRepository>();
        var stats = sp.GetService<IRedditStatsService>();
        
        var results = await repo.GetSubreddit(subreddit);
        
        stats?.UpdateSubredditStats(results!);

        return (
            results?.RateLimit.Used ?? 0,
            (int)Math.Floor(results?.RateLimit.Remaining ?? 0),
            results?.RateLimit.Reset ?? 0
        );
    });

    return Results.Ok("Loop started.");
});


api.MapGet("/subreddit/{subreddit}/status", (IApiMonitor monitor) =>
{
    return monitor.Status();
});


api.MapGet("/subreddit/{subreddit}/stop", async (IApiMonitor monitor) =>
{
    if (!monitor.Status().IsRunning)
    {
        return Results.BadRequest("The loop is not running.");
    }

    await monitor.StopAsync();
    return Results.Ok("Loop stopped.");
});



app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.MapHub<SubredditHub>("/subreddithub");

app.Run();

