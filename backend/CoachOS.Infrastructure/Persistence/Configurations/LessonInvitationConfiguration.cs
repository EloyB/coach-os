using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonInvitationConfiguration : IEntityTypeConfiguration<LessonInvitation>
{
    public void Configure(EntityTypeBuilder<LessonInvitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(i => i.FirstName)
            .HasMaxLength(100);

        builder.Property(i => i.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.Status).IsRequired();

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Lesson)
            .WithMany()
            .HasForeignKey(i => i.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.LessonId);
        builder.HasIndex(i => new { i.LessonId, i.Email }).IsUnique();
    }
}
