using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityNumber)
            .HasMaxLength(200);

        builder.Property(x => x.ActorDisplayName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.BeforeStatus)
            .HasMaxLength(80);

        builder.Property(x => x.AfterStatus)
            .HasMaxLength(80);

        builder.Property(x => x.MetadataJson)
            .HasMaxLength(4000);

        builder.HasIndex(x => new { x.BillingRequestId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });

        builder.HasOne(x => x.BillingRequest)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.BillingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ActorUser)
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(FlowLedgerSeedData.AuditLogs);
    }
}
