using ChristianLibrary.Data.Context;
using ChristianLibrary.Data.Repositories;
using ChristianLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChristianLibrary.Data.Extensions
{
    /// <summary>
    /// Extension methods for registering data layer services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register all data layer services including DbContext, repositories, and Unit of Work
        /// </summary>
        public static IServiceCollection AddDataServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("ChristianLibrary.Data")));

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register Generic Repository (optional - if you want to inject IRepository<T> directly)
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}