using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampConfiguration : IEntityTypeConfiguration<Camp>
{
    public void Configure(EntityTypeBuilder<Camp> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.Price).HasColumnType("numeric(10,2)");
        builder.Property(c => c.StartDate).IsRequired();
        builder.Property(c => c.EndDate).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.TennisClub)
            .WithMany()
            .HasForeignKey(c => c.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict is bewust (projectregel: nooit cascade; spiegelt EnrollmentFormConfiguration):
        // een toekomstige camp-delete moet het formulier dus eerst zelf verwijderen.
        builder.HasOne(c => c.EnrollmentForm)
            .WithOne(f => f.Camp)
            .HasForeignKey<CampEnrollmentForm>(f => f.CampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrganizationId);
    }
}
