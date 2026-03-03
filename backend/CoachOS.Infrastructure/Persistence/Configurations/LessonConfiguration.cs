using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Notes)
            .HasMaxLength(1000);

        builder.Property(l => l.CancellationReason)
            .HasMaxLength(500);

        builder.Property(l => l.Level)
            .IsRequired();

        builder.HasOne(l => l.Organization)
            .WithMany()
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LessonSeries)
            .WithMany(ls => ls.Lessons)
            .HasForeignKey(l => l.LessonSeriesId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(l => l.Court)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.OrganizationId);
        builder.HasIndex(l => l.TrainerId);
        builder.HasIndex(l => l.CourtId);
        builder.HasIndex(l => l.Date);
        builder.HasIndex(l => new { l.OrganizationId, l.Date });
    }
}
