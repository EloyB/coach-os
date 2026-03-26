using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
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
        ApplicationUser? existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result<Guid>.Fail("E-mailadres is al in gebruik");

        string inviteToken = Guid.NewGuid().ToString("N");

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            OrganizationId = organizationId,
            Role = UserRole.Trainer,
            IsActive = false,
            InviteToken = inviteToken,
            InviteTokenExpiry = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Use a placeholder password — will be replaced on invite acceptance
        IdentityResult result = await userManager.CreateAsync(user, "Placeholder@1!");
        if (!result.Succeeded)
            return Result<Guid>.Fail(result.Errors.Select(e => e.Description));

        string inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{inviteToken}";
        await emailService.SendTrainerInviteAsync(email, firstName, inviteUrl, ct);

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default)
    {
        ApplicationUser? user = await userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == token, ct);

        if (user is null)
            return Result<AuthResponseDto>.Fail("Ongeldige uitnodigingslink");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponseDto>.Fail("Uitnodigingslink is verlopen");

        string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult result = await userManager.ResetPasswordAsync(user, resetToken, password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));

        user.IsActive = true;
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        (string jwtToken, DateTime expiresAt) = tokenService.GenerateToken(user);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = jwtToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = user.OrganizationId,
            Role = user.Role.ToString()
        });
    }

    public async Task<Result<List<TrainerDto>>> GetTrainersAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        Dictionary<Guid, int> counts = await context.LessonSeries
            .Where(ls => ls.OrganizationId == organizationId)
            .GroupBy(ls => ls.TrainerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        List<TrainerDto> trainers = await userManager.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId && u.Role == UserRole.Trainer)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new TrainerDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                IsActive = u.IsActive,
                InvitePending = u.InviteToken != null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        foreach (TrainerDto trainer in trainers)
        {
            trainer.LessonSeriesCount = counts.GetValueOrDefault(trainer.Id, 0);
        }

        return Result<List<TrainerDto>>.Ok(trainers);
    }

    public async Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        ApplicationUser? trainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == trainerId && u.OrganizationId == organizationId, ct);

        if (trainer is null)
            return Result.Fail("Trainer niet gevonden");

        if (trainer.Role != UserRole.Trainer)
            return Result.Fail("Gebruiker is geen trainer");

        trainer.IsActive = false;
        trainer.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(trainer);

        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        ApplicationUser? trainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == trainerId && u.OrganizationId == organizationId, ct);

        if (trainer is null)
            return Result.Fail("Trainer niet gevonden");

        if (trainer.Role != UserRole.Trainer)
            return Result.Fail("Gebruiker is geen trainer");

        int count = await context.LessonSeries
            .CountAsync(ls => ls.TrainerId == trainerId && ls.OrganizationId == organizationId, ct);

        if (count > 0)
            return Result.Fail($"Trainer heeft {count} lesreeks(en). Wijs deze eerst toe aan een andere trainer.");

        IdentityResult result = await userManager.DeleteAsync(trainer);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => e.Description));
    }

    public async Task<Result> ReassignSeriesAsync(
        Guid fromTrainerId,
        Guid toTrainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        ApplicationUser? toTrainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == toTrainerId && u.OrganizationId == organizationId, ct);

        if (toTrainer is null)
            return Result.Fail("Doeltrainer niet gevonden");

        if (toTrainer.Role != UserRole.Trainer)
            return Result.Fail("Doelgebruiker is geen trainer");

        if (!toTrainer.IsActive)
            return Result.Fail("Doeltrainer is niet actief");

        await context.LessonSeries
            .Where(ls => ls.TrainerId == fromTrainerId && ls.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(ls => ls.TrainerId, toTrainerId), ct);

        return Result.Ok();
    }
}
