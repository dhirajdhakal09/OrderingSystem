using InventoryService.Data;
using InventoryService.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace InventoryService.Messaging
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        private const int MaxRetries = 3;

        public RabbitMQConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            var connection =
                await factory.CreateConnectionAsync();

            var channel =
                await connection.CreateChannelAsync();

            // for dead letter queue, we need to configure the topology first
            await RabbitMQTopology.ConfigureAsync(channel);

            //await channel.QueueDeclareAsync(
            //    queue: "order-created",
            //    durable: true,
            //    exclusive: false,
            //    autoDelete: false);

            var consumer =
                new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(args.Body.ToArray());

                    var orderEvent =
                        JsonSerializer.Deserialize<OrderCreatedEvent>(json);

                    if (orderEvent == null)
                    {
                        await MoveToDeadLetterQueue(
                            channel,
                            args);

                        return;
                    }

                    // TEST ONLY
                    //throw new Exception("TEST RETRY");

                    using var scope =
                        _scopeFactory.CreateScope();

                    var dbContext =
                        scope.ServiceProvider
                            .GetRequiredService<InventoryDbContext>();

                    var inventory =
                        await dbContext.InventoryItems
                            .FirstOrDefaultAsync(
                                x => x.ProductId == orderEvent.ProductId);

                    if (inventory == null)
                    {
                        await channel.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: false);

                        return;
                    }

                    if (inventory.AvailableQuantity <
                        orderEvent.Quantity)
                    {
                        Console.WriteLine(
                            $"Insufficient inventory. " +
                            $"Available: " +
                            $"{inventory.AvailableQuantity}, " +
                            $"Requested: " +
                            $"{orderEvent.Quantity}");

                        // Business failure.
                        // Don't retry automatically.
                        await MoveToDeadLetterQueue(
                            channel,
                            args);

                        return;
                    }

                    inventory.AvailableQuantity -=
                        orderEvent.Quantity;

                    await dbContext.SaveChangesAsync();

                    Console.WriteLine(
                    $"Order {orderEvent.OrderId} " +
                   $"processed successfully.");

                    // Tell RabbitMQ the message was processed successfully
                    await channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: " + $"{ex.Message}");

                    await HandleRetryAsync(
                        channel,
                        args);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "order-created",
                autoAck: false,
                consumer: consumer);

            await Task.Delay(
                Timeout.Infinite,
                cancellationToken);
        }

        private async Task HandleRetryAsync(IChannel channel, BasicDeliverEventArgs args)
        {
            var retryCount =
                GetRetryCount(args);

            Console.WriteLine(
                $"Current retry count: {retryCount}");


            if (retryCount >= MaxRetries)
            {
                Console.WriteLine(
                    "Maximum retries reached. " +
                    "Moving message to DLQ.");

                await MoveToDeadLetterQueue(
                    channel,
                    args);

                return;
            }


            // Increase retry count
            var nextRetryCount =
                retryCount + 1;


            var properties =
                new BasicProperties
                {
                    Persistent = true,
                    Headers =
                        new Dictionary<string, object?>
                        {
                            ["x-retry-count"] =
                                nextRetryCount
                        }
                };


            // Publish message to retry queue
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "order-retry",
                mandatory: false,
                basicProperties: properties,
                body: args.Body.ToArray());


            Console.WriteLine(
                $"Message moved to retry queue. " +
                $"Retry #{nextRetryCount}");


            // ACK the original message.
            //
            // We ACK because we successfully transferred
            // the message to the retry queue.
            await channel.BasicAckAsync(
                args.DeliveryTag,
                multiple: false);
        }


        private static int GetRetryCount(
    BasicDeliverEventArgs args)
        {
            if (args.BasicProperties.Headers == null)
                return 0;

            if (!args.BasicProperties.Headers.TryGetValue(
                    "x-retry-count",
                    out var value))
            {
                return 0;
            }

            if (value is byte[] bytes &&
                int.TryParse(
                    Encoding.UTF8.GetString(bytes),
                    out var byteResult))
            {
                return byteResult;
            }

            if (value is int intValue)
                return intValue;

            return 0;
        }


        private static async Task MoveToDeadLetterQueue(
            IChannel channel,
            BasicDeliverEventArgs args)
        {
            await channel.BasicNackAsync(
                args.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }
}
