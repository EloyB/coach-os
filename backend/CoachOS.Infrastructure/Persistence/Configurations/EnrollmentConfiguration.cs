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

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
            .WithMany(l => l.Enrollments)
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.LessonSeries)
            .WithMany(ls => ls.Enrollments)
            .HasForeignKey(e => e.LessonSeriesId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // StudentId verwijst naar ApplicationUser (geen nav property in Domain)
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.LessonId);
        builder.HasIndex(e => e.LessonSeriesId);
    }
}
