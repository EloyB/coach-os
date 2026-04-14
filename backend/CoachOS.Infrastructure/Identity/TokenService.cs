using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user)
    {
        // Prefer Scaleway Secret Manager literal names, fall back to colon form
        // (nested appsettings.json) for local dev.
        var key = (configuration["Jwt__Key"] ?? configuration["Jwt:Key"])!;
        var issuer = (configuration["Jwt__Issuer"] ?? configuration["Jwt:Issuer"])!;
        var audience = (configuration["Jwt__Audience"] ?? configuration["Jwt:Audience"])!;
        var expiryMinutes = int.Parse((configuration["Jwt__ExpiryMinutes"] ?? configuration["Jwt:ExpiryMinutes"])!);

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claimsList =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        if (user.OrganizationId.HasValue)
            claimsList.Add(new Claim("organizationId", user.OrganizationId.Value.ToString()));

        var claims = claimsList.ToArray();

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
        var key = (configuration["Jwt__Key"] ?? configuration["Jwt:Key"])!;
        var issuer = (configuration["Jwt__Issuer"] ?? configuration["Jwt:Issuer"])!;
        var audience = (configuration["Jwt__Audience"] ?? configuration["Jwt:Audience"])!;
        var expiryMinutes = int.Parse((configuration["Jwt__ExpiryMinutes"] ?? configuration["Jwt:ExpiryMinutes"])!);
        return (key, issuer, audience, expiryMinutes);
    }
}
