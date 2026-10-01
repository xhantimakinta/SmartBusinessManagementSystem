using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartBusiness.Infrastructure.Persistence;

namespace SmartBusiness.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SmartBusiness");

        services.AddDbContext<SmartBusinessDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString) ||
                connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString is null || string.IsNullOrWhiteSpace(connectionString)
                    ? "Data Source=smartbusiness.db"
                    : connectionString);
                return;
            }

            options.UseSqlServer(connectionString);
        });

        return services;
    }
}