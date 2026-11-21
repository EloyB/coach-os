using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

public interface IActiveStudentCountService
{
    Task<Result<ActiveStudentsDto>> GetActiveStudentsAsync(Guid organizationId, CancellationToken ct = default);
}
