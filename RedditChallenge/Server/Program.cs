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

api.MapGet("/subreddit/{subreddit}/start", async (IApiMonitor monitor, ISubredditRepository repo, string subreddit) =>
{    
    return await monitor.StartMonitor(subreddit);

    return await repo.GetSubreddit(subreddit);
});


app.MapRazorPages();
//app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

