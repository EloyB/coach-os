using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<InboxDto>> GetInboxAsync(Guid organizationId, int limit = 10, CancellationToken ct = default);
    Task<Result<List<MyLessonDto>>> GetMyLessonsAsync(
        Guid organizationId, Guid trainerId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<MyOverviewDto>> GetMyOverviewAsync(
        Guid organizationId, Guid trainerId, CancellationToken ct = default);
    Task<Result<List<MyCampDayDto>>> GetMyCampDaysAsync(
        Guid organizationId, Guid trainerId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<List<MyCampDayDto>>> GetOrganizationCampDaysAsync(
        Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<List<MyLessonDto>>> GetMyClubLessonsAsync(
        Guid organizationId, IReadOnlyList<Guid> headTrainerClubIds, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<ClubOverviewDto>> GetMyClubOverviewAsync(
        Guid organizationId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
    Task<Result<List<MyLessonDto>>> GetOrganizationLessonsAsync(
        Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<MyLessonDetailDto>> GetOrganizationLessonAsync(
        Guid organizationId, Guid lessonId, CancellationToken ct = default);
    Task<Result<MyLessonDetailDto>> GetMyLessonAsync(
        Guid organizationId, Guid trainerId, Guid lessonId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
    Task<Result<DashboardMetricsDto>> GetMetricsAsync(Guid organizationId, int weeks = 7, CancellationToken ct = default);
}
