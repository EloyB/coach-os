using CoachOS.Domain.Models;

namespace CoachOS.Application.AccountEmail;

public record EmailChangeChallenge(string FirstName, string Token);

/// <summary>Identity boundary: never changes another user's account or exposes credentials.</summary>
public interface IAccountEmailIdentity
{
    Task<Result<EmailChangeChallenge>> BeginAsync(Guid userId, string newEmail, string currentPassword, CancellationToken ct = default);
    Task<Result> ConfirmAsync(Guid userId, string newEmail, string token, CancellationToken ct = default);
}
