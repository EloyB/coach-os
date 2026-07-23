using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.StudentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StudentEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StudentPhone)
            .HasMaxLength(30);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Nullable: bestaande inschrijvingen van vóór de tariefcategorieën hebben
        // geen geboortedatum. Nieuwe inschrijvingen dwingt de validator af.
        builder.Property(e => e.DateOfBirth);

        builder.Property(e => e.Category);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
            .WithMany(l => l.Enrollments)
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.LessonSerie)
            .WithMany(ls => ls.Enrollments)
            .HasForeignKey(e => e.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.EnrollmentGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.EnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.StudentEmail);
        builder.HasIndex(e => e.LessonId);
        builder.HasIndex(e => e.LessonSerieId);

        // Prevent duplicate active enrollments for the same email + series
        builder.HasIndex(e => new { e.LessonSerieId, e.StudentEmail })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2)");
    }
}
