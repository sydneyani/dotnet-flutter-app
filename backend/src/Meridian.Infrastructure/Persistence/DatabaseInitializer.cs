using Meridian.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meridian.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MeridianDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (bool.TryParse(configuration["Seed:Enabled"], out var seed) && seed)
        {
            await MeridianSeeder.SeedAsync(scope.ServiceProvider, cancellationToken);
        }
    }
}
