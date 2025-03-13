using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.EntityFramework
{
    public class ProductDbContext: DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
    }
}
