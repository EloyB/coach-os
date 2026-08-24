using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class HeadTrainerClubConfiguration : IEntityTypeConfiguration<HeadTrainerClub>
{
    public void Configure(EntityTypeBuilder<HeadTrainerClub> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Membership)
            .WithMany(m => m.HeadTrainerClubs)
            .HasForeignKey(h => h.OrganizationMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.TennisClub)
            .WithMany()
            .HasForeignKey(h => h.TennisClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.OrganizationMembershipId, h.TennisClubId }).IsUnique();
    }
}
