using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.MolliePaymentId)
            .HasMaxLength(100);

        builder.Property(p => p.MollieCheckoutUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Enrollment)
            .WithMany(e => e.Payments)
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.EnrollmentId);
        builder.HasIndex(p => p.MolliePaymentId);
    }
}
