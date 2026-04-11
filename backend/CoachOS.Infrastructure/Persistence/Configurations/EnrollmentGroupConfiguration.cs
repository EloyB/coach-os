using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class EnrollmentGroupConfiguration : IEntityTypeConfiguration<EnrollmentGroup>
{
    public void Configure(EntityTypeBuilder<EnrollmentGroup> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(g => g.Organization)
            .WithMany()
            .HasForeignKey(g => g.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.LessonSerie)
            .WithMany(ls => ls.EnrollmentGroups)
            .HasForeignKey(g => g.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.LeaderEnrollment)
            .WithMany()
            .HasForeignKey(g => g.LeaderEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.LessonSerieId);
        builder.HasIndex(g => g.OrganizationId);
    }
}
