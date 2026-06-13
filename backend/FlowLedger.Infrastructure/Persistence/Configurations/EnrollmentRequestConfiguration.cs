using FlowLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentRequestConfiguration : IEntityTypeConfiguration<EnrollmentRequest>
{
    public void Configure(EntityTypeBuilder<EnrollmentRequest> builder)
    {
        builder.ToTable("EnrollmentRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .HasMaxLength(160)
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

        builder.Property(x => x.DecisionReason)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
