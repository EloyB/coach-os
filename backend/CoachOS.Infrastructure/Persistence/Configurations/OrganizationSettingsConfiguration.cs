using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.AdminsActAsTrainers)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.DefaultPaymentMode)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.PaymentMode.Immediate);

        builder.Property(s => s.PlatformFeePercentage)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(s => s.PaymentCurrency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EUR");

        builder.Property(s => s.YouthMaxAge)
            .IsRequired()
            .HasDefaultValue(17);

        builder.HasOne(s => s.Organization)
            .WithOne(o => o.Settings)
            .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrganizationId).IsUnique();
    }
}
