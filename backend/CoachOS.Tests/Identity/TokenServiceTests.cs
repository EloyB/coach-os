using System.IdentityModel.Tokens.Jwt;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _service = null!;

    [SetUp]
    public void SetUp()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "coachos-test",
                ["Jwt:Audience"] = "coachos-test",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();
        _service = new TokenService(config);
    }

    [Test]
    public void GenerateToken_HeadTrainerOfTwoClubs_EmitsClaimPerClub()
    {
        Guid clubA = Guid.NewGuid();
        Guid clubB = Guid.NewGuid();
        ApplicationUser user = new() { Id = Guid.NewGuid(), Email = "ht@example.com", FirstName = "H", LastName = "T" };
        OrganizationMembership membership = new()
        {
            UserId = user.Id,
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.Trainer,
            HeadTrainerClubs =
            [
                new HeadTrainerClub { TennisClubId = clubA },
                new HeadTrainerClub { TennisClubId = clubB },
            ]
        };

        (string token, _) = _service.GenerateToken(user, membership);

        JwtSecurityToken decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<string> clubClaims = decoded.Claims
            .Where(c => c.Type == "headTrainerClub")
            .Select(c => c.Value)
            .ToList();
        clubClaims.Should().BeEquivalentTo([clubA.ToString(), clubB.ToString()]);
    }

    [Test]
    public void GenerateToken_NoHeadTrainerClubs_EmitsNoClaim()
    {
        ApplicationUser user = new() { Id = Guid.NewGuid(), Email = "trainer@example.com", FirstName = "T", LastName = "R" };
        OrganizationMembership membership = new()
        {
            UserId = user.Id,
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.Trainer,
        };

        (string token, _) = _service.GenerateToken(user, membership);

        JwtSecurityToken decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);
        decoded.Claims.Should().NotContain(c => c.Type == "headTrainerClub");
    }
}
