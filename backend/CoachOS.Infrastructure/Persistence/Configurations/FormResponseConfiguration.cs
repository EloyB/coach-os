using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class FormResponseConfiguration : IEntityTypeConfiguration<FormResponse>
{
    public void Configure(EntityTypeBuilder<FormResponse> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Value)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(r => r.Enrollment)
            .WithMany(e => e.FormResponses)
            .HasForeignKey(r => r.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.FormField)
            .WithMany(f => f.Responses)
            .HasForeignKey(r => r.FormFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.EnrollmentId);
        builder.HasIndex(r => r.FormFieldId);
    }
}
