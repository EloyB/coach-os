using System.Security.Cryptography;
using System.Web;
using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.MollieConnect;

public class MollieConnectService(
    IMollieClient mollieClient,
    IMollieConnectionRepository connections,
    IOAuthStateRepository states,
    ITokenProtector protector,
    IOptions<MollieOptions> options,
    TimeProvider timeProvider,
    ILogger<MollieConnectService> logger) : IMollieConnectService
{
    private const int StateTtlMinutes = 15;
    private const int StateByteLength = 32;

    private readonly MollieOptions _options = options.Value;

    public async Task<Result<StartConnectResponse>> StartAsync(
        Guid organizationId,
        string redirectUri,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            logger.LogError("Mollie ClientId is niet geconfigureerd; OAuth start onmogelijk.");
            return Result<StartConnectResponse>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Mollie-integratie is niet geconfigureerd op deze omgeving."));
        }

        string state = GenerateSecureToken();
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        OAuthState entity = new()
        {
            OrganizationId = organizationId,
            State = state,
            ExpiresAt = utcNow.AddMinutes(StateTtlMinutes),
        };
        await states.AddAsync(entity, ct);
        await states.SaveChangesAsync(ct);

        string url = BuildAuthorizationUrl(state, redirectUri);
        return Result<StartConnectResponse>.Ok(new StartConnectResponse(url));
    }

    public async Task<Result<Guid>> HandleCallbackAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Validation,
                "Onvolledige OAuth-callback van Mollie."));
        }

        OAuthState? stored = await states.GetByStateAsync(state, ct);
        if (stored is null)
        {
            logger.LogWarning("OAuth callback met onbekende state ontvangen.");
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Unauthorized,
                "Ongeldige of verlopen OAuth-state."));
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (stored.ExpiresAt < utcNow)
        {
            await states.DeleteAsync(stored, ct);
            await states.SaveChangesAsync(ct);
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Unauthorized,
                "Deze koppelingspoging is verlopen, probeer opnieuw."));
        }

        Result<MollieTokenResponse> tokenResult = await mollieClient.ExchangeCodeForTokenAsync(code, redirectUri, ct);
        if (!tokenResult.IsSuccess)
        {
            // State opbruiken zodat dezelfde code niet opnieuw kan worden geprobeerd.
            await states.DeleteAsync(stored, ct);
            await states.SaveChangesAsync(ct);
            return Result<Guid>.Fail(tokenResult.Errors);
        }

        MollieTokenResponse tokens = tokenResult.Value!;
        Result<MollieOrganizationInfo> orgResult = await mollieClient.GetOrganizationAsync(tokens.AccessToken, ct);
        if (!orgResult.IsSuccess)
        {
            await states.DeleteAsync(stored, ct);
            await states.SaveChangesAsync(ct);
            return Result<Guid>.Fail(orgResult.Errors);
        }

        // Upsert: verwijder eventuele oude connectie (her-connect na disconnect),
        // dan nieuwe rij. Goedkoper dan in-place update + dekt tabel-state altijd.
        await connections.DeleteByOrganizationAsync(stored.OrganizationId, ct);

        MollieConnection connection = new()
        {
            OrganizationId = stored.OrganizationId,
            MollieOrganizationId = orgResult.Value!.Id,
            MollieOrganizationName = orgResult.Value!.Name,
            AccessTokenEncrypted = protector.Protect(tokens.AccessToken),
            RefreshTokenEncrypted = protector.Protect(tokens.RefreshToken),
            AccessTokenExpiresAt = utcNow.AddSeconds(tokens.ExpiresInSeconds),
            ConnectedAt = utcNow,
        };
        await connections.AddAsync(connection, ct);
        await connections.SaveChangesAsync(ct);

        await states.DeleteAsync(stored, ct);
        await states.SaveChangesAsync(ct);

        return Result<Guid>.Ok(stored.OrganizationId);
    }

    public async Task<Result> DisconnectAsync(Guid organizationId, CancellationToken ct = default)
    {
        MollieConnection? existing = await connections.GetByOrganizationAsync(organizationId, ct);
        if (existing is null)
        {
            // Niet verbonden = niets te doen; idempotent.
            return Result.Ok();
        }

        // Probeer Mollie token in te trekken; we negeren netwerkfouten zodat de
        // lokale state altijd consistent eindigt (geen "deels verbonden").
        try
        {
            string refreshToken = protector.Unprotect(existing.RefreshTokenEncrypted);
            Result revokeResult = await mollieClient.RevokeRefreshTokenAsync(refreshToken, ct);
            if (!revokeResult.IsSuccess)
            {
                logger.LogWarning("Mollie token revoke faalde voor org {OrgId}; lokale connectie wordt alsnog verwijderd.", organizationId);
            }
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Refresh token voor org {OrgId} kon niet worden ontsleuteld; verwijder enkel lokaal.", organizationId);
        }

        await connections.DeleteByOrganizationAsync(organizationId, ct);
        await connections.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<MollieConnectionStatusDto>> GetStatusAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        MollieConnection? connection = await connections.GetByOrganizationReadOnlyAsync(organizationId, ct);
        if (connection is null)
        {
            return Result<MollieConnectionStatusDto>.Ok(new MollieConnectionStatusDto(false, null, null));
        }

        return Result<MollieConnectionStatusDto>.Ok(new MollieConnectionStatusDto(
            true,
            connection.MollieOrganizationName,
            connection.ConnectedAt));
    }

    public async Task<Result<string>> GetValidAccessTokenAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        MollieConnection? connection = await connections.GetByOrganizationAsync(organizationId, ct);
        if (connection is null)
        {
            return Result<string>.Fail(new Error(
                ErrorCodes.NotFound,
                "Deze club is niet aan Mollie gekoppeld."));
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        // Refresh wanneer token al verlopen is OF binnen 5 minuten verloopt;
        // marge dekt netwerklatentie + Mollie API rate limiting.
        bool needsRefresh = connection.AccessTokenExpiresAt <= utcNow.AddMinutes(5);

        if (!needsRefresh)
        {
            try
            {
                return Result<string>.Ok(protector.Unprotect(connection.AccessTokenEncrypted));
            }
            catch (CryptographicException ex)
            {
                logger.LogError(ex, "Access token voor org {OrgId} kon niet worden ontsleuteld.", organizationId);
                return Result<string>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie-koppeling is beschadigd; koppel opnieuw via instellingen."));
            }
        }

        string refreshToken;
        try
        {
            refreshToken = protector.Unprotect(connection.RefreshTokenEncrypted);
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Refresh token voor org {OrgId} kon niet worden ontsleuteld.", organizationId);
            return Result<string>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Mollie-koppeling is beschadigd; koppel opnieuw via instellingen."));
        }

        Result<MollieTokenResponse> refreshed = await mollieClient.RefreshTokenAsync(refreshToken, ct);
        if (!refreshed.IsSuccess)
        {
            return Result<string>.Fail(refreshed.Errors);
        }

        MollieTokenResponse tokens = refreshed.Value!;
        connection.AccessTokenEncrypted = protector.Protect(tokens.AccessToken);
        connection.RefreshTokenEncrypted = protector.Protect(tokens.RefreshToken);
        connection.AccessTokenExpiresAt = utcNow.AddSeconds(tokens.ExpiresInSeconds);
        await connections.SaveChangesAsync(ct);

        return Result<string>.Ok(tokens.AccessToken);
    }

    private string BuildAuthorizationUrl(string state, string redirectUri)
    {
        // Mollie verwacht space-separated scopes in de scope query param.
        // We laten de URL-encoder dit voor zijn rekening nemen.
        Dictionary<string, string> queryParams = new()
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["scope"] = _options.Scopes,
            ["response_type"] = "code",
            ["approval_prompt"] = "auto",
        };
        string query = string.Join("&", queryParams
            .Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));
        return $"{_options.OAuthBaseUrl}/oauth2/authorize?{query}";
    }

    private static string GenerateSecureToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(StateByteLength);
        // Url-safe base64: vervang +/= door -_ (zonder padding) zodat de string
        // veilig in een query parameter past zonder extra encoding-noise.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
