using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class RescheduleRequestConfiguration : IEntityTypeConfiguration<RescheduleRequest>
{
    public void Configure(EntityTypeBuilder<RescheduleRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.ResolverNote)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Enrollment)
            .WithMany()
            .HasForeignKey(r => r.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.EnrollmentGroup)
            .WithMany()
            .HasForeignKey(r => r.EnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ScheduleAssignment)
            .WithMany()
            .HasForeignKey(r => r.ScheduleAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AlternativeWeeklyTemplateEntry)
            .WithMany()
            .HasForeignKey(r => r.AlternativeWeeklyTemplateEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.OrganizationId);
        builder.HasIndex(r => r.ScheduleAssignmentId);
    }
}
