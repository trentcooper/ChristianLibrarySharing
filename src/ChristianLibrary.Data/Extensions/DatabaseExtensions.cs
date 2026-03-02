
using ChristianLibrary.Data.Context;
using ChristianLibrary.Data.Seed;
using ChristianLibrary.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChristianLibrary.Data.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Seeds the database with initial data (roles, admin user, sample books)
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<DbSeeder>>();

            var seeder = new DbSeeder(context, userManager, roleManager, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseExtensions");
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}