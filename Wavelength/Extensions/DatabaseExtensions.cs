using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Wavelength.Repositories;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabaseAccess(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure DbContext with PostgreSQL
            services.AddDbContext<AppDbContext>(
                options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Add database access services
            services.AddScoped<EmailVaidationRepository>();

            return services;
        }
    }
}
