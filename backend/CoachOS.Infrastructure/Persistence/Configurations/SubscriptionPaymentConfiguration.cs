using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.AmountExclVat).HasPrecision(10, 2);
        builder.Property(p => p.AmountInclVat).HasPrecision(10, 2);
        builder.Property(p => p.VatRate).HasPrecision(5, 4);
        builder.Property(p => p.MolliePaymentId).HasMaxLength(100);
        builder.Property(p => p.CheckoutUrl).HasMaxLength(1000);
        builder.Property(p => p.InvoiceId).HasMaxLength(100);
        builder.Property(p => p.InvoiceNumber).HasMaxLength(50);
        builder.Property(p => p.InvoicePdfUrl).HasMaxLength(1000);
        builder.Property(p => p.InvoiceLastError).HasMaxLength(2000);
        builder.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Eén rij per Mollie-betaling: webhooks mogen dubbel of gelijktijdig binnenkomen.
        builder.HasIndex(p => p.MolliePaymentId)
            .IsUnique()
            .HasFilter("\"MolliePaymentId\" IS NOT NULL");

        builder.HasIndex(p => new { p.SubscriptionId, p.PeriodStart });
        builder.HasIndex(p => p.InvoiceStatus);
        builder.HasIndex(p => p.OrganizationId);
    }
}
