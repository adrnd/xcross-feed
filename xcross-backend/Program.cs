using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
//Controllers for WebSocket support to the Builder
builder.Services.AddControllers();
var app = builder.Build();

// Simple test endpoint to verify the server is running and responsive. Probably gonna keep this around during development.
app.MapGet("/hello", () => "Hello World!"); 

//setting up basic WebSocket options as a starting point
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(1)
};
//necessary for WebSocket support
app.UseWebSockets(webSocketOptions);
app.MapControllers();
app.Run();
