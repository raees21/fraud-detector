using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FraudEngine.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        // This connection string is ONLY used by dotnet-ef cli tool during design time migrations.
        // It does not need to connect to a real database to generate migration classes.
        builder.UseNpgsql("Host=localhost;Database=fraud_db;Username=postgres;Password=postgres");

        return new AppDbContext(builder.Options);
    }
}
