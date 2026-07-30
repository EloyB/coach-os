using System.Security.Claims;
using CoachOS.API.Middleware;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Middleware;

[TestFixture]
public class SubscriptionAccessMiddlewareTests
{
    private sealed class FakeTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid OrganizationId { get; } = organizationId;
        public Guid UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated { get; } = true;
    }

    private static DefaultHttpContext AuthedContext(string path, Guid orgId)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("organizationId", orgId.ToString())],
            authenticationType: "Test"));
        return ctx;
    }

    [Test]
    public async Task DeniesProtectedPath_WhenTrialExpired()
    {
        var orgId = Guid.NewGuid();
        var repo = new Mock<ISubscriptionRepository>();
        repo.Setup(r => r.GetByOrganizationAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(-1) });

        var tenant = new FakeTenantContext(orgId);
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        var ctx = AuthedContext("/api/lessons", orgId);

        await mw.InvokeAsync(ctx, repo.Object, tenant);

        called.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task AllowsProtectedPath_WhenTrialActive()
    {
        var orgId = Guid.NewGuid();
        var repo = new Mock<ISubscriptionRepository>();
        repo.Setup(r => r.GetByOrganizationAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(5) });

        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        await mw.InvokeAsync(AuthedContext("/api/lessons", orgId), repo.Object, new FakeTenantContext(orgId));

        called.Should().BeTrue();
    }

    [Test]
    public async Task AllowsBillingPath_EvenWhenExpired()
    {
        var orgId = Guid.NewGuid();
        var repo = new Mock<ISubscriptionRepository>();
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        await mw.InvokeAsync(AuthedContext("/api/billing/status", orgId), repo.Object, new FakeTenantContext(orgId));

        called.Should().BeTrue();
        repo.Verify(r => r.GetByOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GatesLookalikePath_ThatOnlySharesPrefixWithAllowlist()
    {
        var orgId = Guid.NewGuid();
        var repo = new Mock<ISubscriptionRepository>();
        repo.Setup(r => r.GetByOrganizationAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription { Status = SubscriptionStatus.Trialing, TrialEndsAt = DateTime.UtcNow.AddDays(-1) });

        var tenant = new FakeTenantContext(orgId);
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        var ctx = AuthedContext("/api/authX", orgId);

        await mw.InvokeAsync(ctx, repo.Object, tenant);

        called.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task AllowsAuthLoginPath_EvenWhenExpired()
    {
        var orgId = Guid.NewGuid();
        var repo = new Mock<ISubscriptionRepository>();
        var called = false;
        var mw = new SubscriptionAccessMiddleware(_ => { called = true; return Task.CompletedTask; });
        await mw.InvokeAsync(AuthedContext("/api/auth/login", orgId), repo.Object, new FakeTenantContext(orgId));

        called.Should().BeTrue();
        repo.Verify(r => r.GetByOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
