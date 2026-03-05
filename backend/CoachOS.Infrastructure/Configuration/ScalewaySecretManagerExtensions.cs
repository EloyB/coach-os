using Microsoft.Extensions.Configuration;

namespace CoachOS.Infrastructure.Configuration;

public static class ScalewaySecretManagerExtensions
{
    /// <summary>
    /// Adds Scaleway Secret Manager as a configuration source.
    /// Secrets are fetched once at startup and merged into the configuration system.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="apiKey">Scaleway IAM API key.</param>
    /// <param name="region">Scaleway region (e.g. "nl-ams").</param>
    /// <param name="secrets">Dictionary mapping Scaleway secret name → .NET config key.</param>
    public static IConfigurationBuilder AddScalewaySecretManager(
        this IConfigurationBuilder builder,
        string apiKey,
        string region,
        Dictionary<string, string> secrets)
    {
        builder.Add(new ScalewaySecretManagerConfigurationSource(apiKey, region, secrets));
        return builder;
    }
}

internal class ScalewaySecretManagerConfigurationSource : IConfigurationSource
{
    private readonly string _apiKey;
    private readonly string _region;
    private readonly Dictionary<string, string> _secrets;

    internal ScalewaySecretManagerConfigurationSource(
        string apiKey,
        string region,
        Dictionary<string, string> secrets)
    {
        _apiKey = apiKey;
        _region = region;
        _secrets = secrets;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new ScalewaySecretManagerConfigurationProvider(_apiKey, _region, _secrets);
}
