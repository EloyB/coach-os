using AwesomeAssertions;
using CoachOS.Application;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture, NonParallelizable]
public class AccountEmailPostgresTests
{
    private string? _admin;
    private string _database = null!;
    private ServiceProvider _provider = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG ontbreekt: echte PostgreSQL vereist.");
        _database = $"coachos_email_{Guid.NewGuid():N}";
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand create = new($"CREATE DATABASE \"{_database}\"", admin);
        await create.ExecuteNonQueryAsync();
        NpgsqlConnectionStringBuilder connection = new(_admin) { Database = _database };
        ServiceCollection services = new();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString,
            ["Jwt:Key"] = "synthetic-local-test-signing-key-at-least-32-bytes",
            ["Jwt:Issuer"] = "coachos-test",
            ["Jwt:Audience"] = "coachos-test",
            ["Jwt:ExpiryMinutes"] = "60",
        }).Build();
        services.AddSingleton(configuration);
        services.AddApplication().AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider();
        using IServiceScope scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if (_provider is not null) await _provider.DisposeAsync();
        if (_database is null || string.IsNullOrWhiteSpace(_admin)) return;
        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private async Task<(Guid Id, string Email)> Seed()
    {
        using IServiceScope scope = _provider.CreateScope();
        string email = $"{Guid.NewGuid():N}@example.be";
        ApplicationUser user = new() { UserName = email, Email = email, EmailConfirmed = true, IsActive = true, FirstName = "Noor", LastName = "Test" };
        (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user, "Current123")).Succeeded.Should().BeTrue();
        return (user.Id, email);
    }

    private async Task<Result<EmailChangeChallenge>> Begin(Guid id, string address)
    {
        using IServiceScope scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountEmailIdentity>().BeginAsync(id, address, "Current123");
    }

    private async Task<Result> Confirm(Guid id, string address, string token)
    {
        using IServiceScope scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountEmailIdentity>().ConfirmAsync(id, address, token);
    }

    private async Task<ApplicationUser> Load(Guid id)
    {
        using IServiceScope scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AsNoTracking().SingleAsync(u => u.Id == id);
    }

    [Test]
    public async Task Confirm_ChangesSameAccount_AndRejectsReplay()
    {
        (Guid id, string original) = await Seed();
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> challenge = await Begin(id, address);
        (await Load(id)).Email.Should().Be(original);
        (await Confirm(id, address, challenge.Value!.Token)).IsSuccess.Should().BeTrue();
        ApplicationUser changed = await Load(id);
        changed.Email.Should().Be(address);
        changed.UserName.Should().Be(address);
        changed.NormalizedEmail.Should().Be(address.ToUpperInvariant());
        changed.NormalizedUserName.Should().Be(address.ToUpperInvariant());
        changed.EmailConfirmed.Should().BeTrue();
        (await Confirm(id, address, challenge.Value.Token)).IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Request_ReplacesPreviousChallenge_WithoutChangingCurrentEmail()
    {
        (Guid id, string original) = await Seed();
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> first = await Begin(id, address);
        Result<EmailChangeChallenge> second = await Begin(id, address);
        (await Confirm(id, address, first.Value!.Token)).IsSuccess.Should().BeFalse();
        (await Load(id)).Email.Should().Be(original);
        (await Confirm(id, address, second.Value!.Token)).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Confirm_ParallelReplay_HasExactlyOneWinner()
    {
        (Guid id, _) = await Seed();
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> challenge = await Begin(id, address);
        Result[] results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Confirm(id, address, challenge.Value!.Token)));
        results.Count(r => r.IsSuccess).Should().Be(1);
        (await Load(id)).Email.Should().Be(address);
    }

    [Test]
    public async Task Confirm_AddressTakenAfterRequest_RollsBackBothIdentityFields()
    {
        (Guid id, string original) = await Seed();
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> challenge = await Begin(id, address);
        using (IServiceScope scope = _provider.CreateScope())
        {
            ApplicationUser other = new() { UserName = address, Email = address, IsActive = true };
            (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(other)).Succeeded.Should().BeTrue();
        }
        (await Confirm(id, address, challenge.Value!.Token)).IsSuccess.Should().BeFalse();
        ApplicationUser unchanged = await Load(id);
        unchanged.Email.Should().Be(original);
        unchanged.UserName.Should().Be(original);
        unchanged.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task Confirm_TwoAccountsTargetSameAddress_HasExactlyOneWinner()
    {
        (Guid first, _) = await Seed();
        (Guid second, _) = await Seed();
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> a = await Begin(first, address);
        Result<EmailChangeChallenge> b = await Begin(second, address);
        Result[] results = await Task.WhenAll(Confirm(first, address, a.Value!.Token), Confirm(second, address, b.Value!.Token));
        results.Count(r => r.IsSuccess).Should().Be(1);
        using IServiceScope scope = _provider.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync(u => u.NormalizedEmail == address.ToUpperInvariant())).Should().Be(1);
    }

    [Test]
    public async Task ServiceConfirm_PreservesCurrentTrainerOrganizationAndAllMemberships()
    {
        (Guid id, _) = await Seed();
        Guid current;
        using (IServiceScope scope = _provider.CreateScope())
        {
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            CoachOS.Domain.Entities.Organization adminOrg = new() { Name = "Admin club", Email = "admin-club@example.be" };
            CoachOS.Domain.Entities.Organization trainerOrg = new() { Name = "Trainer club", Email = "trainer-club@example.be" };
            db.Organizations.AddRange(adminOrg, trainerOrg);
            db.OrganizationMemberships.AddRange(
                new() { UserId = id, Organization = adminOrg, Role = CoachOS.Domain.Enums.UserRole.Admin, JoinedAt = DateTime.UtcNow },
                new() { UserId = id, Organization = trainerOrg, Role = CoachOS.Domain.Enums.UserRole.Trainer, JoinedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            current = trainerOrg.Id;
        }
        string address = $"{Guid.NewGuid():N}@example.be";
        Result<EmailChangeChallenge> challenge = await Begin(id, address);
        using IServiceScope confirmation = _provider.CreateScope();
        Result<CoachOS.Application.Auth.DTOs.AuthResponseDto> result = await confirmation.ServiceProvider.GetRequiredService<IAccountEmailService>()
            .ConfirmAsync(id, current, new(address, challenge.Value!.Token));
        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(id);
        result.Value.Email.Should().Be(address);
        result.Value.OrganizationId.Should().Be(current);
        result.Value.Role.Should().Be("Trainer");
        result.Value.Memberships.Should().HaveCount(2);
        System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(result.Value.Token);
        jwt.Claims.Should().Contain(c => c.Type == "organizationId" && c.Value == current.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == address);
    }
}
