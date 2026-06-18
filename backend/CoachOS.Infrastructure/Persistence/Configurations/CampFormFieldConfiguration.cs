using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampFormFieldConfiguration : IEntityTypeConfiguration<CampFormField>
{
    public void Configure(EntityTypeBuilder<CampFormField> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Label).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Type).IsRequired();
        builder.Property(f => f.Options).HasMaxLength(2000);

        builder.HasOne(f => f.CampEnrollmentForm)
            .WithMany(ef => ef.Fields)
            .HasForeignKey(f => f.CampEnrollmentFormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.CampEnrollmentFormId);
    }
}
