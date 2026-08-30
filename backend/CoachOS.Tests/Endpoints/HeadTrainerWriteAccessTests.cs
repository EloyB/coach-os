using System.Security.Claims;
using CoachOS.API.Auth;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace CoachOS.Tests.Endpoints;

/// <summary>
/// Regressietest voor de hoofdtrainer-write-restrictie op muterende inschrijvings-endpoints
/// (o.a. <c>DELETE /lessonseries/{id}/enrollment-groups/{groupId}</c>).
/// Bug: het endpoint autoriseerde elke Trainer, maar hoofdtrainers zijn read-only. Een hoofdtrainer
/// kon de verborgen UI omzeilen en de call rechtstreeks doen om een hele groep te annuleren.
/// Client-side verbergen is geen autorisatiegrens; <see cref="HeadTrainerAccess.EnsureWriteAllowed"/>
/// dwingt het af op de API.
/// </summary>
[TestFixture]
public class HeadTrainerWriteAccessTests
{
    private static HttpContext BuildHttpContext(string role, params Guid[] headTrainerClubIds)
    {
        List<Claim> claims = [new Claim(ClaimTypes.Role, role)];
        claims.AddRange(headTrainerClubIds.Select(id =>
            new Claim(CoachOsClaims.HeadTrainerClub, id.ToString())));

        ClaimsIdentity identity = new(claims, authenticationType: "TestAuth");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    [Test]
    public void EnsureWriteAllowed_HeadTrainer_IsForbidden()
    {
        // Arrange — trainer die hoofdtrainer is van een club (read-only viewer).
        HttpContext ctx = BuildHttpContext("Trainer", Guid.NewGuid());

        // Act
        Result result = HeadTrainerAccess.EnsureWriteAllowed(ctx);

        // Assert — geblokkeerd met Forbidden (403).
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.Forbidden);
    }

    [Test]
    public void EnsureWriteAllowed_Admin_IsAllowed()
    {
        // Arrange — admin mag alles, ook als er (theoretisch) club-claims zouden zijn.
        HttpContext ctx = BuildHttpContext("Admin", Guid.NewGuid());

        // Act
        Result result = HeadTrainerAccess.EnsureWriteAllowed(ctx);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void EnsureWriteAllowed_RegularTrainerWithoutHeadClubs_IsAllowed()
    {
        // Arrange — gewone trainer zonder hoofdtrainer-club is geen read-only viewer.
        HttpContext ctx = BuildHttpContext("Trainer");

        // Act
        Result result = HeadTrainerAccess.EnsureWriteAllowed(ctx);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
