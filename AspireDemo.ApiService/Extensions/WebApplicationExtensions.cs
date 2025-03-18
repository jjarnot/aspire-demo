using AspireDemo.ApiService.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Extensions;

public static class WebApplicationExtensions
{
    public static void UseCatalogDbMigration(this WebApplication webApp)
    {
        using var scope = webApp.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        dbContext.Database.Migrate();
    }

    public static void UseCatalogDbDataSeeder(this WebApplication webApp)
    {
        using var scope = webApp.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        DataSeeder.Seed(dbContext);
    }
}
