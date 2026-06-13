using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentConfiguration : IEntityTypeConfiguration<CampEnrollment>
{
    public void Configure(EntityTypeBuilder<CampEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.ParticipantName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ParticipantEmail).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ParticipantPhone).HasMaxLength(30);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Camp)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.CampEnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.CampId);
        builder.HasIndex(e => e.ParticipantEmail);

        // Voorkom dubbele actieve inschrijving voor hetzelfde e-mailadres + kamp.
        builder.HasIndex(e => new { e.CampId, e.ParticipantEmail })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2, 5)");
    }
}
