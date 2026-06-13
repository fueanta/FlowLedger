using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.PasswordSalt)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasData(FlowLedgerSeedData.Users);
    }
}
