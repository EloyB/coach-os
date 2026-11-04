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
        string method, string path, string status, string accessLevel)
    {
        Mock<ISubscriptionAccessService> access = new();
        access.Setup(a => a.GetStatusAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(status, accessLevel, null, null, false)));

        ServiceCollection services = new();
        services.AddSingleton(access.Object);

        DefaultHttpContext context = new()
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(CoachOsClaims.OrganizationId, OrgId.ToString())], "test")),
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
