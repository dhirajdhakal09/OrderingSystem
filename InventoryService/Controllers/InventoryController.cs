using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll() => Ok(new[] { new { sku = "SKU-1", available = 42 } });

        [HttpGet("{sku}")]
        public IActionResult GetBySku(string sku) => Ok(new { sku, available = 42 });
    }
}
