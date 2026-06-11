using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class BillingRequestLineItemConfiguration : IEntityTypeConfiguration<BillingRequestLineItem>
{
    public void Configure(EntityTypeBuilder<BillingRequestLineItem> builder)
    {
        builder.ToTable("BillingRequestLineItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);

        builder.HasOne(x => x.BillingRequest)
            .WithMany(x => x.LineItems)
            .HasForeignKey(x => x.BillingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(FlowLedgerSeedData.LineItems);
    }
}
