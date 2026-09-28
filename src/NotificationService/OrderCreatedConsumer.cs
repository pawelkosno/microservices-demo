using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService;

// Listens for OrderCreated events and "sends a notification" (logs it).
public class OrderCreatedConsumer(IConfiguration config, ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = config["RabbitMq:Host"] ?? "localhost" };
        await using var connection = await ConnectWithRetryAsync(factory, stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(Queues.OrderCreated, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var order = JsonSerializer.Deserialize<OrderCreated>(ea.Body.Span)!;
            logger.LogInformation("Notification: order {OrderId} - {Quantity} x {Product}, total {Total}",
                order.OrderId, order.Quantity, order.ProductName, order.Total);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(Queues.OrderCreated, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Listening on queue '{Queue}'", Queues.OrderCreated);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // The broker may start slower than this service - keep trying.
    private async Task<IConnection> ConnectWithRetryAsync(ConnectionFactory factory, CancellationToken ct)
    {
        while (true)
        {
            try
            {
                return await factory.CreateConnectionAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("RabbitMQ not reachable ({Message}), retrying in 3s...", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }
}
