using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.OrganizationId).IsRequired();
        builder.Property(m => m.Role).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();
        builder.Property(m => m.WeeklyCapacityHours).IsRequired().HasDefaultValue(16);
        builder.Property(m => m.Notes).HasMaxLength(1000);
        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasOne(m => m.Organization)
            .WithMany()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.UserId, m.OrganizationId }).IsUnique();
        builder.HasIndex(m => m.OrganizationId);
        builder.HasIndex(m => m.UserId);
    }
}
