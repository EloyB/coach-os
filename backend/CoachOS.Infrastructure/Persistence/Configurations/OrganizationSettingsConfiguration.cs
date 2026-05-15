using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.AdminsActAsTrainers)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(s => s.Organization)
            .WithOne(o => o.Settings)
            .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrganizationId).IsUnique();
    }
}
