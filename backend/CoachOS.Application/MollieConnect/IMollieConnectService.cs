using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.MollieConnect;

public interface IMollieConnectService
{
    /// <summary>
    /// Genereert een CSRF state-token en bouwt de Mollie OAuth-autorisatie-URL.
    /// De frontend redirect de gebruiker naar deze URL.
    /// </summary>
    Task<Result<StartConnectResponse>> StartAsync(
        Guid organizationId,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>
    /// Handelt de OAuth-callback af: valideert state, wisselt code in voor tokens,
    /// haalt Mollie organisatie-info op en bewaart de versleutelde tokens als
    /// <c>MollieConnection</c>. Retourneert de gerelateerde <see cref="Guid"/>
    /// organizationId zodat de endpoint een correcte redirect-URL kan bouwen.
    /// </summary>
    Task<Result<Guid>> HandleCallbackAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>Trekt de Mollie tokens in en verwijdert de <c>MollieConnection</c>.</summary>
    Task<Result> DisconnectAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Read-only status-check; gebruikt door de admin settings-pagina.</summary>
    Task<Result<MollieConnectionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default);
}
