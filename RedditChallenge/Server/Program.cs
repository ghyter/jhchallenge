using Microsoft.AspNetCore.ResponseCompression;
using RedditChallenge.Shared.Repositories;
using RedditChallenge.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddHttpClient("RedditTokenClient",httpclient => {
   httpclient.BaseAddress = new Uri("https://www.reddit.com/"); 
   httpclient.DefaultRequestHeaders.UserAgent.ParseAdd("RedditChallenge/1.0 (by /u/Dependent-Bar-8662)");
});

builder.Services.AddSingleton<IRedditAuthRepository,RedditAuthRepository>();
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


api.MapGet("/subreddit/{subreddit}/start", async (IApiMonitor monitor, IServiceProvider serviceProvider, string subreddit) =>
{
    if (monitor.Status())
    {
        return Results.BadRequest("The loop is already running.");
    }

    await monitor.StartAsync(async () =>
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISubredditRepository>();

        var results = await repo.GetSubreddit(subreddit);

        results?.Data?.Children?.ForEach(post =>
        {
            Console.WriteLine(post?.Data?.Title);
        });

        return (
            (int)Math.Floor(results?.RateLimit.Remaining ?? 0),
            results?.RateLimit.Reset ?? 0
        );
    });

    return Results.Ok("Loop started.");
});


api.MapGet("/subreddit/{subreddit}/status", (IApiMonitor monitor) =>
{
    return Results.Json(new
    {
        isRunning = monitor.Status(),
        runningSince = monitor.RunningSince
    });
});


api.MapPost("/subreddit/{subreddit}/stop", async (IApiMonitor monitor) =>
{
    if (!monitor.Status())
    {
        return Results.BadRequest("The loop is not running.");
    }

    await monitor.StopAsync();
    return Results.Ok("Loop stopped.");
});



app.MapRazorPages();
//app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

