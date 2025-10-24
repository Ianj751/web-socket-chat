using System.Net.WebSockets;
using System.Threading.Channels;

public struct Message
{
    public required byte[] buffer;
    public required WebSocket source;
    public required WebSocket destination;
    public required WebSocketReceiveResult receiveResult;
}

public class MessageQueueService
{
    private readonly Channel<Message> _channel;
    private readonly CancellationTokenSource _cts;

    private readonly Task _processingTask;

    public MessageQueueService(int capacity = 127)
    {
        _channel = Channel.CreateBounded<Message>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _cts = new CancellationTokenSource();

        _processingTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }


    public async Task EnqueueMessage(Message msg)
    {
        await _channel.Writer.WriteAsync(msg);
    }
    private async Task ProcessMessage(Message msg)
    {
        var r = msg.receiveResult;
        if (r.MessageType == WebSocketMessageType.Close)
            return;
        await msg.destination.SendAsync(new ArraySegment<byte>(msg.buffer), r.MessageType, r.EndOfMessage, CancellationToken.None);
    }

    private async Task ProcessQueueAsync(CancellationToken token)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(token))
        {
            try
            {
                await ProcessMessage(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message queue item: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
        _cts.Cancel();

        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {

        }

        _cts.Dispose();
    }
}
