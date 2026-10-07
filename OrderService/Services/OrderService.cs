using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Events;
using OrderService.Messaging;

namespace OrderService.Services
{
    public class OrderService: IOrderService
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
            order.CreatedAt = DateTime.UtcNow;

            // 1. Save Order to SQL
            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // 2. Publish event AFTER SQL succeeds
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                ProductId = order.ProductId,
                Quantity = order.Quantity
            };

            await _rabbitMQPublisher.PublishAsync(
                orderCreatedEvent,
                "order-created");

            return order;
        }
    }
}
