using System.Data;
using System.IO.Compression;
using System.Text;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Export;
using CoachOS.Application.Privacy;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Tests.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class ClubExportTests
{
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero));

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [Test]
    public void EscapeCsv_FormulaLikeValues_ArePrefixedAndQuoted()
    {
        ClubExportArchive.EscapeCsv("=HYPERLINK(\"x\")").Should().Be("\"'=HYPERLINK(\"\"x\"\")\"");
        ClubExportArchive.EscapeCsv("  +32 470").Should().Be("\"'  +32 470\"");
        ClubExportArchive.EscapeCsv("-1").Should().Be("\"'-1\"");
        ClubExportArchive.EscapeCsv("@cmd").Should().Be("\"'@cmd\"");
        ClubExportArchive.EscapeCsv("\tx").Should().Be("\"'\tx\"");
        ClubExportArchive.EscapeCsv("Jan Peeters").Should().Be("\"Jan Peeters\"");
    }

    [Test]
    public void Build_WritesCsvJsonAndManifest()
    {
        ExportTable table = new("enrollments", ["Id", "StudentName"],
            [new object?[] { Guid.Empty, "=1+1" }]);

        byte[] zip = ClubExportArchive.Build(Guid.NewGuid(), [table], Clock.GetUtcNow().UtcDateTime);

        using ZipArchive archive = new(new MemoryStream(zip));
        archive.Entries.Select(e => e.FullName).Should().BeEquivalentTo("enrollments.csv", "enrollments.json", "manifest.json");
        string csv = Read(archive, "enrollments.csv");
        csv.Should().StartWith("﻿\"Id\",\"StudentName\"");
        csv.Should().Contain("\"'=1+1\"");
        Read(archive, "enrollments.json").Should().Contain("\"StudentName\":\"=1\\u002B1\"");
    }

    [Test]
    public async Task ExportAsync_NotAnActiveConfirmedAdmin_Forbidden()
    {
        Mock<IOrganizationExportRepository> repo = new();
        Mock<IUnitOfWork> unitOfWork = new();
        ClubExportService sut = new(repo.Object, unitOfWork.Object, Clock, NullLogger<ClubExportService>.Instance);

        Result<ExportFileDto> result = await sut.ExportAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbidden);
        repo.Verify(r => r.GetTablesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExportAsync_Admin_ReturnsZipFromOneSnapshot()
    {
        Guid org = Guid.NewGuid();
        Guid user = Guid.NewGuid();
        Mock<IOrganizationExportRepository> repo = new();
        repo.Setup(r => r.IsActiveConfirmedAdminAsync(org, user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetTablesAsync(org, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExportTable("clubs", ["Id"], [])]);
        Mock<IUnitOfWork> unitOfWork = new();
        ClubExportService sut = new(repo.Object, unitOfWork.Object, Clock, NullLogger<ClubExportService>.Instance);

        Result<ExportFileDto> result = await sut.ExportAsync(org, user);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("application/zip");
        unitOfWork.Verify(u => u.BeginTransactionAsync(IsolationLevel.RepeatableRead, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Repository_ExportsOnlyOwnOrganization_AndAllColumnsExist()
    {
        await using ApplicationDbContext db = new(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new NoTenantContext());
        (Guid ownOrg, Guid admin) = Seed(db, "TC Eigen");
        (Guid otherOrg, _) = Seed(db, "TC Ander");
        await db.SaveChangesAsync();
        OrganizationExportRepository repo = new(db);

        IReadOnlyList<ExportTable> tables = await repo.GetTablesAsync(ownOrg);

        tables.Should().HaveCountGreaterThan(30);
        tables.Single(t => t.Name == "enrollments").Rows.Should().ContainSingle();
        tables.Single(t => t.Name == "contacts").Columns.Should().NotContain(c => c.Contains("Password") || c.Contains("Token") || c.Contains("Stamp"));
        tables.SelectMany(t => t.Rows).SelectMany(r => r).Should().NotContain(otherOrg);
        (await repo.IsActiveConfirmedAdminAsync(ownOrg, admin)).Should().BeTrue();
        (await repo.IsActiveConfirmedAdminAsync(otherOrg, admin)).Should().BeFalse();
    }

    private static (Guid OrgId, Guid AdminId) Seed(ApplicationDbContext db, string name)
    {
        Organization org = new() { Id = Guid.NewGuid(), Name = name, Email = $"{Guid.NewGuid():N}@test.be" };
        ApplicationUser admin = new()
        {
            Id = Guid.NewGuid(), UserName = $"{Guid.NewGuid():N}@test.be", Email = "admin@test.be",
            FirstName = "A", LastName = "B", EmailConfirmed = true, PasswordHash = "geheim"
        };
        db.Organizations.Add(org);
        db.Users.Add(admin);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, UserId = admin.Id, Role = UserRole.Admin
        });
        db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), OrganizationId = org.Id, StudentName = "=cmd", ContactEmail = "ouder@test.be"
        });
        return (org.Id, admin.Id);
    }

    private static string Read(ZipArchive archive, string name)
    {
        using StreamReader reader = new(archive.GetEntry(name)!.Open(), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false);
        return reader.ReadToEnd();
    }
}
