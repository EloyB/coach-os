using CoachOS.Application.AccountEmail;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Npgsql;

namespace CoachOS.Infrastructure.Identity;

public class AccountEmailIdentity(
    UserManager<ApplicationUser> users, IUnitOfWork unitOfWork,
    IStringLocalizer<AccountEmailResources> text) : IAccountEmailIdentity
{
    public async Task<Result<EmailChangeChallenge>> BeginAsync(Guid userId, string newEmail, string currentPassword, CancellationToken ct = default)
    {
        ApplicationUser? user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, currentPassword))
            return Result<EmailChangeChallenge>.Fail(Validation("PasswordInvalid"));
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase) || await users.FindByEmailAsync(newEmail) is not null)
            return Result<EmailChangeChallenge>.Fail(Validation("EmailUnavailable"));
        // Persisted stamp rotation invalidates older requests, including requests for other
        // addresses. JWT validation does NOT consult this stamp: this is not JWT revocation.
        IdentityResult rotated = await users.UpdateSecurityStampAsync(user);
        if (!rotated.Succeeded)
            return Result<EmailChangeChallenge>.Fail(Validation("Retry"));
        string token = await users.GenerateChangeEmailTokenAsync(user, newEmail);
        return Result<EmailChangeChallenge>.Ok(new(user.FirstName, token));
    }

    public async Task<Result> ConfirmAsync(Guid userId, string newEmail, string token, CancellationToken ct = default)
    {
        ApplicationUser? user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return Result.Fail(Validation("LinkInvalid"));
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // ChangeEmail rotates the stamp and the EF Identity store conditions its UPDATE
            // on ConcurrencyStamp. A parallel confirmation/resend can therefore win only once.
            IdentityResult changed = await users.ChangeEmailAsync(user, newEmail, token);
            if (!changed.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return Result.Fail(Validation("LinkInvalid"));
            }
            // NormalizedUserName has a UNIQUE index; unlike the non-unique EmailIndex this
            // is the final arbiter against registrations/invitations racing our email check.
            IdentityResult renamed = await users.SetUserNameAsync(user, newEmail);
            if (!renamed.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return Result.Fail(Validation("EmailUnavailable"));
            }
            await unitOfWork.CommitTransactionAsync(ct);
            return Result.Ok();
        }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException ||
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return Result.Fail(Validation("EmailUnavailable"));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private Error Validation(string key) => new(ErrorCodes.Validation, text[key]);
}
