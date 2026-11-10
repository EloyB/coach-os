using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.WebsiteWaitlist;

public sealed class WebsiteWaitlistService(IWebsiteWaitlistRepository repository, IUnitOfWork unitOfWork, TimeProvider clock)
    : IWebsiteWaitlistService
{
    // Goedgekeurde lancering 2026-11-15, zes kalendermaanden.
    public static readonly DateTime RetentionDeadline = new(2027, 5, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MailPayloadLifetime = TimeSpan.FromDays(7);

    public async Task<Result> RegisterAsync(RegisterWebsiteWaitlistRequest request, CancellationToken ct = default)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        if (now >= RetentionDeadline) return Result.Fail(WebsiteWaitlistErrors.Closed);

        string email = request.Email.Trim().ToLowerInvariant();
        if (await repository.ExistsAsync(email, ct)) return Result.Ok();

        string token = NewToken();
        WebsiteWaitlistEntry entry = new()
        {
            Id = Guid.NewGuid(), Email = email, Role = request.Role, UnsubscribeTokenHash = Hash(token), CreatedAt = now,
        };
        await repository.AddAsync(entry, NewMail(entry.Id, token, now), ct);
        // Entry + mail in een SaveChanges. Een gelijktijdige dubbele inschrijving botst op de
        // unieke index en is een no-op: geen van beide antwoorden lekt of verstuurt iets extra.
        await unitOfWork.TrySaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UnsubscribeAsync(UnsubscribeWebsiteWaitlistRequest request, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await repository.RemoveByHashAsync(Hash(request.Token), ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result.Ok();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<Result<ManagedWebsiteWaitlistMailResult>> QueueManagedMailAsync(
        Guid entryId, bool apply, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            WebsiteWaitlistEntry? entry = await repository.FindForMailAsync(entryId, ct);
            WebsiteWaitlistMail? pending = await repository.FindMailAsync(entryId, ct);
            bool eligible = now < RetentionDeadline && entry is not null && pending is null;
            if (eligible && apply)
            {
                string token = NewToken();
                entry!.UnsubscribeTokenHash = Hash(token);
                await repository.AddMailAsync(NewMail(entryId, token, now), ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
            await unitOfWork.CommitTransactionAsync(ct);
            return Result<ManagedWebsiteWaitlistMailResult>.Ok(new ManagedWebsiteWaitlistMailResult(eligible, eligible && apply));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    /// <summary>SHA-256 (hex, lowercase) van het ruwe token; het importscript gebruikt dezelfde vorm.</summary>
    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string NewToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    private static WebsiteWaitlistMail NewMail(Guid entryId, string token, DateTime now)
    {
        DateTime expiresAt = now.Add(MailPayloadLifetime);
        return new WebsiteWaitlistMail
        {
            EntryId = entryId,
            Token = token,
            CreatedAt = now,
            AvailableAt = now,
            ExpiresAt = expiresAt < RetentionDeadline ? expiresAt : RetentionDeadline,
        };
    }
}
