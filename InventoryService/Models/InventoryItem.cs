namespace InventoryService.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int AvailableQuantity { get; set; }
    }

    public class InboxMessage
    {
        public Guid EventId { get; set; }

        public DateTime ProcessedOnUtc { get; set; } =
            DateTime.UtcNow;
    }
}
