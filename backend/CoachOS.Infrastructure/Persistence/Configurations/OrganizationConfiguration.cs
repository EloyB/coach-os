using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.Phone)
            .HasMaxLength(20);

        builder.Property(o => o.Address)
            .HasMaxLength(300);

        builder.Property(o => o.City)
            .HasMaxLength(100);

        builder.Property(o => o.PostalCode)
            .HasMaxLength(10);

        builder.Property(o => o.Country)
            .IsRequired()
            .HasMaxLength(2)
            .HasDefaultValue("BE");

        builder.Property(o => o.LogoUrl)
            .HasMaxLength(500);

        builder.HasIndex(o => o.Email).IsUnique();
    }
}
