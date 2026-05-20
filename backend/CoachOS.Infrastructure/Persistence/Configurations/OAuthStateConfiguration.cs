using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class OAuthStateConfiguration : IEntityTypeConfiguration<OAuthState>
{
    public void Configure(EntityTypeBuilder<OAuthState> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.State)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(s => s.State).IsUnique();
        builder.HasIndex(s => s.ExpiresAt);
    }
}
