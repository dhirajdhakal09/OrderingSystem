using Microsoft.EntityFrameworkCore;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTime OccurredOnUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? ProcessedOnUtc { get; set; }

        public string? Error { get; set; }
    }
}
