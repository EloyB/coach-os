using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Type)
            .IsRequired();

        builder.Property(f => f.Options)
            .HasMaxLength(2000);

        builder.HasOne(f => f.EnrollmentForm)
            .WithMany(ef => ef.Fields)
            .HasForeignKey(f => f.EnrollmentFormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.EnrollmentFormId);
    }
}
