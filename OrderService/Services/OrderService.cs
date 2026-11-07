using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Messaging;
using System.Text.Json;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly OrderDbContext _context;

        public OrderService(IRabbitMQPublisher rabbitMQPublisher, OrderDbContext context)
        {
            _rabbitMQPublisher = rabbitMQPublisher;
            _context = context;
        }
        public async Task<Order> CreateOrderAsync(Order order)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                order.CreatedAt = DateTime.UtcNow;

                // 1. Save Order to SQL
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                var eventGuid = Guid.NewGuid();
                // 2. Publish event AFTER SQL succeeds
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    EventId = eventGuid,
                    OrderId = order.Id,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity
                };

                //await _rabbitMQPublisher.PublishAsync(
                //    orderCreatedEvent,
                //    "order-created");

                //return order;
                // 3. Store the event in the outbox.
                var outboxMessage = new OutboxMessage
                {
                    Id = eventGuid,
                    Type = nameof(OrderCreatedEvent),
                    Payload = JsonSerializer.Serialize(orderCreatedEvent),
                    OccurredOnUtc = DateTime.UtcNow
                };

                _context.OutboxMessages.Add(outboxMessage);

                await _context.SaveChangesAsync();

                // 4. Commit both database changes together.
                await transaction.CommitAsync();

                // 5. Do NOT publish to RabbitMQ here.
                // The background worker will publish the event.

                return order;

            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }
    }
}
