using System.Security.Cryptography;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class TrainerService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    IEmailService emailService,
    ApplicationDbContext context)
    : ITrainerService
{
    public async Task<Result<Guid>> InviteAsync(
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string inviteBaseUrl,
        CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);

        // Case 1: bestaande user — check alleen membership in deze org (multi-tenant!).
        if (existing is not null)
        {
            var existingMembership = await context.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.UserId == existing.Id && m.OrganizationId == organizationId, ct);

            if (existingMembership is { IsActive: true })
                return Result<Guid>.Fail("Trainer is al lid van deze organisatie");

            // User bestaat globaal en is actief elders → voeg direct membership toe,
            // geen accept-flow nodig. User logt in met bestaand wachtwoord en switcht.
            if (existing.IsActive)
            {
                if (existingMembership is null)
                {
                    context.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        Id = Guid.NewGuid(),
                        UserId = existing.Id,
                        OrganizationId = organizationId,
                        Role = UserRole.Trainer,
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingMembership.IsActive = true;
                    existingMembership.Role = UserRole.Trainer;
                }
                await context.SaveChangesAsync(ct);

                // Informatieve mail (geen accept-link nodig).
                await emailService.SendTrainerInviteAsync(email, firstName, $"{inviteBaseUrl.TrimEnd('/')}/login", ct);
                return Result<Guid>.Ok(existing.Id);
            }

            // User bestaat maar is inactief (openstaande invite elders) — dat pad
            // houden we strikt om accounts-takeover te voorkomen.
            return Result<Guid>.Fail("Er bestaat al een openstaande uitnodiging voor dit e-mailadres");
        }

        // Case 2: volledig nieuwe user — klassieke invite+accept flow.
        var inviteToken = Guid.NewGuid().ToString("N");
        var hashedToken = HashToken(inviteToken);

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = false,
            InviteToken = hashedToken,
            InviteTokenExpiry = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "!A1";
        var createResult = await userManager.CreateAsync(user, randomPassword);
        if (!createResult.Succeeded)
            return Result<Guid>.Fail(createResult.Errors.Select(e => e.Description));

        context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = organizationId,
            Role = UserRole.Trainer,
            IsActive = false, // wordt actief bij AcceptInvite
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);

        var inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{inviteToken}";
        await emailService.SendTrainerInviteAsync(email, firstName, inviteUrl, ct);

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default)
    {
        var hashedToken = HashToken(token);
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == hashedToken, ct);

        if (user is null)
            return Result<AuthResponseDto>.Fail("Ongeldige uitnodigingslink");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponseDto>.Fail("Uitnodigingslink is verlopen");

        // Hele accept-flow in één transactie: password reset, user activation
        // en membership activation moeten atomair gebeuren. Zonder transactie
        // kon een fout halverwege een account met nieuw wachtwoord maar
        // inactieve membership achterlaten.
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, password);
        if (!result.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));
        }

        user.IsActive = true;
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return Result<AuthResponseDto>.Fail(updateResult.Errors.Select(e => e.Description));
        }

        // Activeer het bijbehorende (pending) membership. Een nieuwe user heeft
        // bij acceptatie per definitie exact één pending membership — we checken
        // dit expliciet om te voorkomen dat we bij toekomstige flow-uitbreidingen
        // per ongeluk de verkeerde membership activeren.
        var pendings = await context.OrganizationMemberships
            .Include(m => m.Organization)
            .Where(m => m.UserId == user.Id && !m.IsActive)
            .ToListAsync(ct);

        if (pendings.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return Result<AuthResponseDto>.Fail("Geen openstaande uitnodiging gevonden");
        }

        if (pendings.Count > 1)
        {
            await tx.RollbackAsync(ct);
            return Result<AuthResponseDto>.Fail("Meerdere openstaande uitnodigingen gevonden — neem contact op met support");
        }

        var pending = pendings[0];

        pending.IsActive = true;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        (var jwtToken, var expiresAt) = tokenService.GenerateToken(user, pending);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = jwtToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = pending.OrganizationId,
            Role = pending.Role.ToString(),
            Memberships =
            [
                new OrganizationMembershipDto
                {
                    OrganizationId = pending.OrganizationId,
                    OrganizationName = pending.Organization?.Name ?? string.Empty,
                    Role = pending.Role.ToString(),
                    IsActive = true
                }
            ]
        });
    }

    public async Task<Result<List<TrainerDto>>> GetTrainersAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        // Trainers zijn alle memberships (Trainer-rol) in deze org, via join met users.
        // Pending invites (m.IsActive == false, u.InviteToken != null) blijven meekomen
        // zodat de admin uitstaande uitnodigingen ziet en kan opnieuw versturen.
        var query =
            from m in context.OrganizationMemberships.AsNoTracking()
            join u in context.Users.AsNoTracking() on m.UserId equals u.Id
            where m.OrganizationId == organizationId
                  && m.Role == UserRole.Trainer
            orderby u.FirstName, u.LastName
            select new TrainerDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                IsActive = u.IsActive,
                InvitePending = u.InviteToken != null,
                CreatedAt = u.CreatedAt,
                LessonSeriesCount = context.LessonSeries
                    .Count(ls => ls.Lessons.Any(l => l.TrainerId == u.Id) && ls.OrganizationId == organizationId && ls.IsActive),
                WeeklyCapacityHours = 16,
            };

        List<TrainerDto> trainers = await query.ToListAsync(ct);

        // Calculate current week hours booked in-memory (TimeOnly arithmetic not supported in EF/Npgsql)
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int dayOffset = ((int)today.DayOfWeek + 6) % 7; // Monday=0
        DateOnly weekStart = today.AddDays(-dayOffset);
        DateOnly weekEnd = weekStart.AddDays(6);

        List<Guid> trainerIds = trainers.Select(t => t.Id).ToList();

        var weekLessons = await context.Lessons
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId
                && l.TrainerId.HasValue
                && trainerIds.Contains(l.TrainerId.Value)
                && l.Date >= weekStart && l.Date <= weekEnd
                && !l.IsCancelled)
            .Select(l => new { l.TrainerId, l.StartTime, l.EndTime })
            .ToListAsync(ct);

        Dictionary<Guid, decimal> hoursMap = weekLessons
            .GroupBy(l => l.TrainerId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(l => (decimal)(l.EndTime - l.StartTime).TotalHours));

        foreach (TrainerDto trainer in trainers)
        {
            trainer.CurrentWeekHoursBooked = hoursMap.GetValueOrDefault(trainer.Id, 0m);
        }

        return Result<List<TrainerDto>>.Ok(trainers);
    }

    public async Task<Result> ResendInviteAsync(
        Guid trainerId,
        Guid organizationId,
        string inviteBaseUrl,
        CancellationToken ct = default)
    {
        // Verify membership exists in this org and is a trainer.
        OrganizationMembership? membership = await context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == trainerId
                && m.OrganizationId == organizationId
                && m.Role == UserRole.Trainer, ct);

        if (membership is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden in deze organisatie."));

        ApplicationUser? user = await userManager.FindByIdAsync(trainerId.ToString());
        if (user is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Gebruiker niet gevonden."));

        // Only allow resend when there is a pending invite (user not yet active, token present).
        if (user.IsActive || user.InviteToken is null)
            return Result.Fail(new Error(ErrorCodes.Validation, "Er is geen openstaande uitnodiging voor deze trainer."));

        // Rate limit: block resend if the current token was issued less than 5 minutes ago.
        // InviteTokenExpiry = issued + 72h, so issued = expiry - 72h.
        if (user.InviteTokenExpiry.HasValue)
        {
            DateTime lastInviteSent = user.InviteTokenExpiry.Value.AddHours(-72);
            if (DateTime.UtcNow - lastInviteSent < TimeSpan.FromMinutes(5))
                return Result.Fail(new Error(ErrorCodes.Validation, "Wacht minstens 5 minuten voordat je opnieuw een uitnodiging verstuurt."));
        }

        // Generate a fresh invite token.
        string newInviteToken = Guid.NewGuid().ToString("N");
        string hashedToken = HashToken(newInviteToken);

        user.InviteToken = hashedToken;
        user.InviteTokenExpiry = DateTime.UtcNow.AddHours(72);
        user.UpdatedAt = DateTime.UtcNow;

        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Fail(updateResult.Errors.Select(e => e.Description));

        string inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{newInviteToken}";
        await emailService.SendTrainerInviteAsync(user.Email!, user.FirstName, inviteUrl, ct);

        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        // Deactivatie = membership intrekken. De user-account blijft globaal bestaan;
        // ze kunnen in andere orgs blijven werken.
        var membership = await context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == trainerId && m.OrganizationId == organizationId, ct);

        if (membership is null || membership.Role != UserRole.Trainer)
            return Result.Fail("Trainer niet gevonden");

        membership.IsActive = false;
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        // Draai de hele remove-operatie in één transactie: anders kan tussen
        // "check resterende memberships" en "delete user" een parallelle invite
        // een nieuw membership aanmaken — en we verwijderen alsnog de user.
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var membership = await context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == trainerId && m.OrganizationId == organizationId, ct);

        if (membership is null || membership.Role != UserRole.Trainer)
            return Result.Fail("Trainer niet gevonden");

        var count = await context.Lessons
            .CountAsync(l => l.TrainerId == trainerId && l.OrganizationId == organizationId, ct);

        if (count > 0)
            return Result.Fail($"Trainer heeft {count} les(sen). Wijs deze eerst toe aan een andere trainer.");

        context.OrganizationMemberships.Remove(membership);
        await context.SaveChangesAsync(ct);

        // Alleen het user-account verwijderen als er geen andere memberships meer zijn.
        var remainingMemberships = await context.OrganizationMemberships
            .AnyAsync(m => m.UserId == trainerId, ct);
        if (!remainingMemberships)
        {
            var user = await userManager.FindByIdAsync(trainerId.ToString());
            if (user is not null)
            {
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    return Result.Fail(result.Errors.Select(e => e.Description));
                }
            }
        }

        await tx.CommitAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ReassignSeriesAsync(
        Guid fromTrainerId,
        Guid toTrainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var toMembership = await context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == toTrainerId
                && m.OrganizationId == organizationId
                && m.Role == UserRole.Trainer
                && m.IsActive, ct);

        if (toMembership is null)
            return Result.Fail("Doeltrainer niet gevonden of niet actief");

        await context.Lessons
            .Where(l => l.TrainerId == fromTrainerId && l.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.TrainerId, toTrainerId), ct);

        return Result.Ok();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}
