using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CoachOS.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-super-secret-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "CoachOS-Test",
            ["Jwt:Audience"] = "CoachOS-Test",
            ["Jwt:ExpiryMinutes"] = "60"
        }).Build();
        _service = new TokenService(config);
    }

    [Test]
    public void GenerateSuperAdminToken_emits_isSuperAdmin_claim_without_org_claim()
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            Email = "root@coach-os.be",
            UserName = "root@coach-os.be",
            FirstName = "Root",
            LastName = "User",
            IsSuperAdmin = true
        };

        (var token, var expiresAt) = _service.GenerateSuperAdminToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "isSuperAdmin" && c.Value == "true");
        jwt.Claims.Should().NotContain(c => c.Type == "organizationId");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    [Test]
    public void GenerateToken_for_org_membership_does_not_emit_isSuperAdmin_claim()
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            Email = "admin@brederode.be",
            UserName = "admin@brederode.be",
            FirstName = "Jan",
            LastName = "Janssen"
        };
        var orgId = Guid.NewGuid();

        (var token, _) = _service.GenerateToken(user, orgId, Domain.Enums.UserRole.Admin);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == "isSuperAdmin");
        jwt.Claims.Should().Contain(c => c.Type == "organizationId" && c.Value == orgId.ToString());
    }
}
