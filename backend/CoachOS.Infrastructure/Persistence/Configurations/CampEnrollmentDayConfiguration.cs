using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentDayConfiguration : IEntityTypeConfiguration<CampEnrollmentDay>
{
    public void Configure(EntityTypeBuilder<CampEnrollmentDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.CampEnrollment)
            .WithMany(e => e.Days)
            .HasForeignKey(d => d.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.CampDay)
            .WithMany()
            .HasForeignKey(d => d.CampDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.CampEnrollmentId, d.CampDayId }).IsUnique();
        builder.HasIndex(d => d.CampDayId);
        builder.HasIndex(d => d.OrganizationId);

        builder.ToTable(t => t.HasCheckConstraint("CK_CampEnrollmentDays_AtLeastOnePart", "\"Morning\" OR \"Afternoon\""));
    }
}
