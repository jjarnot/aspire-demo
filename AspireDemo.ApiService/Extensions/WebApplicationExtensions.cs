using AspireDemo.ApiService.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void UseProductDbMigration(this WebApplication webApp)
        {
            using var scope = webApp.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            dbContext.Database.Migrate();
        }

        public static void UseProductDbDataSeeder(this WebApplication webApp)
        {
            using var scope = webApp.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            DataSeeder.Seed(dbContext);
        }
    }
}
