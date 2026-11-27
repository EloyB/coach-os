using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AwesomeAssertions;
using CoachOS.API.Endpoints.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailEndpointTests
{
    private sealed class StaffAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? role = Request.Headers["X-Test-Role"].FirstOrDefault();
            if (role is null) return Task.FromResult(AuthenticateResult.NoResult());
            List<Claim> claims = [new(ClaimTypes.Role, role)];
            if (role != "Student")
            {
                claims.Add(new(ClaimTypes.NameIdentifier, Request.Headers["X-Test-User"].ToString()));
                claims.Add(new("organizationId", Request.Headers["X-Test-Organization"].ToString()));
            }
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    [Test]
    public async Task HttpContracts_ValidateAuthorizeAndRateLimitPerAccount()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddApplication();
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, StaffAuthentication>("test", _ => { });
        builder.Services.AddRateLimiting(builder.Configuration);
        var service = new Mock<IAccountEmailService>();
        service.Setup(s => s.RequestAsync(It.IsAny<Guid>(), It.IsAny<RequestAccountEmailChange>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());
        service.Setup(s => s.ConfirmAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ConfirmAccountEmailChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Ok(new() { Email = "new@example.be", Token = "fresh-session", Role = "Trainer" }));
        builder.Services.AddSingleton(service.Object);
        await using WebApplication app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.Use((context, next) =>
        {
            if (IPAddress.TryParse(context.Request.Headers["X-Test-IP"].ToString(), out IPAddress? address))
                context.Connection.RemoteIpAddress = address;
            return next(context);
        });
        // Exercise the exact authentication/rate-limiting pipeline used by Program.cs.
        app.UseAuthenticatedRateLimiting();
        app.UseAuthorization();
        new AccountEmailEndpoints().MapEndpoint(app.MapGroup("/api"));
        await app.StartAsync();
        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
        RequestAccountEmailChange request = new("new@example.be", "Current123");
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Student");
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Trainer");
        Guid userId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Organization", organizationId.ToString());
        (await client.PostAsJsonAsync("/api/auth/email-change/request", new RequestAccountEmailChange("invalid", ""))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage confirmed = await client.PostAsJsonAsync("/api/auth/email-change/confirm", new ConfirmAccountEmailChange("new@example.be", "token"));
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await confirmed.Content.ReadFromJsonAsync<AuthResponseDto>())!.Token.Should().Be("fresh-session");
        service.Verify(s => s.ConfirmAsync(userId, organizationId, It.IsAny<ConfirmAccountEmailChange>(), It.IsAny<CancellationToken>()), Times.Once);
        service.Setup(s => s.RequestAsync(It.IsAny<Guid>(), It.IsAny<RequestAccountEmailChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new Error(ErrorCodes.Validation, "Controleer je huidige wachtwoord.")));
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        // The same account cannot reset its exhausted budget by changing IP.
        client.DefaultRequestHeaders.Add("X-Test-IP", "192.0.2.10");
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        // A different staff account on the same IP does not share the exhausted account budget.
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        (await client.PostAsJsonAsync("/api/auth/email-change/request", request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await app.StopAsync();
    }
}
