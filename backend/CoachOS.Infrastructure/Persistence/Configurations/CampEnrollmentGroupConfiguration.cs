using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentGroupConfiguration : IEntityTypeConfiguration<CampEnrollmentGroup>
{
    public void Configure(EntityTypeBuilder<CampEnrollmentGroup> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(g => g.Camp)
            .WithMany()
            .HasForeignKey(g => g.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.LeaderEnrollment)
            .WithMany()
            .HasForeignKey(g => g.LeaderEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.CampId);
        builder.HasIndex(g => g.OrganizationId);
    }
}
