using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampPackageSlotConfiguration : IEntityTypeConfiguration<CampPackageSlot>
{
    public void Configure(EntityTypeBuilder<CampPackageSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.PartOrder).IsRequired();

        builder.HasOne(s => s.CampPackage)
            .WithMany(p => p.Slots)
            .HasForeignKey(s => s.CampPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.OrganizationId);
        builder.HasIndex(s => new { s.CampPackageId, s.Date, s.PartOrder }).IsUnique();
    }
}
