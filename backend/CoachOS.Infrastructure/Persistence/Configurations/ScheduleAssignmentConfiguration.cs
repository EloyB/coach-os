using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class ScheduleAssignmentConfiguration : IEntityTypeConfiguration<ScheduleAssignment>
{
    public void Configure(EntityTypeBuilder<ScheduleAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .IsRequired();

        builder.Property(a => a.IsLocked)
            .HasDefaultValue(false);

        builder.HasOne(a => a.Organization)
            .WithMany()
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.LessonSerie)
            .WithMany(ls => ls.ScheduleAssignments)
            .HasForeignKey(a => a.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.WeeklyTemplateEntry)
            .WithMany(w => w.Assignments)
            .HasForeignKey(a => a.WeeklyTemplateEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.EnrollmentGroup)
            .WithMany()
            .HasForeignKey(a => a.EnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Enrollment)
            .WithMany()
            .HasForeignKey(a => a.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.LessonSerieId, a.WeeklyTemplateEntryId });
        builder.HasIndex(a => a.OrganizationId);
    }
}
