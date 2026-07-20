using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class LessonSeriePriceConfiguration : IEntityTypeConfiguration<LessonSeriePrice>
{
    public void Configure(EntityTypeBuilder<LessonSeriePrice> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalPrice)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Category)
            .IsRequired();

        builder.Property(p => p.GroupSize)
            .IsRequired();

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LessonSerie)
            .WithMany(ls => ls.Prices)
            .HasForeignKey(p => p.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Eén tarief per (reeks, categorie, groepsgrootte) — voorkomt dubbele cellen
        // in de matrix, die een niet-deterministische prijs zouden opleveren.
        builder.HasIndex(p => new { p.LessonSerieId, p.Category, p.GroupSize })
            .IsUnique();

        builder.HasIndex(p => p.OrganizationId);
    }
}
