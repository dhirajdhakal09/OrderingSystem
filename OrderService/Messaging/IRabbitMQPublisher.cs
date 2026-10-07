namespace OrderService.Messaging
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(T message, string queueName);
    }
}
