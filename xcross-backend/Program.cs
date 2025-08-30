using System.Net.WebSockets;
using xcross_backend.Controllers;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddSingleton<WebSocketTweetController>();
builder.Services.AddControllers();
var app = builder.Build();

// Simple test endpoint to verify the server is running and responsive. Probably gonna keep this around during development.
app.MapGet("/", () => "Hello World!");

//setting up basic WebSocket options as a starting point
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(1)
};
app.UseWebSockets(webSocketOptions);

//Controllers for WebSocket support to the Builder
app.MapControllers();
app.Run();
