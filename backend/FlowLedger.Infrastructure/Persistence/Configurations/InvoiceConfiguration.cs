using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", table => table.IsTemporal(temporal =>
        {
            temporal.UseHistoryTable("InvoicesHistory");
            temporal.HasPeriodStart("ValidFrom");
            temporal.HasPeriodEnd("ValidTo");
        }));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatPercentage).HasPrecision(5, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.InvoiceNumber)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.BillingRequest)
            .WithOne(x => x.Invoice)
            .HasForeignKey<Invoice>(x => x.BillingRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(FlowLedgerSeedData.Invoices);
    }
}
