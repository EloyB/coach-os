using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CoachOS.Infrastructure.Identity;

public sealed class TokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user)
    {
        string key = configuration["Jwt:Key"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        int expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"]!);

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("organizationId", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
