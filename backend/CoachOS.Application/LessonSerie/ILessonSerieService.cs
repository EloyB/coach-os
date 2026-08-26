using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSerie;

public interface ILessonSerieService
{
    Task<Result<List<LessonSerieDto>>> GetAllAsync(Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
    Task<Result<LessonSerieDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<List<LessonSerieMemberDto>>> GetMembersAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateLessonSerieRequest request, CancellationToken ct = default);
    Task<Result<LessonSerieDto>> UpdateAsync(Guid id, Guid organizationId, UpdateLessonSerieRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> AddLessonAsync(Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default);
    Task<Result<Guid>> AddWeeklyTemplateEntryAsync(Guid seriesId, Guid organizationId, AddWeeklyTemplateEntryRequest request, CancellationToken ct = default);
    Task<Result<LessonDto>> UpdateLessonAsync(Guid seriesId, Guid lessonId, Guid organizationId, UpdateLessonRequest request, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(Guid seriesId, Guid lessonId, Guid organizationId, bool wholeSlot = false, CancellationToken ct = default);
    Task<Result<Guid>> GetClubIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
