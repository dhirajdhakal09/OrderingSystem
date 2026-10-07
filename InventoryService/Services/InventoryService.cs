namespace InventoryService.Services
{
    public class InventoryService
    {
        public IEnumerable<object> GetInventory() => new[] { new { sku = "SKU-1", available = 42 } };
    }
}
