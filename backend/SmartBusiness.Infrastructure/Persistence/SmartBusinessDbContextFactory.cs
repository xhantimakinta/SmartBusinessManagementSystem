using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartBusiness.Infrastructure.Persistence;

public sealed class SmartBusinessDbContextFactory : IDesignTimeDbContextFactory<SmartBusinessDbContext>
{
    public SmartBusinessDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SmartBusiness")
            ?? throw new InvalidOperationException("ConnectionStrings__SmartBusiness must be set for EF Core design-time commands.");
        var optionsBuilder = new DbContextOptionsBuilder<SmartBusinessDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SmartBusinessDbContext(optionsBuilder.Options);
    }
}