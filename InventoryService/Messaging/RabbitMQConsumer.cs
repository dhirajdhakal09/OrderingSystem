using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

using InventoryService.Events;
using InventoryService.Data;
using InventoryService.Models;

namespace InventoryService.Messaging
{
    public class RabbitMQConsumer: IRabbitMQConsumer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

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
                        await channel.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: false);

                        return;
                    }

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
                        await channel.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: false);

                        return;
                    }

                    inventory.AvailableQuantity -=
                        orderEvent.Quantity;

                    await dbContext.SaveChangesAsync();

                    // Tell RabbitMQ the message was processed successfully
                    await channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);
                }
                catch
                {
                    // Processing failed.
                    // Requeue for now.
                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: true);
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
    }
}
