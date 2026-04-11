using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class WeeklyTemplateEntryConfiguration : IEntityTypeConfiguration<WeeklyTemplateEntry>
{
    public void Configure(EntityTypeBuilder<WeeklyTemplateEntry> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CourtName)
            .HasMaxLength(100);

        builder.HasOne(w => w.LessonSerie)
            .WithMany(ls => ls.WeeklyTemplate)
            .HasForeignKey(w => w.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.LessonSerieId);

        builder.HasIndex(w => new { w.LessonSerieId, w.DayOfWeek, w.StartTime, w.CourtName })
            .IsUnique()
            .HasFilter("\"CourtName\" IS NOT NULL");
    }
}
