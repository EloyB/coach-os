using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.CourtName)
            .HasMaxLength(100);

        builder.Property(l => l.Notes)
            .HasMaxLength(1000);

        builder.Property(l => l.CancellationReason)
            .HasMaxLength(500);

        builder.HasOne(l => l.Organization)
            .WithMany()
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LessonSerie)
            .WithMany(ls => ls.Lessons)
            .HasForeignKey(l => l.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(l => l.RescheduledToLesson)
            .WithOne()
            .HasForeignKey<Lesson>(l => l.RescheduledToLessonId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Optionele terugverwijzing naar het weekslot. SetNull: verwijderen van een
        // weekslot maakt de les los i.p.v. te blokkeren of mee te cascade-deleten.
        builder.HasOne(l => l.WeeklyTemplateEntry)
            .WithMany()
            .HasForeignKey(l => l.WeeklyTemplateEntryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(l => l.WeeklyTemplateEntryId);

        builder.HasIndex(l => l.OrganizationId);
        builder.HasIndex(l => l.TrainerId);
        builder.HasIndex(l => l.Date);
        builder.HasIndex(l => new { l.OrganizationId, l.Date });
        builder.HasIndex(l => new { l.OrganizationId, l.Date, l.CourtName });
    }
}
