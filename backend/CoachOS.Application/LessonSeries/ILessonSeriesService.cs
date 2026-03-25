using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSeries;

public interface ILessonSeriesService
{
    Task<Result<List<LessonSeriesDto>>> GetAllAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<List<LessonSeriesMemberDto>>> GetMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result<LessonSeriesDto>> UpdateAsync(Guid id, Guid organizationId, UpdateLessonSeriesRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> AddLessonAsync(Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default);
}
