using RabbitMQ.Client;

namespace InventoryService.Messaging
{
    public class RabbitMQTopology
    {
        public static async Task ConfigureAsync(
            IChannel channel)
        {
            // 1. Dead Letter Exchange
            await channel.ExchangeDeclareAsync(
                exchange: "order-dlx",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // 2. Dead Letter Queue
            await channel.QueueDeclareAsync(
                queue: "order-dead-letter",
                durable: true,
                exclusive: false,
                autoDelete: false);

            // 3. Bind DLQ to DLX
            await channel.QueueBindAsync(
                queue: "order-dead-letter",
                exchange: "order-dlx",
                routingKey: "order-failed");

            // 4. Main queue
            var arguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "order-dlx",
                ["x-dead-letter-routing-key"] = "order-failed"
            };

            await channel.QueueDeclareAsync(
                queue: "order-created",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);
        }
    }
}
