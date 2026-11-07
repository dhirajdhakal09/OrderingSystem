namespace OrderService.Events
{
    public class OrderCreatedEvent
    {
        public Guid EventId { get; set; }
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}