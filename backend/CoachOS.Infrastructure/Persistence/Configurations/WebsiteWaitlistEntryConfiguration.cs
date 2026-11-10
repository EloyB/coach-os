using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public sealed class WebsiteWaitlistEntryConfiguration : IEntityTypeConfiguration<WebsiteWaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WebsiteWaitlistEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Role).HasMaxLength(10);
        builder.Property(x => x.UnsubscribeTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.UnsubscribeTokenHash).IsUnique();
    }
}

public sealed class WebsiteWaitlistMailConfiguration : IEntityTypeConfiguration<WebsiteWaitlistMail>
{
    public void Configure(EntityTypeBuilder<WebsiteWaitlistMail> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.EntryId).IsUnique();
        builder.HasIndex(x => x.AvailableAt);
        builder.HasOne<WebsiteWaitlistEntry>().WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
    }
}
