using OrderService.Data;
using OrderService.Events;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Messaging
{
    public class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxBackgroundService> _logger;

        public OutboxBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while processing outbox messages.");
                }

                // Poll the database every 5 seconds.
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<OrderDbContext>();

            var publisher = scope.ServiceProvider
                .GetRequiredService<IRabbitMQPublisher>();

            var messages = await dbContext.OutboxMessages
                .Where(x => x.ProcessedOnUtc == null)
                .OrderBy(x => x.OccurredOnUtc)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await PublishMessageAsync(publisher, message);

                    // Only mark processed after publishing succeeds.
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;

                    await dbContext.SaveChangesAsync(
                        cancellationToken);

                    _logger.LogInformation(
                        "Outbox message {MessageId} published.",
                        message.Id);
                }
                catch (Exception ex)
                {
                    // Leave ProcessedOnUtc null so the worker
                    // can retry on a later poll.
                    message.Error = ex.Message;

                    await dbContext.SaveChangesAsync(
                        cancellationToken);

                    _logger.LogError(
                        ex,
                        "Failed to publish outbox message {MessageId}.",
                        message.Id);
                }
            }
        }

        private static async Task PublishMessageAsync(
            IRabbitMQPublisher publisher,
            OutboxMessage message)
        {
            switch (message.Type)
            {
                case nameof(OrderCreatedEvent):
                    var orderEvent =
                        JsonSerializer.Deserialize<OrderCreatedEvent>(
                            message.Payload);

                    if (orderEvent == null)
                    {
                        throw new InvalidOperationException(
                            $"Invalid payload for outbox message {message.Id}.");
                    }

                    await publisher.PublishAsync(
                        orderEvent,
                        "order-created");

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown outbox event type: {message.Type}");
            }
        }
    }
}
