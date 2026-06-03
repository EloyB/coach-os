using CoachOS.API.Endpoints.MollieConnect;
using CoachOS.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace CoachOS.Tests.Endpoints;

/// <summary>
/// Regressietests voor de Mollie OAuth redirect URI-resolutie.
/// Bug: <c>BuildRedirectUri</c> negeerde de geconfigureerde
/// <see cref="MollieOptions.RedirectUri"/> en leidde de URI altijd af van het
/// request scheme/host. Achter de prod reverse proxy resolved scheme naar
/// <c>http</c>, waardoor de meegestuurde redirect_uri niet matchte met de
/// <c>https</c>-URI in het Mollie dashboard ("The redirect URI provided is
/// missing or does not match").
/// </summary>
[TestFixture]
public class MollieRedirectUriTests
{
    private static HttpContext BuildHttpContext(string scheme, string host)
    {
        DefaultHttpContext ctx = new();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host);
        return ctx;
    }

    [Test]
    public void BuildRedirectUri_WhenConfigured_UsesSecretVerbatimIgnoringRequestScheme()
    {
        // Arrange — request komt binnen als http (TLS-terminating proxy), maar
        // de geconfigureerde secret is de https-URI uit het Mollie dashboard.
        HttpContext ctx = BuildHttpContext("http", "app.coach-os.be");
        MollieOptions options = new()
        {
            RedirectUri = "https://app.coach-os.be/api/oauth/mollie/callback",
        };

        // Act
        string result = StartConnectEndpoint.BuildRedirectUri(ctx, options);

        // Assert — secret wint, niet de afgeleide http-URI.
        result.Should().Be("https://app.coach-os.be/api/oauth/mollie/callback");
    }

    [Test]
    public void BuildRedirectUri_WhenSecretEmpty_DerivesFromRequest()
    {
        // Arrange — lokaal dev: geen secret gezet.
        HttpContext ctx = BuildHttpContext("http", "localhost:5142");
        MollieOptions options = new() { RedirectUri = string.Empty };

        // Act
        string result = StartConnectEndpoint.BuildRedirectUri(ctx, options);

        // Assert
        result.Should().Be("http://localhost:5142/api/oauth/mollie/callback");
    }

    [Test]
    public void BuildRedirectUri_WhenSecretWhitespace_FallsBackToRequest()
    {
        // Arrange — een per ongeluk leeg/whitespace secret mag de fallback niet breken.
        HttpContext ctx = BuildHttpContext("https", "app.coach-os.be");
        MollieOptions options = new() { RedirectUri = "   " };

        // Act
        string result = StartConnectEndpoint.BuildRedirectUri(ctx, options);

        // Assert
        result.Should().Be("https://app.coach-os.be/api/oauth/mollie/callback");
    }
}
