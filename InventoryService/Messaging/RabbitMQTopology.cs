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


            // =========================================================
            // 4. Retry Queue
            // =========================================================
            //
            // Messages stay here for 10 seconds.
            // After 10 seconds RabbitMQ sends them back to
            // the order-created queue.
            //

            var retryQueueArguments = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = 10000, //Keep a message in order-retry for 10,000 milliseconds = 10 seconds.

                // Send expired messages to the default exchange
                ["x-dead-letter-exchange"] = "",

                // Default exchange routes using queue name
                ["x-dead-letter-routing-key"] = "order-created"
            };

            await channel.QueueDeclareAsync(
                queue: "order-retry",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: retryQueueArguments);
        }
    }
}
