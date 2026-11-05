using System.Security.Cryptography;
using System.Text;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using FluentValidation;

namespace CoachOS.Application.WebsiteWaitlist;

public record RegisterWebsiteWaitlistRequest(string Email, string? Role = null);
public record UnsubscribeWebsiteWaitlistRequest(string Token);
public record ManagedWebsiteWaitlistMailRequest(bool Apply = false);
public record ManagedWebsiteWaitlistMailResult(bool Eligible, bool Queued);

public sealed class UnsubscribeWebsiteWaitlistValidator : AbstractValidator<UnsubscribeWebsiteWaitlistRequest>
{
    public UnsubscribeWebsiteWaitlistValidator() => RuleFor(x => x.Token).NotEmpty().Matches("\\A[a-f0-9]{64}\\z");
}

public sealed class RegisterWebsiteWaitlistValidator : AbstractValidator<RegisterWebsiteWaitlistRequest>
{
    public RegisterWebsiteWaitlistValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.Role).Must(x => x is null or "club" or "coach" or "anders");
    }
}

public interface IWebsiteWaitlistService
{
    Task<bool> RegisterAsync(RegisterWebsiteWaitlistRequest request, CancellationToken ct);
    Task UnsubscribeAsync(UnsubscribeWebsiteWaitlistRequest request, CancellationToken ct);
    Task<ManagedWebsiteWaitlistMailResult> QueueManagedMailAsync(Guid entryId, bool apply, CancellationToken ct);
}

public sealed class WebsiteWaitlistService(IWebsiteWaitlistRepository repository, IUnitOfWork unitOfWork, TimeProvider clock)
    : IWebsiteWaitlistService
{
    // Approved launch 2026-11-15, six calendar months. No new required configuration.
    public static readonly DateTime RetentionDeadline = new(2027, 5, 15, 0, 0, 0, DateTimeKind.Utc);

    public async Task<bool> RegisterAsync(RegisterWebsiteWaitlistRequest request, CancellationToken ct)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        if (now >= RetentionDeadline) return false;
        string email = request.Email.Trim().ToLowerInvariant();
        if (await repository.ExistsAsync(email, ct)) return true;
        string token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        WebsiteWaitlistEntry entry = new()
        {
            Id = Guid.NewGuid(), Email = email, Role = request.Role, UnsubscribeTokenHash = Hash(token), CreatedAt = now,
        };
        WebsiteWaitlistMail mail = new()
        {
            EntryId = entry.Id, Token = token, CreatedAt = now, AvailableAt = now,
            ExpiresAt = new[] { now.AddDays(7), RetentionDeadline }.Min(),
        };
        await repository.AddAsync(entry, mail, ct);
        // One EF transaction saves entry + mail together. A racing duplicate is a
        // unique-key no-op; neither response exposes a capability or sends again.
        await unitOfWork.TrySaveChangesAsync(ct);
        return true;
    }

    public async Task UnsubscribeAsync(UnsubscribeWebsiteWaitlistRequest request, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await repository.RemoveByHashAsync(Hash(request.Token), ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<ManagedWebsiteWaitlistMailResult> QueueManagedMailAsync(Guid entryId, bool apply, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            WebsiteWaitlistEntry? entry = await repository.FindForMailAsync(entryId, ct);
            WebsiteWaitlistMail? mail = await repository.FindMailAsync(entryId, ct);
            bool eligible = now < RetentionDeadline && entry is not null && mail is null;
            if (eligible && apply)
            {
                string token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
                entry!.UnsubscribeTokenHash = Hash(token);
                await repository.AddMailAsync(new WebsiteWaitlistMail
                {
                    EntryId = entryId, Token = token, AvailableAt = now, CreatedAt = now,
                    ExpiresAt = new[] { now.AddDays(7), RetentionDeadline }.Min(),
                }, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
            await unitOfWork.CommitTransactionAsync(ct);
            return new(eligible, eligible && apply);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
