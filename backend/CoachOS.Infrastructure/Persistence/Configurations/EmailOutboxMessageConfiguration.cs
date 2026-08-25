using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OrganizationId).IsRequired();
        builder.Property(e => e.EnrollmentId).IsRequired();
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.HasIndex(e => new { e.Status, e.AvailableAt });
        builder.HasIndex(e => e.EnrollmentId);
    }
}
