using System.Collections.Concurrent;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitRpcClient;

public class RpcClient : IDisposable
{
    private const string QueueName = "rpc_queue";

    private const string ReplyExchangeName = "my-reply-exchange";
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

    public RpcClient()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        // declare a server-named queue
        _replyQueueName = _channel.QueueDeclare("my-reply-queue-name").QueueName;
        // declare a new exchange for MT to reply to
        _channel.ExchangeDeclare(ReplyExchangeName, "fanout", false, true);
        //bind the reply queue to the reply exchange
        _channel.QueueBind(_replyQueueName, ReplyExchangeName, string.Empty);
        
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var conversationIdBytes = (byte[])ea.BasicProperties.Headers["ConversationId"];
            var conversationId = Encoding.UTF8.GetString(conversationIdBytes);
            if (!_callbackMapper.TryRemove(conversationId, out var tcs))
                return;
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);
        };

        _channel.BasicConsume(consumer: consumer,
            queue: _replyQueueName,
            autoAck: true);
    }
    
    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        IBasicProperties props = _channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.MessageId = correlationId;
        props.ReplyTo = _replyQueueName;
        props.Headers = new Dictionary<string, object>();
        props.Headers.Add("MT-Response-Address", $"rabbitmq://localhost/{ReplyExchangeName}?temporary=true");
        // props.Headers.Add("MessageId", correlationId);
        // props.Headers.Add("RequestId", correlationId);
        props.Headers.Add("ConversationId", correlationId);
        
        Console.WriteLine($"CorrelationId= {correlationId}");
        Console.WriteLine(props.Headers["MT-Response-Address"]);
        
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var tcs = new TaskCompletionSource<string>();
        _callbackMapper.TryAdd(correlationId, tcs);

        _channel.ExchangeDeclare("my.existing.exchange", "direct",true);
        _channel.BasicPublish(
            exchange: "my.existing.exchange",
            routingKey: string.Empty,
            basicProperties: props,
            body: messageBytes);

        cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
        return tcs.Task;
    }

    public void Dispose()
    {
        _connection.Close();
    }
}