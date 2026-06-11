using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasOne(x => x.BillingRequest)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.BillingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AuthorUser)
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(FlowLedgerSeedData.Comments);
    }
}
