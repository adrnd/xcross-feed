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
    private readonly ITweetStore _tweetStore;
    private TwitterAPI_TAIO _tweeter;
    private TimingService _timingService;

    /// <summary>
    /// Constructor for the WebSocketController and it's required services.
    /// </summary>
    /// <param name="tweetStore">Static TweetStore Interface</param>
    /// <param name="tweeter">Connected Twitter API</param>
    /// <param name="timingService">Global Timing Service</param>
    public WebSocketController(ITweetStore tweetStore, TwitterAPI_TAIO tweeter, TimingService timingService)
    {
        _tweetStore = tweetStore;
        _tweeter = tweeter;
        _timingService = timingService;
    }
    private static List<WebSocket> _sockets = new();
    private static DateTime lastCheck = DateTime.MinValue;
    /// <summary>
    /// simple reflection to check if the Timer Tick has ever been subscribed to
    /// needs to be replaced with a more reliable solution
    /// </summary>
    private bool timerScrubscribed = false;


    /// <summary>
    /// Route to connect clients to, get's handed off to HandleWebSocketConnection;
    /// </summary>
    /// <returns></returns>
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
    /// When a new WebSocket connection gets detected, it gets added as a _socket instances and monitored as long as it's open.
    /// Starts the Timing Service as soon as the first 
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    public async Task HandleWebSocketConnection(WebSocket socket)
    {
        _sockets.Add(socket);
        var buffer = new byte[1024 * 2];

        //immediately trying to send the current TweetList as bytes array
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_tweetStore.TweetsList);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, default);

        //starts the TimingService as soon as the first client connects 
        if (_timingService._disposed == true)
        {
            await _timingService.StartAsync(default);
            if (timerScrubscribed == false) //TODO: check if the OnTickAsync event needs to be reinitialized in case the timer got restarted
            {
                _timingService.OnTickAsync += async () => //subscribing to the Tick event of the TimingService, triggers a refresh
                {

                    Console.WriteLine("Received Ping");
                    await RefreshTweets(true);
                };
            }
        }
        //this loop is where the communication happens per client, you should not break out of it unless it closes
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
            if (result.MessageType == WebSocketMessageType.Text) //received a message that is not a CloseStatus
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received message: " + message);
                //different messages can be added as commands in the future
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
                        var msbytes = System.Text.Encoding.UTF8.GetBytes("nothing new"); //note: seems to send the string including quotation marks (")
                        await socket.SendAsync(msbytes, WebSocketMessageType.Text, true, default);
                    }
                }
            }


        }
        _sockets.Remove(socket);
        if (_sockets.Count == 0)
        { 
            //await _timingService.StopAsync(default); //disposes the timing service when no more sockets are left, commented out for the case that other services are relying on it
            //_timingService.Dispose();
        }
    }

    /// <summary>
    /// Refreshes the TweetsList in the TweetStore interface by triggering a refresh in the Twitter API service. Compares the newest entries before and after the refresh.
    /// </summary>
    /// <param name="sendToSockets">By default, the result of the TweetsList refresh will not get sent to the connected clients. Can be removed in production.</param>
    /// <returns>True when new tweets were found, false when no new tweets were found.</returns>
    [Route("/chirp")]
    public async Task<bool> RefreshTweets(bool sendToSockets = false)
    {
       if (_sockets.Count == 0) 
        { return false; }
        if (lastCheck.AddSeconds(20) > DateTime.UtcNow) { return false; } //simple limit to avoid blocking the Twitter API when too many "chirp" requests come in
        
        string latestEntry = _tweetStore.TweetsList[0].TweetId;
        await _tweeter.PullTweets();
        string lastestEntryAfterRefresh = _tweetStore.TweetsList[0].TweetId;
        if (latestEntry == lastestEntryAfterRefresh)
        {
            Console.WriteLine(DateTime.Now + "nothing new");
            lastCheck = DateTime.UtcNow;
            await BroadcastToAll(JsonSerializer.SerializeToUtf8Bytes("nothing new"));
            return false;
        }
        if (sendToSockets)
        {
            lastCheck = DateTime.UtcNow;
            await BroadcastToAll(JsonSerializer.SerializeToUtf8Bytes(_tweetStore.TweetsList));
        }
        return true;
    }
    /// <summary>
    /// Sends bytes array to ALL clients that are currently connected e.g. for updating the TweetList on clients.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private async Task BroadcastToAll (byte[] data)
    {
        var sendTasks = _sockets
        .Where(s => s.State == WebSocketState.Open)
        .Select(s => s.SendAsync(data, WebSocketMessageType.Text, true, default));

        // await all sends
        await Task.WhenAll(sendTasks);
    }
}
