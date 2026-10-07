namespace InventoryService.Messaging
{
    public interface IRabbitMQConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
