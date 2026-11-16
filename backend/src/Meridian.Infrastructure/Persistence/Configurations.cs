using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meridian.Infrastructure.Persistence;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("sites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(200).IsRequired();
    }
}

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssetTagValue).HasColumnName("asset_tag").HasMaxLength(16).IsRequired();
        builder.HasIndex(x => x.AssetTagValue).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(80).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => x.Status);
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
    }
}

internal sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.LaborHours).HasPrecision(6, 2);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssetId);
        builder.HasIndex(x => x.DueAt);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class WorkOrderCommentConfiguration : IEntityTypeConfiguration<WorkOrderComment>
{
    public void Configure(EntityTypeBuilder<WorkOrderComment> builder)
    {
        builder.ToTable("work_order_comments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.WorkOrderId);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("inspections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Findings).HasMaxLength(2000);
        builder.HasIndex(x => x.AssetId);
        builder.HasIndex(x => x.InspectedAt);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.InspectorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.ReplacedByHash).HasMaxLength(64);
        builder.HasIndex(x => x.UserId);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("number_sequences");
        builder.HasKey(x => x.Name);
        builder.Property(x => x.Name).HasMaxLength(40);
        builder.HasData(NumberSequence.Start(NumberSequence.WorkOrder, 1));
    }
}
