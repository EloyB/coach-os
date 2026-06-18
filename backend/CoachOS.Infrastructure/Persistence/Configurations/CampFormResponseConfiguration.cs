using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class CampFormResponseConfiguration : IEntityTypeConfiguration<CampFormResponse>
{
    public void Configure(EntityTypeBuilder<CampFormResponse> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Value).IsRequired().HasMaxLength(1000);

        builder.HasOne(r => r.CampEnrollment)
            .WithMany(e => e.FormResponses)
            .HasForeignKey(r => r.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CampFormField)
            .WithMany(f => f.Responses)
            .HasForeignKey(r => r.CampFormFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CampEnrollmentId);
        builder.HasIndex(r => r.CampFormFieldId);
    }
}
