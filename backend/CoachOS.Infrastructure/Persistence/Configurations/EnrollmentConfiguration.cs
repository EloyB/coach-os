using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachOS.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.StudentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ContactEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StudentEmail)
            .HasMaxLength(200);

        // Genormaliseerde naam als stored computed kolom: de unique index hieronder
        // moet deterministisch zijn en mag niet van de Postgres-collatie afhangen.
        builder.Property<string>("StudentNameNormalized")
            .HasComputedColumnSql("lower(btrim(\"StudentName\"))", stored: true);

        builder.Property(e => e.StudentPhone)
            .HasMaxLength(30);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Nullable: bestaande inschrijvingen van vóór de tariefcategorieën hebben
        // geen geboortedatum. Nieuwe inschrijvingen dwingt de validator af.
        builder.Property(e => e.DateOfBirth);

        builder.Property(e => e.Category);

        builder.Property(e => e.SelectedPriceOptionId);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
            .WithMany(l => l.Enrollments)
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.LessonSerie)
            .WithMany(ls => ls.Enrollments)
            .HasForeignKey(e => e.LessonSerieId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(e => e.EnrollmentGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.EnrollmentGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.ContactEmail);
        builder.HasIndex(e => e.LessonId);
        builder.HasIndex(e => e.LessonSerieId);

        // Dezelfde persoon mag niet twee keer in dezelfde reeks staan; verschillende
        // personen op één contactadres mogen wél. Partieel op DateOfBirth: rijen van
        // vóór de geboortedatum-feature zouden de index anders blokkeren.
        // Statussen 1, 2, 5 = Pending, Confirmed, PendingPayment.
        builder.HasIndex(nameof(Enrollment.LessonSerieId), nameof(Enrollment.ContactEmail),
                "StudentNameNormalized", nameof(Enrollment.DateOfBirth))
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_Participant")
            .HasFilter("\"DateOfBirth\" IS NOT NULL AND \"Status\" IN (1, 2, 5)");
    }
}
