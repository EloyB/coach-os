using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Persistence;

/// <summary>
/// Centrale DbContext voor CoachOS. Uitbreidt IdentityDbContext voor ASP.NET Identity.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<TennisClub> TennisClubs { get; set; } = null!;
    public DbSet<LessonSerie> LessonSeries { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<EnrollmentForm> EnrollmentForms { get; set; } = null!;
    public DbSet<FormField> FormFields { get; set; } = null!;
    public DbSet<FormResponse> FormResponses { get; set; } = null!;
    public DbSet<WeeklyTemplateEntry> WeeklyTemplateEntries { get; set; } = null!;
    public DbSet<EnrollmentGroup> EnrollmentGroups { get; set; } = null!;
    public DbSet<TimeSlotPreference> TimeSlotPreferences { get; set; } = null!;
    public DbSet<ScheduleAssignment> ScheduleAssignments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new OrganizationConfiguration());
        builder.ApplyConfiguration(new TennisClubConfiguration());
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new LessonSerieConfiguration());
        builder.ApplyConfiguration(new LessonConfiguration());
        builder.ApplyConfiguration(new EnrollmentConfiguration());
        builder.ApplyConfiguration(new PaymentConfiguration());
        builder.ApplyConfiguration(new SubscriptionConfiguration());
        builder.ApplyConfiguration(new EnrollmentFormConfiguration());
        builder.ApplyConfiguration(new FormFieldConfiguration());
        builder.ApplyConfiguration(new FormResponseConfiguration());
        builder.ApplyConfiguration(new WeeklyTemplateEntryConfiguration());
        builder.ApplyConfiguration(new EnrollmentGroupConfiguration());
        builder.ApplyConfiguration(new TimeSlotPreferenceConfiguration());
        builder.ApplyConfiguration(new ScheduleAssignmentConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in
            ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = entry.Entity.Id == Guid.Empty ? Guid.NewGuid() : entry.Entity.Id;
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in
            ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = entry.Entity.Id == Guid.Empty ? Guid.NewGuid() : entry.Entity.Id;
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
