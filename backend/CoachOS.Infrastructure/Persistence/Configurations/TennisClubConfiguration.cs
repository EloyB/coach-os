using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class TennisClubConfiguration : IEntityTypeConfiguration<TennisClub>
{
    public void Configure(EntityTypeBuilder<TennisClub> builder)
    {
        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tc => tc.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(tc => tc.Organization)
            .WithMany()
            .HasForeignKey(tc => tc.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tc => tc.OrganizationId);
    }
}
