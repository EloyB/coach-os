using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Identity;

/// <summary>
/// Promoveert bij startup de geconfigureerde e-mail (Scaleway secret <c>SuperAdmin__Email</c>)
/// tot system-level super admin. Idempotent: doet niets als de user al de flag heeft of
/// als er nog geen user met die e-mail bestaat (in dat geval logt het een waarschuwing).
///
/// De waarde wordt handmatig in de Scaleway Secret Manager gezet — Terraform definieert
/// enkel het bestaan van de secret, niet de inhoud (zie infra/).
/// </summary>
public class SuperAdminBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SuperAdminBootstrapHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuredEmail = configuration["SuperAdmin__Email"] ?? configuration["SuperAdmin:Email"];
        if (string.IsNullOrWhiteSpace(configuredEmail))
        {
            logger.LogInformation("Geen SuperAdmin__Email geconfigureerd — bootstrap overgeslagen.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var normalizedEmail = configuredEmail.Trim().ToLowerInvariant();
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpperInvariant(), cancellationToken);

        if (user is null)
        {
            logger.LogWarning(
                "SuperAdmin bootstrap: geen user gevonden met e-mail {Email}. " +
                "Registreer eerst manueel en herstart, of laat het bestaande seed-script de user aanmaken.",
                normalizedEmail);
            return;
        }

        if (user.IsSuperAdmin)
        {
            logger.LogInformation("SuperAdmin bootstrap: {Email} is al super admin — geen actie nodig.", normalizedEmail);
            return;
        }

        user.IsSuperAdmin = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError(
                "SuperAdmin bootstrap mislukt voor {Email}: {Errors}",
                normalizedEmail,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogWarning(
            "SuperAdmin bootstrap: {Email} (UserId: {UserId}) is gepromoveerd tot super admin.",
            normalizedEmail, user.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
