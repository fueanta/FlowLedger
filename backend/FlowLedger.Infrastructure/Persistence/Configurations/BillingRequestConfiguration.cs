using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class BillingRequestConfiguration : IEntityTypeConfiguration<BillingRequest>
{
    public void Configure(EntityTypeBuilder<BillingRequest> builder)
    {
        builder.ToTable("BillingRequests", table => table.IsTemporal(temporal =>
        {
            temporal.UseHistoryTable("BillingRequestsHistory");
            temporal.HasPeriodStart("ValidFrom");
            temporal.HasPeriodEnd("ValidTo");
        }));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.AssignedQueue).HasConversion<int>();

        builder.HasIndex(x => x.RequestNumber)
            .IsUnique();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssignedQueue);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(FlowLedgerSeedData.BillingRequests);
    }
}
