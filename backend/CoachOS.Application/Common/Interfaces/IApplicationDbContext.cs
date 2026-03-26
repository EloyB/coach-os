using CoachOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<TennisClub> TennisClubs { get; }
    DbSet<Domain.Entities.LessonSeries> LessonSeries { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Subscription> Subscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
