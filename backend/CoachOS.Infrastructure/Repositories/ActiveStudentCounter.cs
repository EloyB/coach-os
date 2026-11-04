using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Benadering van "actieve leerlingen": unieke combinatie naam + e-mail over lessenreeksen en
/// kampen. Een speler zonder eigen e-mail (kind van een ouder) telt per naam.
/// </summary>
public class ActiveStudentCounter(ApplicationDbContext context) : IActiveStudentCounter
{
    public async Task<int> CountAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        DateTime fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime toUtcExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        IQueryable<StudentKey> enrollments = context.Enrollments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId
                        && e.Status != EnrollmentStatus.Cancelled
                        && e.EnrolledAt >= fromUtc && e.EnrolledAt < toUtcExclusive)
            .Select(e => new StudentKey { Name = e.StudentName.Trim().ToLower(), Email = e.ContactEmail.Trim().ToLower() });

        IQueryable<StudentKey> campEnrollments = context.CampEnrollments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId
                        && e.Status != EnrollmentStatus.Cancelled
                        && e.EnrolledAt >= fromUtc && e.EnrolledAt < toUtcExclusive)
            .Select(e => new StudentKey { Name = e.ParticipantName.Trim().ToLower(), Email = e.ParticipantEmail.Trim().ToLower() });

        return await enrollments.Union(campEnrollments).CountAsync(ct);
    }

    private sealed class StudentKey
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
