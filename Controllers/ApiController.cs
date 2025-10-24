


using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

record ErrorResponse(string message);
[Route("api")]
public class ApiControlller : ControllerBase
{
    private readonly MessageQueueService _mqService;
    private readonly ILogger<ApiControlller> _logger;
    private readonly ChatService _chatService;


    public ApiControlller(ILogger<ApiControlller> logger, ChatService chatService, MessageQueueService mqService)
    {
        _logger = logger;
        _chatService = chatService;
        _mqService = mqService;

    }
    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        // msg stored in buffer
        WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
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



    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogInformation("recieved websocket request");
            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _chatService.HandleWebSocketConn(ws);

        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
