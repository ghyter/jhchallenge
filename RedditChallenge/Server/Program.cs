using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

var api = app.MapGroup("/api");
api.MapGet("/hello", () => "Hello World!");

api.MapGet("/redditconfig", () =>
{    
    
    var config = new RedditOATHSettings(Environment.GetEnvironmentVariable("REDDIT_CLIENT_ID")?? string.Empty,
        Environment.GetEnvironmentVariable("REDDIT_CLIENT_SECRET")?? string.Empty,
        Environment.GetEnvironmentVariable("REDDIT_REDIRECT_URI") ?? string.Empty,
        Ulid.NewUlid().ToString()
    );
    return config;
});


app.MapRazorPages();
//app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

record RedditOATHSettings(string ClientId, string ClientSecret, string RedirectUri, string State);
