
using System.Net.WebSockets;

public class ChatService
{

    List<WebSocket> clients = new List<WebSocket>();
    private readonly MessageQueueService mqService;

    public ChatService(MessageQueueService mqService)
    {
        this.mqService = mqService;
    }

    public async Task HandleWebSocketConn(WebSocket webSocket)
    {
        clients.Add(webSocket);
        await RecieveLoop(webSocket);
        clients.Remove(webSocket);
    }

    //Recieves messages from the socket
    private async Task RecieveLoop(WebSocket client)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (client.State == WebSocketState.Open)
            {

                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(result.CloseStatus!.Value, result.CloseStatusDescription, default);
                    break;
                }

                Random random = new Random(69420);
                Message msg;
                msg.buffer = buffer[..result.Count].ToArray(); ;

                //TODO: replace with something that actually makes sense
                msg.destination = clients
                    .Where(c => c != client)
                    .OrderBy(c => Guid.NewGuid())
                    .FirstOrDefault() ?? client;

                msg.receiveResult = result;
                msg.source = client;
                await mqService.EnqueueMessage(msg);

                //Array.Clear(buffer, 0, buffer.Length);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Recieve Error: ", e.Message);
        }

    }





}
