using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static xcross_backend.Controllers.TwitterAPI_TAIO;

namespace xcross_backend.Controllers;

//for reference: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/websockets/samples/8.x/WebSocketsSample/Controllers/WebSocketController.cs
/// <summary>
/// A simple WebSocket controller to demonstrate WebSocket functionality in ASP.NET Core.
/// </summary>
public class WebSocketController : ControllerBase
{
    private readonly TwitterAPI_TAIO.ITweetStore _tweetStore;
    private TwitterAPI_TAIO _tweeter;
    private TimingService _timingService;

    public WebSocketController(ITweetStore tweetStore, TwitterAPI_TAIO tweeter, TimingService timingService)
    {
        _tweetStore = tweetStore;
        _tweeter = tweeter;
        _timingService = timingService;
    }
    private static List<WebSocket> _sockets = new();
    private static DateTime lastCheck = DateTime.MinValue;



    [Route("/ws")]
    public async Task Get()
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
    /// <summary>
    /// When a new Socket connection gets detected, it gets added as a new WebSocket instances and added to the while loop.
    /// ATM messages can only be sent in this loop. For simplicity sake, clients will periodically 
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public async Task HandleWebSocketConnection(WebSocket socket)
    {

        _sockets.Add(socket);
        var buffer = new byte[1024 * 2];
        //push the initial tweet list now already?
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_tweetStore.TweetsList);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, default);
        if (_sockets.Count == 1)
        {
            await _timingService.StartAsync(default);
            _timingService.OnTickAsync += async () =>
            {
                Console.WriteLine("ws Received Ping");
                await RefreshTweets(true);
            };
        }
        //this loop is where the communication happens, you should not break out of it
        while (socket.State == WebSocketState.Open)
        {
            //when we receive a message, the following lines get triggered
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
            if (result.MessageType == WebSocketMessageType.Close)
            {

                var closeStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                await socket.CloseAsync(closeStatus, result.CloseStatusDescription, default);
                break;
            }
            else
            if (result.MessageType == WebSocketMessageType.Text)
            {
                //for our example we can just assume that any message is a manual request to update
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received message: " + message);

                if (message == "chirp")
                {
                    if (await RefreshTweets(true) == false)
                    {
                        var msbytes = JsonSerializer.SerializeToUtf8Bytes(_tweetStore.TweetsList);
                        foreach (var _socket in _sockets)
                        {
                            await _socket.SendAsync(msbytes, WebSocketMessageType.Text, true, default);
                        }
                    }
                    else
                    {
                        var msbytes = System.Text.Encoding.UTF8.GetBytes("nothing new");
                        await socket.SendAsync(msbytes, WebSocketMessageType.Text, true, default);
                    }
                }
            }


        }
        _sockets.Remove(socket);
        if (_sockets.Count == 0)
        { await _timingService.StopAsync(default); //disposes the timing service when no moore sockets are left,line can be removed when other services are relying on it
            _timingService.Dispose();
        }
    }

    [Route("/chirp")]
    public async Task<bool> RefreshTweets(bool send2Sockets = false)
    {
       if (_sockets.Count == 0) { 
            return false; }
        if (lastCheck.AddSeconds(20) > DateTime.UtcNow) { return false; }
        var tempList = _tweetStore.TweetsList[0].TweetId;
        await _tweeter.PullTweets();
        var newList = _tweetStore.TweetsList[0].TweetId;
        if (tempList == newList)
        {
            Console.WriteLine(DateTime.Now + "nothing new");
            lastCheck = DateTime.UtcNow;
            await BroadcastToAll(JsonSerializer.SerializeToUtf8Bytes("nothing new"));
            return false;
        }
        if (send2Sockets)
        {
            lastCheck = DateTime.UtcNow;
            await BroadcastToAll(JsonSerializer.SerializeToUtf8Bytes(_tweetStore.TweetsList));
        }
        return true;
    }

    private async Task BroadcastToAll (byte[] data)
    {
        var sendTasks = _sockets
        .Where(ws => ws.State == WebSocketState.Open)
        .Select(ws => ws.SendAsync(data, WebSocketMessageType.Text, true, default));

        // await all sends
        await Task.WhenAll(sendTasks);
    }
}
