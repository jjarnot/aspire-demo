namespace AspireDemo.ApiService.EntityFramework
{
    public class DataSeeder
    {
        public static void Seed(ProductDbContext dbContext)
        {
            if (dbContext.Products.Any())
                return;

            dbContext.Products.Add(new Product { Name = "Test1", Description = "Test 1 description", Price = 10.00m});
            dbContext.Products.Add(new Product { Name = "Test2", Description = "Test 2 description", Price = 11.00m });
            dbContext.Products.Add(new Product { Name = "Test3", Description = "Test 3 description", Price = 12.00m });
            dbContext.SaveChanges();
        }
    }
}
