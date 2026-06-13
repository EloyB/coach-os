using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampEnrollmentFormConfiguration : IEntityTypeConfiguration<CampEnrollmentForm>
{
    public void Configure(EntityTypeBuilder<CampEnrollmentForm> builder)
    {
        builder.HasKey(f => f.Id);
        // 1:1 met Camp wordt geconfigureerd in CampConfiguration (HasForeignKey<CampEnrollmentForm>).
        builder.HasIndex(f => f.CampId).IsUnique();
        builder.HasIndex(f => f.OrganizationId);
    }
}
