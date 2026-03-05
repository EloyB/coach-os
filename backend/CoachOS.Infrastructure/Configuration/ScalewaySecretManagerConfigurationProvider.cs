using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CoachOS.Infrastructure.Configuration;

/// <summary>
/// Pulls secrets from Scaleway Secret Manager at startup and injects them
/// into the .NET configuration system as regular key/value pairs.
/// </summary>
public class ScalewaySecretManagerConfigurationProvider : ConfigurationProvider
{
    private readonly string _apiKey;
    private readonly string _region;

    /// <summary>Maps Scaleway secret name → .NET config key (e.g. "coachos-email-username" → "Email:Username").</summary>
    private readonly Dictionary<string, string> _secretMappings;

    public ScalewaySecretManagerConfigurationProvider(
        string apiKey,
        string region,
        Dictionary<string, string> secretMappings)
    {
        _apiKey = apiKey;
        _region = region;
        _secretMappings = secretMappings;
    }

    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    private async Task LoadAsync()
    {
        using HttpClient http = new();
        http.DefaultRequestHeaders.Add("X-Auth-Token", _apiKey);

        // Step 1: list all secrets to build name → id map
        string listUrl = $"https://api.scaleway.com/secret-manager/v1beta1/regions/{_region}/secrets?page_size=100";
        string listJson = await http.GetStringAsync(listUrl);
        using JsonDocument listDoc = JsonDocument.Parse(listJson);

        Dictionary<string, string> nameToId = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement secret in listDoc.RootElement.GetProperty("secrets").EnumerateArray())
        {
            string name = secret.GetProperty("name").GetString() ?? string.Empty;
            string id   = secret.GetProperty("id").GetString()   ?? string.Empty;
            if (name.Length > 0 && id.Length > 0)
                nameToId[name] = id;
        }

        // Step 2: fetch each required secret by its ID
        foreach (KeyValuePair<string, string> mapping in _secretMappings)
        {
            string secretName = mapping.Key;
            string configKey  = mapping.Value;

            if (!nameToId.TryGetValue(secretName, out string? secretId))
                throw new InvalidOperationException(
                    $"Scaleway secret '{secretName}' niet gevonden in regio '{_region}'.");

            string url = $"https://api.scaleway.com/secret-manager/v1beta1/regions/{_region}" +
                         $"/secrets/{secretId}/versions/latest/access";

            try
            {
                HttpResponseMessage response = await http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

                string base64 = doc.RootElement.GetProperty("data").GetString()
                    ?? throw new InvalidOperationException($"Geen data in secret '{secretName}'.");

                Data[configKey] = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Ophalen van Scaleway secret '{secretName}' mislukt: {ex.Message}", ex);
            }
        }
    }
}
