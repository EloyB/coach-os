using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampDayPartConfiguration : IEntityTypeConfiguration<CampDayPart>
{
    public void Configure(EntityTypeBuilder<CampDayPart> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(60).IsRequired();
        builder.Property(p => p.Order).IsRequired();
        builder.Property(p => p.StartTime).IsRequired();
        builder.Property(p => p.EndTime).IsRequired();

        builder.HasOne(p => p.CampDay)
            .WithMany(d => d.Parts)
            .HasForeignKey(p => p.CampDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CampDayId);
        builder.HasIndex(p => p.OrganizationId);
    }
}
