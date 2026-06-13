using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampDayConfiguration : IEntityTypeConfiguration<CampDay>
{
    public void Configure(EntityTypeBuilder<CampDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date).IsRequired();
        builder.Property(d => d.StartTime).IsRequired();
        builder.Property(d => d.EndTime).IsRequired();

        builder.HasOne(d => d.Camp)
            .WithMany(c => c.Days)
            .HasForeignKey(d => d.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CampId);
        builder.HasIndex(d => d.OrganizationId);
    }
}
