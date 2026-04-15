using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Configuration;
using CoachOS.Application.StudentAuth;
using CoachOS.Application.StudentAuth.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Identity;

public class StudentMagicLinkService(
    IMagicLinkTokenRepository repo,
    IEmailService emailService,
    TokenService tokenService,
    IOptions<AppOptions> appOptions,
    ILogger<StudentMagicLinkService> logger)
    : IStudentMagicLinkService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result> RequestAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        // Generate 32 random bytes, URL-safe base64.
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncode(rawBytes);
        var tokenHash = Sha256Hex(rawToken);

        MagicLinkToken entity = new()
        {
            Id = Guid.NewGuid(),
            Email = normalized,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime)
        };

        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);

        var url = $"{_app.StudentMagicLinkBaseUrl.TrimEnd('/')}?token={rawToken}";

        try
        {
            await emailService.SendStudentMagicLinkAsync(normalized, url, ct);
            logger.LogInformation("Magic link verstuurd naar {Email}", MaskEmail(normalized));
        }
        catch (Exception ex)
        {
            // Still return success to the caller to avoid leaking email deliverability.
            logger.LogError(ex, "Versturen magic link mislukt voor {Email}", MaskEmail(normalized));
        }

        return Result.Ok();
    }

    public async Task<Result<StudentAuthResponseDto>> RedeemAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenHash = Sha256Hex(rawToken);

        // Atomisch consumeren voorkomt dat twee parallelle requests dezelfde token
        // beide accepteren (race-condition tussen check en mark-used).
        var entity = await repo.TryConsumeAsync(tokenHash, DateTime.UtcNow, ct);
        if (entity is null)
        {
            logger.LogWarning("Magic-link redeem: onbekende, verlopen of al gebruikte token");
            return Result<StudentAuthResponseDto>.Fail("Ongeldige of verlopen link");
        }

        (var jwt, var expiresAt) = tokenService.GenerateStudentToken(entity.Email);

        logger.LogInformation("Succesvolle student-login via magic link: {Email}", entity.Email);

        return Result<StudentAuthResponseDto>.Ok(new StudentAuthResponseDto
        {
            Token = jwt,
            ExpiresAt = expiresAt,
            Email = entity.Email,
            Role = "Student"
        });
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        StringBuilder sb = new(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
