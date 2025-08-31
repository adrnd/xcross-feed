using xcross_backend.Controllers;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddSingleton<WebSocketController>();
builder.Services.AddSingleton<TwitterAPI_TAIO>();
builder.Services.AddSingleton<ITweetStore, TweetStore>(); //in-memory static list/interface of our gathered tweets
builder.Services.AddSingleton<TimingService>();


builder.Services.AddControllers();

var app = builder.Build();

// Simple test endpoint to verify the server is running and responsive. Probably gonna keep this around during development.
app.MapGet("/", () => "Hello World!");

//setting up basic WebSocket options as a starting point
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(1),
    
};
app.UseWebSockets(webSocketOptions);

app.Lifetime.ApplicationStarted.Register(async () =>
{
    
    using var scope = app.Services.CreateScope();
    var start = scope.ServiceProvider.GetRequiredService<TwitterAPI_TAIO>();

    try
    {
        await start.PullTweets();

    }
    catch (Exception ex)
    {
        Console.WriteLine("Initial Twitter pull failed: " + ex.ToString());
    }
});

//Controllers for WebSocket support to the Builder
app.MapControllers();
app.Run();
