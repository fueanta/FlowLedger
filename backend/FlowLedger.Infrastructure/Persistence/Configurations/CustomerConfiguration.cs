using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ContactPerson)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.BillingAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.TaxIdentifier)
            .HasMaxLength(80)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.ContactEmail);
        builder.HasIndex(x => x.Status);

        builder.HasData(FlowLedgerSeedData.Customers);
    }
}
