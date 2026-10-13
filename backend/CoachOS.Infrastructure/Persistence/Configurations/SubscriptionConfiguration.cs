using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Plan)
            .IsRequired();

        builder.Property(s => s.Interval)
            .IsRequired();

        builder.Property(s => s.Price)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(s => s.MollieCustomerId)
            .HasMaxLength(100);

        builder.Property(s => s.MollieSubscriptionId)
            .HasMaxLength(100);

        builder.Property(s => s.LastPaymentId)
            .HasMaxLength(100);

        builder.Property(s => s.LastCheckoutUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.LastPaymentStatus)
            .HasMaxLength(50);

        builder.HasOne(s => s.Organization)
            .WithOne(o => o.Subscription)
            .HasForeignKey<Subscription>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrganizationId).IsUnique();
    }
}
