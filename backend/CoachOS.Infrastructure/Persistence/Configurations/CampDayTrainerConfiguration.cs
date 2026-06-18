using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampDayTrainerConfiguration : IEntityTypeConfiguration<CampDayTrainer>
{
    public void Configure(EntityTypeBuilder<CampDayTrainer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.StartTime).IsRequired();
        builder.Property(t => t.EndTime).IsRequired();

        builder.HasOne(t => t.CampDay)
            .WithMany(d => d.TrainerAssignments)
            .HasForeignKey(t => t.CampDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.CampDayId);
        builder.HasIndex(t => new { t.TrainerId, t.OrganizationId });
    }
}
