using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class TermsAcceptanceConfiguration : IEntityTypeConfiguration<TermsAcceptance>
{
    public void Configure(EntityTypeBuilder<TermsAcceptance> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.DocumentType)
            .IsRequired();

        builder.Property(t => t.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.AcceptedAt)
            .IsRequired();

        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.OrganizationId);
        builder.HasIndex(t => t.UserId);
    }
}
