using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.MonthlyPrice)
            .HasPrecision(10, 2); // nullable now

        builder.Property(s => s.MollieSubscriptionId)
            .HasMaxLength(100);

        builder.Property(s => s.MollieCustomerId)
            .HasMaxLength(100);

        builder.HasOne(s => s.Organization)
            .WithOne(o => o.Subscription)
            .HasForeignKey<Subscription>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrganizationId).IsUnique();
    }
}
