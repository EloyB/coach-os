using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Persistence;

/// <summary>
/// Centrale DbContext voor CoachOS. Uitbreidt IdentityDbContext voor ASP.NET Identity.
///
/// Multi-tenancy: alle entities met een <c>OrganizationId</c> krijgen een global
/// query filter. De filter is "loose" — hij beperkt tot de actieve tenant zodra
/// er één gezet is, maar laat queries door als er geen tenant is (anonieme/publieke
/// requests zoals enrollment, student magic-link, trainer invite accept).
/// Zo hoeven publieke paden niet overal <c>IgnoreQueryFilters()</c> aan te roepen.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenant)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private readonly ITenantContext _tenant = tenant;

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
    public DbSet<AssignmentConfirmationToken> AssignmentConfirmationTokens { get; set; } = null!;
    public DbSet<MagicLinkToken> MagicLinkTokens { get; set; } = null!;
    public DbSet<OrganizationMembership> OrganizationMemberships { get; set; } = null!;

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
        builder.ApplyConfiguration(new MagicLinkTokenConfiguration());
        builder.ApplyConfiguration(new OrganizationMembershipConfiguration());

        ApplyTenantFilters(builder);
    }

    /// <summary>
    /// Global query filters: loose — alleen actief wanneer er een tenant is gezet.
    /// Ontbreekt de tenant (publieke/anonieme request) dan blijft de query ongefilterd
    /// zodat bestaande publieke flows (enrollment, magic-link, invite accept) werken.
    /// Geauthenticeerde requests worden automatisch gescoped op de active organisatie.
    /// </summary>
    private void ApplyTenantFilters(ModelBuilder builder)
    {
        builder.Entity<TennisClub>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<LessonSerie>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<Lesson>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<Enrollment>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<EnrollmentForm>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<EnrollmentGroup>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<Payment>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<Subscription>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<ScheduleAssignment>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<AssignmentConfirmationToken>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
        builder.Entity<TimeSlotPreference>().HasQueryFilter(e =>
            _tenant.OrganizationId == Guid.Empty || e.OrganizationId == _tenant.OrganizationId);
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
