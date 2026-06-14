using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class TrainerAvailabilityConfiguration : IEntityTypeConfiguration<TrainerAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DayOfWeek).IsRequired();
        builder.Property(a => a.StartTime).IsRequired();
        builder.Property(a => a.EndTime).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();

        builder.HasOne(a => a.Organization)
            .WithMany()
            .HasForeignKey(a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TennisClub)
            .WithMany()
            .HasForeignKey(a => a.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.TrainerId, a.DayOfWeek });
    }
}
