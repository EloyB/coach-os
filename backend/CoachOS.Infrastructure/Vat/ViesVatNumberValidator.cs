using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoachOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Vat;

/// <summary>
/// Btw-controle via de REST-API van VIES (Europese Commissie). Elke fout of time-out geeft
/// <see cref="VatCheckResult.Unavailable"/>: VIES is regelmatig traag of plat en mag een
/// club nooit blokkeren.
/// </summary>
public class ViesVatNumberValidator(HttpClient httpClient, ILogger<ViesVatNumberValidator> logger) : IVatNumberValidator
{
    public const string Endpoint = "https://ec.europa.eu/taxation_customs/vies/rest-api/check-vat-number";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<VatCheckResult> CheckAsync(string normalizedVatNumber, CancellationToken ct = default)
    {
        string countryCode = normalizedVatNumber[..2];
        string number = normalizedVatNumber[2..];

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                Endpoint, new { countryCode, vatNumber = number }, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("VIES gaf {Status} voor {Country}", (int)response.StatusCode, countryCode);
                return VatCheckResult.Unavailable;
            }

            ViesResponse? payload = await response.Content.ReadFromJsonAsync<ViesResponse>(timeout.Token);
            return payload?.Valid switch
            {
                true => VatCheckResult.Valid,
                false => VatCheckResult.Invalid,
                null => VatCheckResult.Unavailable,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "VIES niet bereikbaar");
            return VatCheckResult.Unavailable;
        }
    }

    private sealed record ViesResponse([property: JsonPropertyName("valid")] bool? Valid);
}
