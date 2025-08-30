using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace xcross_backend.Controllers;

//for reference: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/websockets/samples/8.x/WebSocketsSample/Controllers/WebSocketController.cs
/// <summary>
/// A simple WebSocket controller to demonstrate WebSocket functionality in ASP.NET Core.
/// </summary>
public class WebSocketTweetController : ControllerBase
{
     private readonly List<WebSocket> _sockets = new();
    [Route("/ws")]
    public async Task Get () 
    {
    if (HttpContext.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocketConnection(webSocket);
    }
    else
    {
        HttpContext.Response.StatusCode = 400;
        await HttpContext.Response.WriteAsync("Expected a WebSocket request");
    }
    }
    public async Task HandleWebSocketConnection(WebSocket socket)
    {
        _sockets.Add(socket);
        var buffer = new byte[1024 * 2];
        //push the initial tweet list now already?
        var _tweeter = new TwitterAPI_TAIO();
        await _tweeter.PullTweets();  //remove later
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_tweeter.TweetsList);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, default);

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                var closeStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                await socket.CloseAsync(closeStatus, result.CloseStatusDescription, default);
                break;
            }
            _tweeter.TweetListUpdated += async (sender, args) =>
            {
                var tweetBytes = JsonSerializer.SerializeToUtf8Bytes(args.TweetsList);
                foreach (var s in _sockets)
                {
                    await s.SendAsync(tweetBytes, WebSocketMessageType.Text, true, default);
                }
            };

        }
        _sockets.Remove(socket);
    }

/*     /// <summary>
    /// Accepts WebSocket requests and echoes back messages sent by the client. Or return 400 if not a WebSocket request.
    /// </summary>
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    /// <summary>
    /// Method to echo back messages sent by the client. Very simple implementation to test the WebSocket connection in both directions. Closes only when the client sends a close message.
    /// </summary>
    /// <param name="webSocket"></param>
    /// <returns></returns>
    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    } */
}