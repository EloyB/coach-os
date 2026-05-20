using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class MollieConnectServiceTests
{
    private Mock<IMollieClient> _mollie = null!;
    private Mock<IMollieConnectionRepository> _connections = null!;
    private Mock<IOAuthStateRepository> _states = null!;
    private Mock<ITokenProtector> _protector = null!;
    private FixedTimeProvider _time = null!;
    private MollieConnectService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        _mollie = new Mock<IMollieClient>();
        _connections = new Mock<IMollieConnectionRepository>();
        _states = new Mock<IOAuthStateRepository>();
        _protector = new Mock<ITokenProtector>();
        _protector.Setup(p => p.Protect(It.IsAny<string>()))
            .Returns<string>(s => $"enc({s})");
        _protector.Setup(p => p.Unprotect(It.IsAny<string>()))
            .Returns<string>(s => s.StartsWith("enc(") ? s.Substring(4, s.Length - 5) : s);
        _time = new FixedTimeProvider(Now);

        IOptions<MollieOptions> options = Options.Create(new MollieOptions
        {
            ClientId = "test_client",
            ClientSecret = "test_secret",
            OAuthBaseUrl = "https://my.mollie.example",
            ApiBaseUrl = "https://api.mollie.example",
            Scopes = "payments.read payments.write organizations.read",
        });

        _sut = new MollieConnectService(
            _mollie.Object,
            _connections.Object,
            _states.Object,
            _protector.Object,
            options,
            _time,
            NullLogger<MollieConnectService>.Instance);
    }

    [Test]
    public async Task StartAsync_GeneratesStateAndAuthorizationUrl()
    {
        OAuthState? captured = null;
        _states.Setup(s => s.AddAsync(It.IsAny<OAuthState>(), It.IsAny<CancellationToken>()))
            .Callback<OAuthState, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        Result<StartConnectResponse> result = await _sut.StartAsync(
            OrgId, "https://app.example/api/oauth/mollie/callback");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AuthorizationUrl.Should().StartWith("https://my.mollie.example/oauth2/authorize?");
        result.Value!.AuthorizationUrl.Should().Contain("client_id=test_client");
        result.Value!.AuthorizationUrl.Should().Contain("response_type=code");

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.State.Should().NotBeNullOrEmpty();
        captured.ExpiresAt.Should().Be(Now.AddMinutes(15));

        _states.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_MissingClientId_ReturnsExternalServiceError()
    {
        IOptions<MollieOptions> options = Options.Create(new MollieOptions
        {
            ClientId = string.Empty,
            ClientSecret = "x",
        });
        MollieConnectService sut = new(
            _mollie.Object, _connections.Object, _states.Object, _protector.Object,
            options, _time, NullLogger<MollieConnectService>.Instance);

        Result<StartConnectResponse> result = await sut.StartAsync(OrgId, "https://x/");

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.ExternalService);
        _states.Verify(s => s.AddAsync(It.IsAny<OAuthState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleCallbackAsync_UnknownState_ReturnsUnauthorized()
    {
        _states.Setup(s => s.GetByStateAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthState?)null);

        Result<Guid> result = await _sut.HandleCallbackAsync("code", "nope", "https://x/");

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Unauthorized);
        _mollie.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleCallbackAsync_ExpiredState_DeletesAndFails()
    {
        OAuthState expired = new()
        {
            OrganizationId = OrgId,
            State = "abc",
            ExpiresAt = Now.AddMinutes(-1),
        };
        _states.Setup(s => s.GetByStateAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        Result<Guid> result = await _sut.HandleCallbackAsync("code", "abc", "https://x/");

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Unauthorized);
        _states.Verify(s => s.DeleteAsync(expired, It.IsAny<CancellationToken>()), Times.Once);
        _mollie.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleCallbackAsync_ValidFlow_SavesEncryptedConnection()
    {
        OAuthState live = new()
        {
            OrganizationId = OrgId,
            State = "good",
            ExpiresAt = Now.AddMinutes(5),
        };
        _states.Setup(s => s.GetByStateAsync("good", It.IsAny<CancellationToken>()))
            .ReturnsAsync(live);

        _mollie.Setup(m => m.ExchangeCodeForTokenAsync("code", "https://x/", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MollieTokenResponse>.Ok(new MollieTokenResponse(
                AccessToken: "access-1",
                RefreshToken: "refresh-1",
                ExpiresInSeconds: 3600,
                TokenType: "bearer",
                Scope: "payments.read")));

        _mollie.Setup(m => m.GetOrganizationAsync("access-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MollieOrganizationInfo>.Ok(new MollieOrganizationInfo("org_123", "Demo Club")));

        MollieConnection? saved = null;
        _connections.Setup(c => c.AddAsync(It.IsAny<MollieConnection>(), It.IsAny<CancellationToken>()))
            .Callback<MollieConnection, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _sut.HandleCallbackAsync("code", "good", "https://x/");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(OrgId);

        // Bestaande connectie (her-connect-scenario) wordt eerst opgeruimd.
        _connections.Verify(c => c.DeleteByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()), Times.Once);

        saved.Should().NotBeNull();
        saved!.OrganizationId.Should().Be(OrgId);
        saved.MollieOrganizationId.Should().Be("org_123");
        saved.MollieOrganizationName.Should().Be("Demo Club");
        saved.AccessTokenEncrypted.Should().Be("enc(access-1)");
        saved.RefreshTokenEncrypted.Should().Be("enc(refresh-1)");
        saved.AccessTokenExpiresAt.Should().Be(Now.AddSeconds(3600));
        saved.ConnectedAt.Should().Be(Now);

        _states.Verify(s => s.DeleteAsync(live, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleCallbackAsync_TokenExchangeFails_DeletesStateAndPropagatesError()
    {
        OAuthState live = new()
        {
            OrganizationId = OrgId,
            State = "good",
            ExpiresAt = Now.AddMinutes(5),
        };
        _states.Setup(s => s.GetByStateAsync("good", It.IsAny<CancellationToken>()))
            .ReturnsAsync(live);
        _mollie.Setup(m => m.ExchangeCodeForTokenAsync("code", "https://x/", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MollieTokenResponse>.Fail(new Error(ErrorCodes.ExternalService, "boom")));

        Result<Guid> result = await _sut.HandleCallbackAsync("code", "good", "https://x/");

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.ExternalService);
        _states.Verify(s => s.DeleteAsync(live, It.IsAny<CancellationToken>()), Times.Once);
        _connections.Verify(c => c.AddAsync(It.IsAny<MollieConnection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DisconnectAsync_NotConnected_IsNoop()
    {
        _connections.Setup(c => c.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MollieConnection?)null);

        Result result = await _sut.DisconnectAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        _mollie.Verify(m => m.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _connections.Verify(c => c.DeleteByOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DisconnectAsync_Connected_RevokesAndDeletes()
    {
        MollieConnection existing = new()
        {
            OrganizationId = OrgId,
            RefreshTokenEncrypted = "enc(refresh-x)",
            AccessTokenEncrypted = "enc(access-x)",
            MollieOrganizationId = "org_x",
        };
        _connections.Setup(c => c.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mollie.Setup(m => m.RevokeRefreshTokenAsync("refresh-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.DisconnectAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        _mollie.Verify(m => m.RevokeRefreshTokenAsync("refresh-x", It.IsAny<CancellationToken>()), Times.Once);
        _connections.Verify(c => c.DeleteByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DisconnectAsync_RevokeFails_StillDeletesLocally()
    {
        MollieConnection existing = new()
        {
            OrganizationId = OrgId,
            RefreshTokenEncrypted = "enc(refresh-x)",
            AccessTokenEncrypted = "enc(access-x)",
        };
        _connections.Setup(c => c.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mollie.Setup(m => m.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new Error(ErrorCodes.ExternalService, "mollie down")));

        Result result = await _sut.DisconnectAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        _connections.Verify(c => c.DeleteByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_NotConnected_ReturnsFalse()
    {
        _connections.Setup(c => c.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MollieConnection?)null);

        Result<MollieConnectionStatusDto> result = await _sut.GetStatusAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Connected.Should().BeFalse();
        result.Value!.MollieOrganizationName.Should().BeNull();
    }

    [Test]
    public async Task GetStatusAsync_Connected_ReturnsOrganizationName()
    {
        MollieConnection existing = new()
        {
            OrganizationId = OrgId,
            MollieOrganizationName = "Demo Club",
            ConnectedAt = Now,
        };
        _connections.Setup(c => c.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Result<MollieConnectionStatusDto> result = await _sut.GetStatusAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Connected.Should().BeTrue();
        result.Value!.MollieOrganizationName.Should().Be("Demo Club");
        result.Value!.ConnectedAt.Should().Be(Now);
    }

    /// <summary>Eenvoudige test-implementatie van <see cref="TimeProvider"/>.</summary>
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
