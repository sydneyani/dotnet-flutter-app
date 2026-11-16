using Meridian.Application.Abstractions;
using Meridian.Infrastructure.Persistence;
using Meridian.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meridian.Infrastructure;

public static class DependencyInjection
{
    public const string PostgresProvider = "Postgres";
    public const string SqliteProvider = "Sqlite";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? SqliteProvider;
        var connectionString = configuration["Database:ConnectionString"] ?? "Data Source=meridian.db";

        services.AddDbContext<MeridianDbContext>(options =>
        {
            if (provider.Equals(PostgresProvider, StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<MeridianDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDashboardReader, DashboardReader>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();
        return services;
    }
}
