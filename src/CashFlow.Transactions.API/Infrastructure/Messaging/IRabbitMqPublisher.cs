namespace CashFlow.Transactions.API.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T message, string exchange, string routingKey, CancellationToken ct = default);
}
