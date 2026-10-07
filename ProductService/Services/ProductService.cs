using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using StackExchange.Redis;

namespace ProductService.Services
{
    public class ProductService: IProductService
    {
        private readonly ProductDbContext _context;
        private readonly IDatabase _redis;

        public ProductService(ProductDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis.GetDatabase();
        }

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<Product?> UpdateProductAsync(int id, Product product)
        {
            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return null;
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Quantity = product.Quantity;

            await _context.SaveChangesAsync();

            // Invalidate Redis cache
            var cacheKey = $"product:{id}";
            await _redis.KeyDeleteAsync(cacheKey);

            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return false;
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            // Invalidate Redis
            var cacheKey = $"product:{id}";
            await _redis.KeyDeleteAsync(cacheKey);

            return true;
        }
    }
}
