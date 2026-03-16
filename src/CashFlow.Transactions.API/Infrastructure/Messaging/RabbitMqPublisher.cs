using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CashFlow.Transactions.API.Infrastructure.Messaging;

public class RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger) : IRabbitMqPublisher, IDisposable
{
    private IChannel? _channel;

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is null || _channel.IsClosed)
            _channel = await connection.CreateChannelAsync();
        return _channel;
    }

    public async Task PublishAsync<T>(T message, string exchange, string routingKey, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync();

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };

        await channel.BasicPublishAsync(exchange, routingKey, false, props, body, ct);
        logger.LogInformation("Published {EventType} to {Exchange}/{RoutingKey}", typeof(T).Name, exchange, routingKey);
    }

    public void Dispose() => _channel?.Dispose();
}
