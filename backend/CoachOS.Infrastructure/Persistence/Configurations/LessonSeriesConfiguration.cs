using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonSeriesConfiguration : IEntityTypeConfiguration<LessonSeries>
{
    public void Configure(EntityTypeBuilder<LessonSeries> builder)
    {
        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ls => ls.Description)
            .HasMaxLength(1000);

        builder.Property(ls => ls.Price)
            .HasPrecision(10, 2);

        builder.Property(ls => ls.Level)
            .IsRequired();

        builder.HasOne(ls => ls.Organization)
            .WithMany(o => o.LessonSeries)
            .HasForeignKey(ls => ls.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ls => ls.TennisClub)
            .WithMany()
            .HasForeignKey(ls => ls.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        // TrainerId verwijst naar ApplicationUser (geen nav property in Domain)
        builder.HasIndex(ls => ls.OrganizationId);
        builder.HasIndex(ls => ls.TrainerId);
        builder.HasIndex(ls => ls.TennisClubId);
    }
}
