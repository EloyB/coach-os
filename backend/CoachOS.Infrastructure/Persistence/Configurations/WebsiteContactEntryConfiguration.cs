using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public sealed class WebsiteContactEntryConfiguration : IEntityTypeConfiguration<WebsiteContactEntry>
{
    public void Configure(EntityTypeBuilder<WebsiteContactEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
        builder.Property(x => x.Organization).HasMaxLength(100);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.HasIndex(x => x.CreatedAt);
    }
}
