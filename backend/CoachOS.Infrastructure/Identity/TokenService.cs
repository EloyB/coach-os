using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CoachOS.Infrastructure.Identity;

public sealed class TokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAt) GenerateStudentToken(string email)
    {
        (var key, var issuer, var audience, var expiryMinutes) = ReadJwtConfig();

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "Student")
        ];

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Genereert een JWT voor een user binnen een specifieke <see cref="OrganizationMembership"/>.
    /// De <c>organizationId</c> en <c>role</c> claims komen altijd uit de membership — niet van
    /// de user zelf — zodat multi-org users een sessie per organisatie kunnen hebben.
    /// </summary>
    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, OrganizationMembership membership)
    {
        (var key, var issuer, var audience, var expiryMinutes) = ReadJwtConfig();

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, membership.Role.ToString()),
            new("organizationId", membership.OrganizationId.ToString())
        ];

        foreach (HeadTrainerClub grant in membership.HeadTrainerClubs)
        {
            claims.Add(new Claim("headTrainerClub", grant.TennisClubId.ToString()));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>Synthesizeert een tijdelijke membership in-memory — voor registratie waar nog geen membership is opgeslagen.</summary>
    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, Guid organizationId, UserRole role) =>
        GenerateToken(user, new OrganizationMembership
        {
            UserId = user.Id,
            OrganizationId = organizationId,
            Role = role,
            IsActive = true
        });

    /// <summary>
    /// Mint een system-level super-admin JWT. Bevat <c>isSuperAdmin=true</c> en GEEN
    /// <c>organizationId</c> claim — defense in depth: een super-admin token kan
    /// nooit als gewone org-admin functioneren in een specifieke tenant (issue #91, Option C).
    /// </summary>
    public (string Token, DateTime ExpiresAt) GenerateSuperAdminToken(ApplicationUser user)
    {
        (var key, var issuer, var audience, var expiryMinutes) = ReadJwtConfig();

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "SuperAdmin"),
            new("isSuperAdmin", "true")
        ];

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private (string Key, string Issuer, string Audience, int ExpiryMinutes) ReadJwtConfig()
    {
        // Scaleway Secret Manager literal names hebben voorrang, colon-vorm is fallback voor local dev.
        var key = (configuration["Jwt__Key"] ?? configuration["Jwt:Key"])!;
        var issuer = (configuration["Jwt__Issuer"] ?? configuration["Jwt:Issuer"])!;
        var audience = (configuration["Jwt__Audience"] ?? configuration["Jwt:Audience"])!;
        var expiryMinutes = int.Parse((configuration["Jwt__ExpiryMinutes"] ?? configuration["Jwt:ExpiryMinutes"])!);
        return (key, issuer, audience, expiryMinutes);
    }
}
