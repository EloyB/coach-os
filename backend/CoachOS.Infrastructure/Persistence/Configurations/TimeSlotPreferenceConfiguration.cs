using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class TimeSlotPreferenceConfiguration : IEntityTypeConfiguration<TimeSlotPreference>
{
    public void Configure(EntityTypeBuilder<TimeSlotPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Preference)
            .IsRequired();

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Enrollment)
            .WithMany(e => e.TimeSlotPreferences)
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.WeeklyTemplateEntry)
            .WithMany(w => w.Preferences)
            .HasForeignKey(p => p.WeeklyTemplateEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.EnrollmentId, p.WeeklyTemplateEntryId })
            .IsUnique();

        builder.HasIndex(p => p.OrganizationId);
    }
}
