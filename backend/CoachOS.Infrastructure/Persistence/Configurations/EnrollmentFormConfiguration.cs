using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class EnrollmentFormConfiguration : IEntityTypeConfiguration<EnrollmentForm>
{
    public void Configure(EntityTypeBuilder<EnrollmentForm> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.LessonSerie)
            .WithOne(ls => ls.EnrollmentForm)
            .HasForeignKey<EnrollmentForm>(f => f.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.LessonSerieId).IsUnique();
        builder.HasIndex(f => f.OrganizationId);
    }
}
