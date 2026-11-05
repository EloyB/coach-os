using System.Security.Claims;
using CoachOS.API.Auth;
using CoachOS.API.Middleware;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Endpoints;

[TestFixture]
public class SubscriptionAccessMiddlewareTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static async Task<(int Status, bool NextCalled)> InvokeAsync(
        string method, string path, string status, string accessLevel, string? role = "Admin", bool anonymous = false)
    {
        Mock<ISubscriptionAccessService> access = new();
        access.Setup(a => a.GetStatusAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(status, accessLevel, null, null, false)));

        ServiceCollection services = new();
        services.AddSingleton(access.Object);
        Mock<IOrganizationPublicScopeResolver> resolver = new();
        resolver.Setup(x => x.ResolveAsync(It.Is<string>(r => new[] { "lessonseries", "camps", "camp-enrollments", "payments", "confirmation", "invitations" }.Contains(r)), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(OrgId);
        services.AddSingleton(resolver.Object);

        DefaultHttpContext context = new()
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(CoachOsClaims.OrganizationId, OrgId.ToString()), new Claim(ClaimTypes.Role, role ?? "Trainer")], anonymous ? null : "test")),
        };
        context.Request.Method = method;
        context.Request.Path = path;

        bool nextCalled = false;
        SubscriptionAccessMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, nextCalled);
    }

    [Test]
    public async Task ReadOnlyClubOnlyAllowsClubAdminReadsNotTrainerReads()
    {
        (int status, bool nextCalled) = await InvokeAsync("GET", "/api/tennisclubs", "Cancelled", "ReadOnly", "Trainer");
        status.Should().Be(402); nextCalled.Should().BeFalse();
    }

    [TestCase("POST", "/api/public/lessonseries/11111111-1111-4111-8111-111111111111/enroll")]
    [TestCase("POST", "/api/public/camps/11111111-1111-4111-8111-111111111111/enroll")]
    [TestCase("GET", "/api/public/confirmation/synthetic-token")]
    [TestCase("POST", "/api/PUBLIC/CAMPS/11111111-1111-4111-8111-111111111111/ENROLL")]
    [TestCase("GET", "/api/PUBLIC/CONFIRMATION/CaseSensitiveSyntheticToken")]
    public async Task AnonymousPublicScopeCannotBypassEndedOrganizationGate(string method, string path)
    {
        (int status, bool nextCalled) = await InvokeAsync(method, path, "Cancelled", "ReadOnly", anonymous: true);
        status.Should().Be(402); nextCalled.Should().BeFalse();
    }

    [TestCase("GET")]
    [TestCase("POST")]
    public async Task ClosedClubCannotReadOrWriteNormalData(string method)
    {
        (int status, bool nextCalled) = await InvokeAsync(method, "/api/tennisclubs", "Cancelled", "Closed");
        status.Should().Be(402); nextCalled.Should().BeFalse();
    }

    [Test]
    public async Task SimilarLookingBillingPathDoesNotBypassReadOnlyGate()
    {
        (int status, bool nextCalled) = await InvokeAsync("POST", "/api/billing-unsafe", "Cancelled", "ReadOnly");
        status.Should().Be(402); nextCalled.Should().BeFalse();
    }

    [Test]
    public async Task Locked_WriteRequest_IsBlockedRegardlessOfTrialEnforcement()
    {
        (int status, bool nextCalled) = await InvokeAsync("POST", "/api/tennisclubs", "Locked", "ReadOnly");

        status.Should().Be(StatusCodes.Status402PaymentRequired);
        nextCalled.Should().BeFalse();
    }

    [Test]
    public async Task Locked_BillingRequest_Passes()
    {
        (_, bool nextCalled) = await InvokeAsync("POST", "/api/billing/checkout", "Locked", "ReadOnly");

        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task Locked_ReadRequest_Passes()
    {
        (_, bool nextCalled) = await InvokeAsync("GET", "/api/tennisclubs", "Locked", "ReadOnly");

        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task FullAccess_WriteRequest_Passes()
    {
        (_, bool nextCalled) = await InvokeAsync("POST", "/api/tennisclubs", "Trialing", "Full");

        nextCalled.Should().BeTrue();
    }
}
