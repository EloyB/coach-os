using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.StandaloneLessons;

/// <summary>
/// Trainer/admin-flow voor losse lessen: aanmaken + uitnodigen, lijsten,
/// detail, annuleren, extra uitnodigingen toevoegen, herzenden.
/// Alle methodes filteren op <c>OrganizationId</c>.
/// </summary>
public interface IStandaloneLessonService
{
    Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateStandaloneLessonRequest request, CancellationToken ct = default);

    Task<Result<List<StandaloneLessonListItemDto>>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default);

    Task<Result<StandaloneLessonDetailDto>> GetByIdAsync(
        Guid organizationId, Guid lessonId, CancellationToken ct = default);

    Task<Result> CancelAsync(
        Guid organizationId, Guid lessonId, string? reason, CancellationToken ct = default);

    Task<Result> AddInvitationsAsync(
        Guid organizationId, Guid lessonId, List<string> emails, CancellationToken ct = default);

    Task<Result> ResendInvitationAsync(
        Guid organizationId, Guid lessonId, Guid invitationId, CancellationToken ct = default);
}
