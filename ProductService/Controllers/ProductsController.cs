using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        //[HttpGet]
        //public IActionResult GetAll() => Ok(new[] { new { id = 1, name = "SampleProduct", price = 9.99 } });

        //[HttpGet("{id}")]
        //public IActionResult GetById(int id) => Ok(new { id, name = "SampleProduct", price = 9.99 });


        private readonly IProductService _productService;
        private readonly IDatabase _redis;

        public ProductsController(IProductService productService, IConnectionMultiplexer redis)
        {
            _productService = productService;
            _redis = redis.GetDatabase();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts() => Ok(await _productService.GetProductsAsync());

        [HttpGet("{id}")]
        public async Task<Product?> GetProduct(int id)
        {
            var cacheKey = $"product:{id}";

            // 1. Check Redis
            RedisValue cachedProduct = await _redis.StringGetAsync(cacheKey);

            if (cachedProduct.HasValue)
            {
                return JsonSerializer.Deserialize<Product>((string)cachedProduct!);
            }

            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return null;
            }

            var serializedProduct = JsonSerializer.Serialize(product);

            await _redis.StringSetAsync(
                cacheKey,
                serializedProduct,
                TimeSpan.FromMinutes(10));
            return product;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(
            Product product)
        {
            var createdProduct =
               await _productService.CreateProductAsync(product);

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = createdProduct.Id },
                createdProduct);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
        {
            var updatedProduct =
                await _productService.UpdateProductAsync(id, product);

            if (updatedProduct == null)
            {
                return NotFound();
            }

            return Ok(updatedProduct);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted =
                await _productService.DeleteProductAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
