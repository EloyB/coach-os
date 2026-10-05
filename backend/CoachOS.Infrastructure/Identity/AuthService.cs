using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    TokenService tokenService,
    IEmailService emailService,
    IOptions<RegistrationOptions> registrationOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        string organizationName = request.OrganizationName.Trim();
        string firstName = request.FirstName.Trim();
        string lastName = request.LastName.Trim();
        string email = request.Email.Trim();
        RegistrationOptions options = registrationOptions.Value;

        // Bij registratie van een nieuwe organisatie mag dezelfde email niet
        // opnieuw een nieuwe org aanmaken — dan groeien de tenants ongecontroleerd.
        // Wil je als bestaande user een nieuwe org starten, dan gebeurt dat via
        // een aparte "create organization" flow (post-MVP).
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogWarning("Registratie geweigerd: e-mail {Email} is al in gebruik", MaskEmail(email));
            return Result<AuthResponseDto>.Fail("E-mailadres is al in gebruik");
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            Organization organization = new()
            {
                Id = Guid.NewGuid(),
                Name = organizationName,
                Email = email,
                IsActive = true,
                Country = "BE"
            };

            context.Organizations.Add(organization);

            // Initialiseer org-settings met defaults zodat de FE bij eerste GET geen lazy
            // provisioning hoeft te triggeren. AdminsActAsTrainers default true: een
            // tennisschool draait typisch op de admin-coach combinatie.
            // OnboardingStartedAt wordt *alleen hier* gezet — niet in OrganizationSettingsService.
            // GetOrCreateAsync — zodat pre-existing orgs NULL houden en de checklist niet zien.
            context.OrganizationSettings.Add(new Domain.Entities.OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AdminsActAsTrainers = true,
                OnboardingStartedAt = DateTime.UtcNow,
            });

            // Elke nieuwe organisatie start met een gratis proef — zonder plan of betaalgegevens.
            context.Subscriptions.Add(Subscription.StartTrial(organization.Id, timeProvider.GetBrusselsToday()));

            await context.SaveChangesAsync(cancellationToken);

            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmailConfirmed = options.AutoVerifyEmail
            };

            string? verificationCode = options.AutoVerifyEmail ? null : IssueVerificationCode(user, now);

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));
            }

            OrganizationMembership membership = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = UserRole.Admin,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            context.OrganizationMemberships.Add(membership);

            context.TermsAcceptances.Add(new TermsAcceptance
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                DocumentType = LegalDocumentType.TermsOfService,
                Version = options.TermsVersion,
                AcceptedAt = now
            });
            context.TermsAcceptances.Add(new TermsAcceptance
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                DocumentType = LegalDocumentType.DataProcessingAgreement,
                Version = options.DataProcessingAgreementVersion,
                AcceptedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Nieuwe registratie: {Email}, organisatie {OrgName} ({OrgId})",
                MaskEmail(email), organizationName, organization.Id);

            if (verificationCode is not null)
                await TrySendVerificationEmailAsync(user, verificationCode, cancellationToken);

            (var token, var expiresAt) = tokenService.GenerateToken(user, membership);

            return Result<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = organization.Id,
                Role = UserRole.Admin.ToString(),
                EmailVerified = user.EmailConfirmed,
                Memberships =
                [
                    new OrganizationMembershipDto
                    {
                        OrganizationId = organization.Id,
                        OrganizationName = organization.Name,
                        Role = UserRole.Admin.ToString(),
                        IsActive = true
                    }
                ]
            });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Registratie mislukt voor {Email}", email);
            return Result<AuthResponseDto>.Fail("Registratie mislukt. Probeer het opnieuw.");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("Mislukte inlogpoging: onbekend e-mailadres {Email}", MaskEmail(email));
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        var validPassword = await userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
        {
            logger.LogWarning("Mislukte inlogpoging: fout wachtwoord voor {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Inlogpoging op gedeactiveerd account: {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Account is gedeactiveerd");
        }

        var memberships = await LoadActiveMembershipsAsync(user.Id, cancellationToken);

        // Option C invariant: een super admin opereert los van orgs en mag GEEN
        // memberships hebben. Als die toch bestaan (data corruptie / handmatige
        // inserts) is de veilige fallback: weigeren tot het rechtgezet is.
        if (user.IsSuperAdmin && memberships.Count > 0)
        {
            logger.LogError(
                "Invariant geschonden: super-admin {Email} (UserId: {UserId}) heeft {Count} memberships. Login geweigerd.",
                MaskEmail(email), user.Id, memberships.Count);
            return Result<AuthResponseDto>.Fail("Accountconfiguratie ongeldig. Contacteer de beheerder.");
        }

        if (user.IsSuperAdmin)
        {
            logger.LogInformation("Succesvolle super-admin login: {Email} (UserId: {UserId})", email, user.Id);
            (var saToken, var saExpiresAt) = tokenService.GenerateSuperAdminToken(user);
            return Result<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = saToken,
                ExpiresAt = saExpiresAt,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = null,
                Role = "SuperAdmin",
                Memberships = [],
                IsSuperAdmin = true
            });
        }

        if (memberships.Count == 0)
        {
            logger.LogWarning("Login zonder actieve memberships: {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Geen actieve organisatie gekoppeld aan dit account");
        }

        // Standaard: eerste membership. Bij Admin-rol krijgt die voorrang zodat
        // admins niet elke keer expliciet moeten switchen.
        var active = memberships.FirstOrDefault(m => m.Role == UserRole.Admin) ?? memberships[0];

        logger.LogInformation("Succesvolle login: {Email} (UserId: {UserId}), actieve org {OrgId}",
            email, user.Id, active.OrganizationId);

        return BuildAuthResponse(user, active, memberships);
    }

    public async Task<Result<AuthResponseDto>> SwitchOrganizationAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Fail("Account niet gevonden");

        var memberships = await LoadActiveMembershipsAsync(userId, cancellationToken);
        var target = memberships.FirstOrDefault(m => m.OrganizationId == organizationId);
        if (target is null)
            return Result<AuthResponseDto>.Fail("Geen toegang tot deze organisatie");

        return BuildAuthResponse(user, target, memberships);
    }

    public async Task<Result<List<OrganizationMembershipDto>>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await LoadActiveMembershipsAsync(userId, cancellationToken);
        return Result<List<OrganizationMembershipDto>>.Ok(memberships.Select(ToDto).ToList());
    }

    public async Task<Result> ForgotPasswordAsync(
        string email,
        string resetBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Anti-enumeration: always return Ok, even when e-mail not found.
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Wachtwoord-reset aangevraagd voor onbekend/inactief e-mail {Email}", MaskEmail(email));
            return Result.Ok();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetUrl = $"{resetBaseUrl.TrimEnd('/')}/reset-password?token={encodedToken}&email={encodedEmail}";

        await emailService.SendPasswordResetAsync(email, user.FirstName, resetUrl, cancellationToken);

        logger.LogInformation("Wachtwoord-reset e-mail verstuurd naar {Email}", MaskEmail(email));
        return Result.Ok();
    }

    public async Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken cancellationToken = default)
    {
        var hashedToken = HashToken(token);
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.InviteToken == hashedToken, cancellationToken);

        if (user is null)
            return Result<AuthResponseDto>.Fail("Ongeldige uitnodigingslink");
        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponseDto>.Fail("Uitnodigingslink is verlopen");

        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
        if (!resetResult.Succeeded)
        {
            await tx.RollbackAsync(cancellationToken);
            return Result<AuthResponseDto>.Fail(resetResult.Errors.Select(e => e.Description));
        }

        user.IsActive = true;
        // De uitnodiging kwam via dit e-mailadres binnen — daarmee is het bevestigd.
        user.EmailConfirmed = true;
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await tx.RollbackAsync(cancellationToken);
            return Result<AuthResponseDto>.Fail(updateResult.Errors.Select(e => e.Description));
        }

        var pendings = await context.OrganizationMemberships
            .Include(m => m.Organization)
            .Include(m => m.HeadTrainerClubs)
            .Where(m => m.UserId == user.Id && !m.IsActive)
            .ToListAsync(cancellationToken);

        if (pendings.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return Result<AuthResponseDto>.Fail("Geen openstaande uitnodiging gevonden");
        }
        if (pendings.Count > 1)
        {
            await tx.RollbackAsync(cancellationToken);
            return Result<AuthResponseDto>.Fail("Meerdere openstaande uitnodigingen gevonden — neem contact op met support");
        }

        var pending = pendings[0];
        pending.IsActive = true;
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

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

    public async Task<Result<InviteValidationDto>> ValidateInviteTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<InviteValidationDto>.Fail("Ongeldige uitnodigingslink");

        var hashedToken = HashToken(token);
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.InviteToken == hashedToken, cancellationToken);

        if (user is null)
            return Result<InviteValidationDto>.Fail("Ongeldige uitnodigingslink");
        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<InviteValidationDto>.Fail("Uitnodigingslink is verlopen");

        return Result<InviteValidationDto>.Ok(new InviteValidationDto(user.FirstName));
    }

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return Result.Fail(new Error(ErrorCodes.Validation, "Ongeldige reset-link"));

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Wachtwoord-reset mislukt voor {Email}: {Errors}",
                MaskEmail(email), string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        logger.LogInformation("Wachtwoord succesvol gereset voor {Email}", MaskEmail(email));
        return Result.Ok();
    }

    public async Task<Result> VerifyEmailAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        // AsNoTracking: alle schrijfacties hieronder lopen via ExecuteUpdate, dus er mag geen
        // getrackte (en dan verouderde) kopie van de user in de context blijven hangen.
        ApplicationUser? user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Account niet gevonden"));

        if (user.EmailConfirmed)
            return Result.Ok();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (user.EmailVerificationCodeHash is null
            || user.EmailVerificationCodeExpiry is null
            || user.EmailVerificationCodeExpiry < now)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "De code is verlopen. Vraag een nieuwe code aan."));
        }

        // De poging wordt atomair in de database geteld vóór de code vergeleken wordt. Zo kunnen
        // gelijktijdige requests samen nooit meer dan MaxAttempts codes uitproberen.
        string currentHash = user.EmailVerificationCodeHash;
        int claimedAttempts = await userManager.Users
            .Where(u => u.Id == userId
                        && u.EmailVerificationCodeHash == currentHash
                        && u.EmailVerificationAttempts < EmailVerificationCodes.MaxAttempts)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.EmailVerificationAttempts, u => u.EmailVerificationAttempts + 1),
                cancellationToken);
        if (claimedAttempts == 0)
            return Result.Fail(new Error(ErrorCodes.Validation, "Te veel pogingen. Vraag een nieuwe code aan."));

        if (!EmailVerificationCodes.Matches(currentHash, user.Id, code))
        {
            logger.LogWarning("Foute verificatiecode voor {Email}", MaskEmail(user.Email!));
            return Result.Fail(new Error(ErrorCodes.Validation, "De code is niet juist."));
        }

        await userManager.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.EmailConfirmed, true)
                    .SetProperty(u => u.EmailVerificationCodeHash, (string?)null)
                    .SetProperty(u => u.EmailVerificationCodeExpiry, (DateTime?)null)
                    .SetProperty(u => u.EmailVerificationSentAt, (DateTime?)null)
                    .SetProperty(u => u.EmailVerificationAttempts, 0)
                    .SetProperty(u => u.UpdatedAt, now),
                cancellationToken);

        logger.LogInformation("E-mailadres bevestigd: {Email} (UserId: {UserId})", MaskEmail(user.Email!), user.Id);
        return Result.Ok();
    }

    public async Task<Result> ResendEmailVerificationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Account niet gevonden"));

        if (user.EmailConfirmed)
            return Result.Ok();

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (user.EmailVerificationSentAt is not null
            && now - user.EmailVerificationSentAt.Value < EmailVerificationCodes.ResendCooldown)
        {
            return Result.Fail(new Error(ErrorCodes.Validation, "Wacht even voor je een nieuwe code aanvraagt."));
        }

        if (user.EmailVerificationSendCount >= EmailVerificationCodes.MaxSends)
            return Result.Fail(new Error(ErrorCodes.Validation, "Je hebt te veel codes aangevraagd. Neem contact op via info@coach-os.be."));

        string code = IssueVerificationCode(user, now);
        user.UpdatedAt = now;
        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Fail(updateResult.Errors.Select(e => e.Description));

        try
        {
            await emailService.SendEmailVerificationAsync(user.Email!, user.FirstName, code, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Verificatiemail versturen mislukt voor {Email}", MaskEmail(user.Email!));
            return Result.Fail(new Error(ErrorCodes.ExternalService, "De mail kon niet verstuurd worden. Probeer het later opnieuw."));
        }

        return Result.Ok();
    }

    public async Task<bool> IsEmailVerifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await userManager.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.EmailConfirmed, cancellationToken);
    }

    /// <summary>Zet een verse verificatiecode op de user (nog niet opgeslagen) en geeft de leesbare code terug.</summary>
    private static string IssueVerificationCode(ApplicationUser user, DateTime now)
    {
        string code = EmailVerificationCodes.Generate();
        user.EmailVerificationCodeHash = EmailVerificationCodes.Hash(user.Id, code);
        user.EmailVerificationCodeExpiry = now.Add(EmailVerificationCodes.Lifetime);
        user.EmailVerificationSentAt = now;
        user.EmailVerificationAttempts = 0;
        user.EmailVerificationSendCount++;
        return code;
    }

    /// <summary>
    /// Een mislukte mail mag de registratie niet terugdraaien: het account bestaat al en de
    /// gebruiker kan vanop het verificatiescherm een nieuwe code aanvragen.
    /// </summary>
    private async Task TrySendVerificationEmailAsync(ApplicationUser user, string code, CancellationToken ct)
    {
        try
        {
            await emailService.SendEmailVerificationAsync(user.Email!, user.FirstName, code, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Verificatiemail versturen mislukt voor {Email}", MaskEmail(user.Email!));
        }
    }

    private async Task<List<OrganizationMembership>> LoadActiveMembershipsAsync(Guid userId, CancellationToken ct)
    {
        return await context.OrganizationMemberships
            .AsNoTracking()
            .Include(m => m.Organization)
            .Include(m => m.HeadTrainerClubs)
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.Organization.Name)
            .ToListAsync(ct);
    }

    private Result<AuthResponseDto> BuildAuthResponse(
        ApplicationUser user,
        OrganizationMembership active,
        IReadOnlyList<OrganizationMembership> all)
    {
        (var token, var expiresAt) = tokenService.GenerateToken(user, active);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = active.OrganizationId,
            Role = active.Role.ToString(),
            EmailVerified = user.EmailConfirmed,
            HeadTrainerClubIds = active.HeadTrainerClubs.Select(h => h.TennisClubId).ToList(),
            Memberships = all.Select(ToDto).ToList()
        });
    }

    private static OrganizationMembershipDto ToDto(OrganizationMembership m) => new()
    {
        OrganizationId = m.OrganizationId,
        OrganizationName = m.Organization?.Name ?? string.Empty,
        Role = m.Role.ToString(),
        IsActive = m.IsActive
    };

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
