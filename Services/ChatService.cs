using System.Collections.Concurrent;
using System.Net.WebSockets;


//MVP between 2 clients then we'll scale
//Global vars are not unique to threads. The only unique parts is the controller
// I need each thread to recieve one version of the chat service. Singleton not necessary if DI into ApiController
// server recieves from both ends and sends when its ready
/*
Flow:
1. Client1 and Client 2 connect to the server
2. Client1 sets the websocket
3. both call chat, race to see who recieves first
*/
public class ChatService
{
    private readonly object _lock = new object();
    private WebSocket? client1 = null;
    private WebSocket? client2 = null;



    public void SetWebSocket(WebSocket ws)
    {
        if (ws == null) throw new ArgumentNullException(nameof(ws));

        lock (_lock)
        {
            if (client1 == null)
            {
                client1 = ws;

            }
            else
            {
                client2 = ws;
            }

        }
    }
    public async Task Chat()
    {
        while (client1 == null || client2 == null)
        {
            Console.WriteLine($"waiting {Thread.CurrentThread.ManagedThreadId}...");
            await Task.Delay(1000);
        }

        var c1Buffer = new byte[1024 * 4];
        var c2Buffer = new byte[1024 * 4];




        //OKOKKOKO quick observation: client1 isnt recieving anything but it kinda works???????
        while (true)
        {

            var recvResult1 = client1.ReceiveAsync(new ArraySegment<byte>(c1Buffer), CancellationToken.None);
            var recvResult2 = client2.ReceiveAsync(new ArraySegment<byte>(c2Buffer), CancellationToken.None);

            Task<WebSocketReceiveResult> completed = await Task.WhenAny(recvResult1, recvResult2);
            if (completed == recvResult1)
            {

                var r = await recvResult1;
                if (r.MessageType == WebSocketMessageType.Close)
                    break;
                await client2.SendAsync(new ArraySegment<byte>(c1Buffer, 0, r.Count), r.MessageType, r.EndOfMessage, CancellationToken.None);

            }
            else
            {

                var r = await recvResult2;
                if (r.MessageType == WebSocketMessageType.Close)
                    break;
                await client1.SendAsync(new ArraySegment<byte>(c2Buffer, 0, r.Count), r.MessageType, r.EndOfMessage, CancellationToken.None);

            }
        }

    }



}