using System.Security.Claims;
using AwesomeAssertions;
using CoachOS.API.Endpoints;
using CoachOS.Application;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Infrastructure;
using CoachOS.Infrastructure.Email;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailWiringTests
{
    [TestCase("", "password", false)]
    [TestCase("not-an-email", "password", false)]
    [TestCase("valid@example.be", "", false)]
    [TestCase("valid@example.be", "password", true)]
    [TestCase(" valid@example.be ", "password", true)]
    public void RequestValidator_ValidatesBodyWithDutchErrors(string address, string password, bool valid)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddApplication();
        using ServiceProvider provider = services.BuildServiceProvider();
        FluentValidation.Results.ValidationResult result = provider.GetRequiredService<IValidator<RequestAccountEmailChange>>()
            .Validate(new(address, password));
        result.IsValid.Should().Be(valid);
        if (!valid) result.Errors.Should().OnlyContain(e => e.ErrorMessage.EndsWith("."));
    }

    [TestCase("", "token", false)]
    [TestCase("invalid", "token", false)]
    [TestCase("valid@example.be", "", false)]
    [TestCase("valid@example.be", "token", true)]
    public void ConfirmValidator_RejectsIncompleteLinks(string address, string token, bool valid)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddApplication();
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IValidator<ConfirmAccountEmailChange>>().Validate(new(address, token)).IsValid.Should().Be(valid);
    }

    [Test]
    public void MailTemplate_RendersEscapedLinkAndThirtyMinuteLifetime()
    {
        MjmlTemplateRenderer renderer = new(NullLogger<MjmlTemplateRenderer>.Instance);
        string html = renderer.Render("account-email-change", new Dictionary<string, string>
        {
            ["firstName"] = "<script>bad</script>", ["confirmationUrl"] = "https://app.example.be/dashboard/account#email=a%40b.be&token=abc", ["year"] = "2026",
        });
        html.Should().Contain("30 minuten").And.Contain("/dashboard/account#email=")
            .And.NotContain("<script>bad</script>").And.NotContain("{{");
    }

    [Test]
    public async Task ChangeTokens_AreShortLived_AndBoundToUserAddressPurposeAndStamp()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddApplication().AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused" }).Build());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Microsoft.AspNetCore.Identity.UserManager<CoachOS.Infrastructure.Identity.ApplicationUser> users = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<CoachOS.Infrastructure.Identity.ApplicationUser>>();
        string name = users.Options.Tokens.ChangeEmailTokenProvider;
        name.Should().NotBe(users.Options.Tokens.PasswordResetTokenProvider);
        Microsoft.AspNetCore.Identity.DataProtectorTokenProvider<CoachOS.Infrastructure.Identity.ApplicationUser> tokens =
            (Microsoft.AspNetCore.Identity.DataProtectorTokenProvider<CoachOS.Infrastructure.Identity.ApplicationUser>)scope.ServiceProvider
                .GetRequiredService(users.Options.Tokens.ProviderMap[name].ProviderType!);
        CoachOS.Infrastructure.Identity.ApplicationUser user = new() { Id = Guid.NewGuid(), SecurityStamp = "initial" };
        string token = await users.GenerateChangeEmailTokenAsync(user, "new@example.be");
        (await users.VerifyUserTokenAsync(user, name, "ChangeEmail:new@example.be", token)).Should().BeTrue();
        (await users.VerifyUserTokenAsync(user, name, "ChangeEmail:other@example.be", token)).Should().BeFalse();
        (await users.VerifyUserTokenAsync(user, name, "ResetPassword", token)).Should().BeFalse();
        (await users.VerifyUserTokenAsync(new() { Id = Guid.NewGuid(), SecurityStamp = "initial" }, name, "ChangeEmail:new@example.be", token)).Should().BeFalse();
        user.SecurityStamp = "replacement";
        (await users.VerifyUserTokenAsync(user, name, "ChangeEmail:new@example.be", token)).Should().BeFalse();
        user.SecurityStamp = "initial";
        // Protect a genuine provider-format token issued 31 minutes ago: no waits or fake API.
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(DateTimeOffset.UtcNow.AddMinutes(-31).UtcTicks);
            writer.Write(user.Id.ToString());
            writer.Write("ChangeEmail:new@example.be");
            writer.Write(user.SecurityStamp);
        }
        Microsoft.AspNetCore.DataProtection.IDataProtector protector = Microsoft.AspNetCore.DataProtection.DataProtectionCommonExtensions.CreateProtector(
            scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(), "CoachOS.AccountEmailChange");
        string expired = Convert.ToBase64String(protector.Protect(stream.ToArray()));
        (await users.VerifyUserTokenAsync(user, name, "ChangeEmail:new@example.be", expired)).Should().BeFalse();
        scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CoachOS.Infrastructure.Identity.AccountEmailTokenOptions>>()
            .Value.TokenLifespan.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task Routes_RequireStaffIdentity_AndResolveFeatureDependencies()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=not_used;Username=test;Password=test",
        });
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        await using WebApplication app = builder.Build();
        app.MapAllEndpoints();
        RouteEndpoint[] routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints)
            .OfType<RouteEndpoint>().Where(e => e.RoutePattern.RawText!.Contains("/auth/email-change/")).ToArray();
        routes.Should().HaveCount(2);
        foreach (RouteEndpoint route in routes)
        {
            IReadOnlyList<IAuthorizeData> data = route.Metadata.GetOrderedMetadata<IAuthorizeData>();
            AuthorizationPolicy policy = (await AuthorizationPolicy.CombineAsync(app.Services.GetRequiredService<IAuthorizationPolicyProvider>(), data, route.Metadata.GetOrderedMetadata<AuthorizationPolicy>()))!;
            IAuthorizationService authorization = app.Services.GetRequiredService<IAuthorizationService>();
            foreach (string role in new[] { "Admin", "Trainer", "Student" })
            {
                ClaimsPrincipal principal = new(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                }, "test"));
                (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded.Should().Be(role != "Student");
            }
            ClaimsPrincipal missingId = new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "test"));
            (await authorization.AuthorizeAsync(missingId, null, policy)).Succeeded.Should().BeFalse();
            ClaimsPrincipal anonymous = new(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            }));
            (await authorization.AuthorizeAsync(anonymous, null, policy)).Succeeded.Should().BeFalse();
        }
        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IAccountEmailService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IValidator<RequestAccountEmailChange>>().Validate(new("new@example.be", "Current123")).IsValid.Should().BeTrue();
        scope.ServiceProvider.GetRequiredService<IValidator<ConfirmAccountEmailChange>>().Validate(new("new@example.be", "token")).IsValid.Should().BeTrue();
    }
}
