using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace xcross_backend.Controllers;

//for reference: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/websockets/samples/8.x/WebSocketsSample/Controllers/WebSocketController.cs
/// <summary>
/// A simple WebSocket controller to demonstrate WebSocket functionality in ASP.NET Core.
/// </summary>
public class WebSocketController : ControllerBase
{
    [Route("/ws")]
    /// <summary>
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
    }
}