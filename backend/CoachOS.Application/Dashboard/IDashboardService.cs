using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<InboxDto>> GetInboxAsync(Guid organizationId, int limit = 10, CancellationToken ct = default);
    Task<Result<List<MyLessonDto>>> GetMyLessonsAsync(
        Guid organizationId, Guid trainerId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<MyLessonDetailDto>> GetMyLessonAsync(
        Guid organizationId, Guid trainerId, Guid lessonId, CancellationToken ct = default);
    Task<Result<DashboardMetricsDto>> GetMetricsAsync(Guid organizationId, int weeks = 7, CancellationToken ct = default);
}
