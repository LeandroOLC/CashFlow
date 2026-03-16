using System.Text;
using System.Text.Json;
using CashFlow.Consolidation.API.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Consolidation.API.Infrastructure.Messaging;

public record TransactionCreatedEvent(Guid TransactionId, decimal Amount, string Type, DateTime Date, string Description, DateTime OccurredAt);
public record TransactionUpdatedEvent(Guid TransactionId, decimal OldAmount, string OldType, DateTime OldDate, decimal NewAmount, string NewType, DateTime NewDate, DateTime OccurredAt);
public record TransactionDeletedEvent(Guid TransactionId, decimal Amount, string Type, DateTime Date, DateTime OccurredAt);

public class TransactionCreatedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionCreatedConsumer> logger) : BackgroundService
{
    private const string Exchange = "cashflow.transactions";
    private const string Queue = "consolidation.transaction-events";
    private const string DlxExchange = "cashflow.transactions.dlx";    // Dead Letter Exchange
    private const string DlqQueue = "consolidation.transaction-events.dlq";
    private const string RoutingKeyCreated = "transaction.created";
    private const string RoutingKeyUpdated = "transaction.updated";
    private const string RoutingKeyDeleted = "transaction.deleted";
    private const int MaxRetries = 3;

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // ── Dead Letter Exchange e DLQ ────────────────────────────────────────
        // Mensagens rejeitadas (BasicNack requeue:false) após esgotar retries
        // vão para o DLX → DLQ, onde podem ser inspecionadas e reprocessadas.
        await _channel.ExchangeDeclareAsync(DlxExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(DlqQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(DlqQueue, DlxExchange, Queue, cancellationToken: stoppingToken);

        // ── Fila principal com x-dead-letter-exchange ─────────────────────────
        await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DlxExchange,
            ["x-dead-letter-routing-key"] = Queue,
            ["x-message-ttl"] = 86_400_000   // mensagens não processadas expiram em 24h
        };
        await _channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: queueArgs, cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(Queue, Exchange, RoutingKeyCreated, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(Queue, Exchange, RoutingKeyUpdated, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(Queue, Exchange, RoutingKeyDeleted, cancellationToken: stoppingToken);

        // Processa 1 mensagem por vez — evita race condition no saldo
        await _channel.BasicQosAsync(0, 1, false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var routingKey = ea.RoutingKey;

            // Lê contador de tentativas do header (injetado pelo RabbitMQ a cada Nack)
            var retryCount = 0;
            if (ea.BasicProperties.Headers?.TryGetValue("x-retry-count", out var retryObj) == true)
                retryCount = retryObj is byte[] b ? int.Parse(Encoding.UTF8.GetString(b)) : Convert.ToInt32(retryObj);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IConsolidationService>();

                switch (routingKey)
                {
                    case RoutingKeyCreated:
                        var created = JsonSerializer.Deserialize<TransactionCreatedEvent>(body)
                            ?? throw new InvalidOperationException("Falha ao desserializar TransactionCreatedEvent");
                        await service.ProcessTransactionCreatedAsync(created.Date, created.Amount, created.Type, stoppingToken);
                        break;

                    case RoutingKeyUpdated:
                        var updated = JsonSerializer.Deserialize<TransactionUpdatedEvent>(body)
                            ?? throw new InvalidOperationException("Falha ao desserializar TransactionUpdatedEvent");
                        await service.ProcessTransactionUpdatedAsync(
                            updated.OldAmount, updated.OldType, updated.OldDate,
                            updated.NewAmount, updated.NewType, updated.NewDate, stoppingToken);
                        break;

                    case RoutingKeyDeleted:
                        var deleted = JsonSerializer.Deserialize<TransactionDeletedEvent>(body)
                            ?? throw new InvalidOperationException("Falha ao desserializar TransactionDeletedEvent");
                        await service.ProcessTransactionDeletedAsync(deleted.Date, deleted.Amount, deleted.Type, stoppingToken);
                        break;

                    default:
                        logger.LogWarning("RoutingKey desconhecida: {RoutingKey}", routingKey);
                        break;
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                logger.LogInformation("Mensagem processada. RoutingKey={RoutingKey}", routingKey);
            }
            catch (Exception ex)
            {
                if (retryCount < MaxRetries)
                {
                    // ── Retry com backoff exponencial (2s, 4s, 8s) ───────────
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount + 1));
                    logger.LogWarning(ex,
                        "Falha ao processar mensagem. Tentativa {Attempt}/{Max}. Re-enfileirando em {Delay:0.0}s. RoutingKey={RoutingKey}",
                        retryCount + 1, MaxRetries, delay.TotalSeconds, routingKey);

                    await Task.Delay(delay, stoppingToken);

                    // Re-publica com contador incrementado
                    var props = new BasicProperties();
                    props.Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers)
                    { ["x-retry-count"] = Encoding.UTF8.GetBytes((retryCount + 1).ToString()) };
                    props.DeliveryMode = DeliveryModes.Persistent;
                    props.ContentType = "application/json";

                    await _channel.BasicPublishAsync(Exchange, routingKey, false, props,
                        ea.Body, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                else
                {
                    // ── Esgotou retries → envia para DLQ ─────────────────────
                    logger.LogError(ex,
                        "Mensagem descartada para DLQ após {MaxRetries} tentativas. RoutingKey={RoutingKey} Body={Body}",
                        MaxRetries, routingKey, body);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
                }
            }
        };

        await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        logger.LogInformation("Consumer iniciado. Exchange={Exchange} Queue={Queue} DLQ={DLQ}", Exchange, Queue, DlqQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}