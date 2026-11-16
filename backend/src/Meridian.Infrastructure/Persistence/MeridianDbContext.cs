using Meridian.Application.Abstractions;

namespace Meridian.Infrastructure.Persistence;

public sealed class MeridianDbContext : DbContext, IUnitOfWork
{
    public MeridianDbContext(DbContextOptions<MeridianDbContext> options) : base(options)
    {
    }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderComment> WorkOrderComments => Set<WorkOrderComment>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcInstantConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeridianDbContext).Assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Meridian.Domain.Common.Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(Meridian.Domain.Common.Entity.Id)).ValueGeneratedNever();
            }
        }
    }
}

/// <summary>
/// Stores instants as UTC <see cref="DateTime"/> so both PostgreSQL and SQLite can filter and sort them.
/// </summary>
public sealed class UtcInstantConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTime>
{
    public UtcInstantConverter()
        : base(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
    {
    }
}
