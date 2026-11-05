using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Subscriptions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationPublicScopeResolver(ApplicationDbContext db) : IOrganizationPublicScopeResolver
{
    public async Task<Guid?> ResolveAsync(string resource, string value, CancellationToken ct = default)
    {
        if (resource is "lessonseries" or "camps" or "camp-enrollments" or "payments" && Guid.TryParse(value, out Guid id))
        {
            // Public resource identity, not an unrelated caller's active organization.
            return resource switch
            {
                "lessonseries" => await db.LessonSeries.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
                "camps" => await db.Camps.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
                "camp-enrollments" => await db.CampEnrollments.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
                "payments" => await db.Enrollments.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
                _ => null
            };
        }
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())));
        return resource switch
        {
            "confirmation" => await db.AssignmentConfirmationTokens.IgnoreQueryFilters().AsNoTracking().Where(x => x.TokenHash == hash).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
            "invitations" => await db.LessonInvitations.IgnoreQueryFilters().AsNoTracking().Where(x => x.TokenHash == hash).Select(x => (Guid?)x.OrganizationId).SingleOrDefaultAsync(ct),
            _ => null
        };
    }
}
