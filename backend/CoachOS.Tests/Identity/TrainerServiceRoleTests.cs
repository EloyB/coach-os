using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NUnit.Framework;
using OrganizationSettingsEntity = CoachOS.Domain.Entities.OrganizationSettings;

namespace CoachOS.Tests.Identity;

/// <summary>
/// Toetst de extra-admin ("deputy admin") regels van <see cref="TrainerService"/> tegen een
/// echte (InMemory) DbContext: rol wijzigen, laatste-admin-bescherming, jezelf niet
/// deactiveren/verwijderen, uitnodigen als admin en de ledenlijst.
/// </summary>
[TestFixture]
public class TrainerServiceRoleTests
{
    private ApplicationDbContext _db = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<IOrganizationSettingsRepository> _settings = null!;
    private TrainerService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());

        _userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        _email = new Mock<IEmailService>();
        _settings = new Mock<IOrganizationSettingsRepository>();

        _sut = new TrainerService(
            _userManager.Object, _email.Object, _db, Mock.Of<IAuthService>(), _settings.Object, TimeProvider.System);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<OrganizationMembership> AddMemberAsync(
        UserRole role, bool active = true, Guid? orgId = null, string? name = null)
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@test.be",
            Email = $"{Guid.NewGuid():N}@test.be",
            FirstName = name ?? role.ToString(),
            LastName = "Test",
            IsActive = active,
        };
        _db.Users.Add(user);
        OrganizationMembership membership = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = orgId ?? OrgId,
            Role = role,
            IsActive = active,
            JoinedAt = DateTime.UtcNow,
        };
        _db.OrganizationMemberships.Add(membership);
        await _db.SaveChangesAsync();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        return membership;
    }

    // ── SetRole ──────────────────────────────────────────────────────────────

    [Test]
    public async Task SetRole_PromoteTrainer_BecomesAdminAndClearsHeadTrainerClubs()
    {
        await AddMemberAsync(UserRole.Admin);
        OrganizationMembership trainer = await AddMemberAsync(UserRole.Trainer);
        _db.HeadTrainerClubs.Add(new HeadTrainerClub
        {
            OrganizationMembershipId = trainer.Id, TennisClubId = Guid.NewGuid(),
        });
        await _db.SaveChangesAsync();

        Result result = await _sut.SetRoleAsync(trainer.UserId, OrgId, UserRole.Admin);

        result.IsSuccess.Should().BeTrue();
        OrganizationMembership saved = await _db.OrganizationMemberships.SingleAsync(m => m.Id == trainer.Id);
        saved.Role.Should().Be(UserRole.Admin);
        (await _db.HeadTrainerClubs.CountAsync(h => h.OrganizationMembershipId == trainer.Id)).Should().Be(0);
    }

    [Test]
    public async Task SetRole_DemoteDeputyWhileOwnerStaysAdmin_Succeeds()
    {
        await AddMemberAsync(UserRole.Admin);
        OrganizationMembership deputy = await AddMemberAsync(UserRole.Admin);

        Result result = await _sut.SetRoleAsync(deputy.UserId, OrgId, UserRole.Trainer);

        result.IsSuccess.Should().BeTrue();
        (await _db.OrganizationMemberships.SingleAsync(m => m.Id == deputy.Id)).Role.Should().Be(UserRole.Trainer);
    }

    [Test]
    public async Task SetRole_DemoteLastActiveAdmin_ReturnsConflict()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);

        Result result = await _sut.SetRoleAsync(owner.UserId, OrgId, UserRole.Trainer);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        (await _db.OrganizationMemberships.SingleAsync(m => m.Id == owner.Id)).Role.Should().Be(UserRole.Admin);
    }

    [Test]
    public async Task SetRole_OtherAdminIsOnlyPendingInvite_DoesNotCountAsActiveAdmin()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        await AddMemberAsync(UserRole.Admin, active: false); // uitnodiging nog niet geaccepteerd

        Result result = await _sut.SetRoleAsync(owner.UserId, OrgId, UserRole.Trainer);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task SetRole_MemberOfOtherOrganization_ReturnsNotFound()
    {
        await AddMemberAsync(UserRole.Admin);
        OrganizationMembership foreign = await AddMemberAsync(UserRole.Trainer, orgId: OtherOrgId);

        Result result = await _sut.SetRoleAsync(foreign.UserId, OrgId, UserRole.Admin);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        (await _db.OrganizationMemberships.SingleAsync(m => m.Id == foreign.Id)).Role.Should().Be(UserRole.Trainer);
    }

    [Test]
    public async Task SetRole_StudentMembership_IsNotChangeable()
    {
        await AddMemberAsync(UserRole.Admin);
        OrganizationMembership student = await AddMemberAsync(UserRole.Student);

        Result result = await _sut.SetRoleAsync(student.UserId, OrgId, UserRole.Admin);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    // ── Deactivate / Remove ──────────────────────────────────────────────────

    [Test]
    public async Task Deactivate_Self_ReturnsConflict()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        await AddMemberAsync(UserRole.Admin);

        Result result = await _sut.DeactivateAsync(owner.UserId, OrgId, owner.UserId);

        result.IsSuccess.Should().BeFalse();
        (await _db.OrganizationMemberships.SingleAsync(m => m.Id == owner.Id)).IsActive.Should().BeTrue();
    }

    [Test]
    public async Task Deactivate_LastActiveAdmin_ReturnsConflict()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        OrganizationMembership trainer = await AddMemberAsync(UserRole.Trainer);

        Result result = await _sut.DeactivateAsync(owner.UserId, OrgId, trainer.UserId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task Deactivate_DeputyByOwner_Succeeds()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        OrganizationMembership deputy = await AddMemberAsync(UserRole.Admin);

        Result result = await _sut.DeactivateAsync(deputy.UserId, OrgId, owner.UserId);

        result.IsSuccess.Should().BeTrue();
        (await _db.OrganizationMemberships.SingleAsync(m => m.Id == deputy.Id)).IsActive.Should().BeFalse();
    }

    [Test]
    public async Task Remove_DeputyByOwner_RemovesMembership()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        OrganizationMembership deputy = await AddMemberAsync(UserRole.Admin);

        Result result = await _sut.RemoveAsync(deputy.UserId, OrgId, owner.UserId);

        result.IsSuccess.Should().BeTrue();
        (await _db.OrganizationMemberships.AnyAsync(m => m.Id == deputy.Id)).Should().BeFalse();
    }

    [Test]
    public async Task Remove_LastActiveAdmin_ReturnsConflict()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        OrganizationMembership trainer = await AddMemberAsync(UserRole.Trainer);

        Result result = await _sut.RemoveAsync(owner.UserId, OrgId, trainer.UserId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        (await _db.OrganizationMemberships.AnyAsync(m => m.Id == owner.Id)).Should().BeTrue();
    }

    [Test]
    public async Task Remove_Trainer_StillWorksWithoutAdminGuard()
    {
        OrganizationMembership owner = await AddMemberAsync(UserRole.Admin);
        OrganizationMembership trainer = await AddMemberAsync(UserRole.Trainer);

        Result result = await _sut.RemoveAsync(trainer.UserId, OrgId, owner.UserId);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Invite ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Invite_AsAdmin_NewUser_CreatesInactiveAdminMembership()
    {
        _userManager.Setup(m => m.FindByEmailAsync("deputy@test.be")).ReturnsAsync((ApplicationUser?)null);

        Result<Guid> result = await _sut.InviteAsync(
            OrgId, "Dana", "Deputy", "deputy@test.be", "https://app.test", asAdmin: true);

        result.IsSuccess.Should().BeTrue();
        OrganizationMembership membership = await _db.OrganizationMemberships.SingleAsync(m => m.UserId == result.Value);
        membership.Role.Should().Be(UserRole.Admin);
        membership.IsActive.Should().BeFalse(); // actief na het accepteren van de uitnodiging
    }

    [Test]
    public async Task Invite_DefaultsToTrainer()
    {
        _userManager.Setup(m => m.FindByEmailAsync("nieuw@test.be")).ReturnsAsync((ApplicationUser?)null);

        Result<Guid> result = await _sut.InviteAsync(OrgId, "Nina", "Nieuw", "nieuw@test.be", "https://app.test");

        result.IsSuccess.Should().BeTrue();
        (await _db.OrganizationMemberships.SingleAsync(m => m.UserId == result.Value)).Role.Should().Be(UserRole.Trainer);
    }

    [Test]
    public async Task Invite_AsAdmin_ExistingUserOfOtherOrganization_GetsActiveAdminMembership()
    {
        OrganizationMembership other = await AddMemberAsync(UserRole.Trainer, orgId: OtherOrgId);
        ApplicationUser existing = await _db.Users.SingleAsync(u => u.Id == other.UserId);
        _userManager.Setup(m => m.FindByEmailAsync(existing.Email!)).ReturnsAsync(existing);

        Result<Guid> result = await _sut.InviteAsync(
            OrgId, "Eva", "Extern", existing.Email!, "https://app.test", asAdmin: true);

        result.IsSuccess.Should().BeTrue();
        OrganizationMembership membership = await _db.OrganizationMemberships
            .SingleAsync(m => m.UserId == existing.Id && m.OrganizationId == OrgId);
        membership.Role.Should().Be(UserRole.Admin);
        membership.IsActive.Should().BeTrue();
    }

    // ── Ledenlijst ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetTrainers_AdminsActAsTrainersOff_HidesAdminsByDefault()
    {
        _settings.Setup(s => s.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettingsEntity { OrganizationId = OrgId, AdminsActAsTrainers = false });
        await AddMemberAsync(UserRole.Admin);
        await AddMemberAsync(UserRole.Trainer);

        Result<List<TrainerDto>> result = await _sut.GetTrainersAsync(OrgId);

        result.Value!.Should().ContainSingle().Which.Role.Should().Be("Trainer");
    }

    [Test]
    public async Task GetTrainers_IncludeAllAdmins_ListsAdminsWithRoleEvenWhenSettingIsOff()
    {
        _settings.Setup(s => s.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettingsEntity { OrganizationId = OrgId, AdminsActAsTrainers = false });
        await AddMemberAsync(UserRole.Admin);
        await AddMemberAsync(UserRole.Trainer);
        await AddMemberAsync(UserRole.Student); // studenten horen nooit in de ledenlijst

        Result<List<TrainerDto>> result = await _sut.GetTrainersAsync(OrgId, includeAllAdmins: true);

        result.Value!.Select(t => t.Role).Should().BeEquivalentTo("Admin", "Trainer");
    }
}
