using CoachOS.Application.Abstractions;
using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Application.Auth;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.AccountEmail;

public class AccountEmailService(
    IAccountEmailIdentity identity, IEmailService email, IAuthService auth, IOptions<AppOptions> options)
: IAccountEmailService
{
    public async Task<Result<CoachOS.Application.Auth.DTOs.AuthResponseDto>> ConfirmAsync(
        Guid userId, Guid organizationId, ConfirmAccountEmailChange request, CancellationToken ct = default)
    {
        string newEmail = request.NewEmail.Trim();
        Result<EmailChanged> changed = await identity.ConfirmAsync(userId, newEmail, request.Token, ct);
        if (!changed.IsSuccess) return Result<CoachOS.Application.Auth.DTOs.AuthResponseDto>.Fail(changed.Errors);
        // The previous owner must learn about a takeover: password reset now goes to the new address.
        await email.SendAccountEmailChangedNoticeAsync(changed.Value!.PreviousEmail, changed.Value.FirstName, newEmail, ct);
        return await auth.SwitchOrganizationAsync(userId, organizationId, ct);
    }

    public async Task<Result> RequestAsync(Guid userId, RequestAccountEmailChange request, CancellationToken ct = default)
    {
        string newEmail = request.NewEmail.Trim();
        Result<EmailChangeChallenge> challenge = await identity.BeginAsync(userId, newEmail, request.CurrentPassword, ct);
        if (!challenge.IsSuccess) return Result.Fail(challenge.Errors);
        string url = $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/dashboard/account#email={Uri.EscapeDataString(newEmail)}&token={Uri.EscapeDataString(challenge.Value!.Token)}";
        await email.SendAccountEmailChangeAsync(newEmail, challenge.Value.FirstName, url, ct);
        return Result.Ok();
    }
}
