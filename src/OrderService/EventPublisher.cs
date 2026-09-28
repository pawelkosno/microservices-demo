using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService;

// Publishes messages to a RabbitMQ queue. One connection is opened lazily and reused.
public sealed class EventPublisher(IConfiguration config) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<T>(string queue, T message)
    {
        await _lock.WaitAsync();
        try
        {
            if (_channel is null)
            {
                var factory = new ConnectionFactory { HostName = config["RabbitMq:Host"] ?? "localhost" };
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }

            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            await _channel.BasicPublishAsync(exchange: "", routingKey: queue, body: body);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
