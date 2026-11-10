using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class PrivacyErasureReceiptConfiguration : IEntityTypeConfiguration<PrivacyErasureReceipt>
{
    public void Configure(EntityTypeBuilder<PrivacyErasureReceipt> builder)
    {
        builder.ToTable("PrivacyErasureReceipts");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);
        // Bestaande bewijzen (van vóór anonimiseren bestond) zijn allemaal verwijderingen.
        builder.Property(r => r.Method)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PrivacyErasureMethod.Deleted)
            .HasSentinel((PrivacyErasureMethod)0);
        builder.Property(r => r.SubjectId).IsRequired();
        builder.Property(r => r.ErasedAt).IsRequired();

        // Bewust geen FK: het bewijs moet het gewiste record en de organisatie kunnen overleven.
        builder.HasIndex(r => new { r.OrganizationId, r.Kind, r.SubjectId }).IsUnique();
    }
}
