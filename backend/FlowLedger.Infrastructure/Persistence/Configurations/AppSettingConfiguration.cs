using FlowLedger.Domain.Entities;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowLedger.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings", table => table.IsTemporal(temporal =>
        {
            temporal.UseHistoryTable("AppSettingsHistory");
            temporal.HasPeriodStart("ValidFrom");
            temporal.HasPeriodEnd("ValidTo");
        }));

        builder.HasKey(x => x.Key);

        builder.Property(x => x.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasData(FlowLedgerSeedData.AppSettings);
    }
}
