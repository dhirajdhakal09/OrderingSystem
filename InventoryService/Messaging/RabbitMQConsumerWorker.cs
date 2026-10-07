namespace InventoryService.Messaging
{
    public class RabbitMQConsumerWorker : BackgroundService
    {
        private readonly IRabbitMQConsumer _consumer;

        public RabbitMQConsumerWorker(
            IRabbitMQConsumer consumer)
        {
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await _consumer.StartAsync(stoppingToken);
        }
    }
}
