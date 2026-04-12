using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class AssignmentConfirmationTokenConfiguration : IEntityTypeConfiguration<AssignmentConfirmationToken>
{
    public void Configure(EntityTypeBuilder<AssignmentConfirmationToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.Response).IsRequired();

        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ScheduleAssignment)
            .WithMany()
            .HasForeignKey(t => t.ScheduleAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Enrollment)
            .WithMany()
            .HasForeignKey(t => t.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.OrganizationId);
        builder.HasIndex(t => t.ScheduleAssignmentId);
    }
}
