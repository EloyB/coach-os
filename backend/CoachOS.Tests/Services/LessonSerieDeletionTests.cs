using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture, NonParallelizable]
public class LessonSerieDeletionTests
{
    private string? _admin;
    private string? _database;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private sealed class Tenant(Guid organizationId) : ITenantContext
    {
        public Guid OrganizationId => organizationId;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => organizationId != Guid.Empty;
    }

    private ApplicationDbContext Db(Guid organizationId = default) => new(_options, new Tenant(organizationId));

    private static LessonSerieService Service(ApplicationDbContext db) => new(
        new LessonSerieRepository(db), new LessonRepository(db, TimeProvider.System), Mock.Of<IEnrollmentRepository>(),
        Mock.Of<ITennisClubRepository>(), Mock.Of<IUserLookupService>(), Mock.Of<IEmailService>(),
        Mock.Of<IMollieConnectionRepository>(), Mock.Of<IScheduleAssignmentRepository>(),
        Mock.Of<ITimeSlotPreferenceRepository>(), Mock.Of<ILessonInvitationRepository>(),
        new UnitOfWork(db), TimeProvider.System, new ApplicationMapper(), NullLogger<LessonSerieService>.Instance);

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin))
            Assert.Ignore("COACHOS_TEST_PG niet gezet: PostgreSQL-integratietests overgeslagen.");

        _database = "series_deletion_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString).Options;
        await using ApplicationDbContext db = Db();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        if (_database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<LessonSerie> SeedSeriesAsync(bool withForm = true, bool withPrice = true)
    {
        Organization organization = new() { Id = Guid.NewGuid(), Name = "Synthetic deletion test", Email = $"{Guid.NewGuid():N}@example.test" };
        TennisClub club = new() { Organization = organization, Name = "Synthetic club" };
        LessonSerie series = new()
        {
            Organization = organization,
            TennisClub = club,
            Name = "Synthetic series",
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 12, 1),
            RegistrationDeadline = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        if (withForm)
        {
            series.EnrollmentForm = new EnrollmentForm
            {
                OrganizationId = organization.Id,
                Fields = [new FormField { Label = "Synthetic question" }],
            };
        }
        if (withPrice)
            series.Prices.Add(new LessonSeriePrice { Organization = organization, Label = "Standard", TotalPrice = 50m });
        WeeklyTemplateEntry entry = new() { StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) };
        series.WeeklyTemplate.Add(entry);
        series.Lessons.Add(new Lesson
        {
            Organization = organization,
            WeeklyTemplateEntry = entry,
            Date = new DateOnly(2026, 11, 24),
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
        });
        await using ApplicationDbContext db = Db();
        db.LessonSeries.Add(series);
        await db.SaveChangesAsync();
        return series;
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task DeleteAsync_RemovesFormFieldsAndPrices_WithoutTouchingOtherSeries(bool withForm, bool withPrice)
    {
        LessonSerie series = await SeedSeriesAsync(withForm, withPrice);
        LessonSerie other = await SeedSeriesAsync();
        await using (ApplicationDbContext db = Db(series.OrganizationId))
        {
            Result result = await Service(db).DeleteAsync(series.Id, series.OrganizationId);
            result.IsSuccess.Should().BeTrue();
        }

        await using ApplicationDbContext verify = Db();
        (await verify.LessonSeries.AnyAsync(s => s.Id == series.Id)).Should().BeFalse();
        (await verify.EnrollmentForms.AnyAsync(f => f.LessonSerieId == series.Id)).Should().BeFalse();
        if (series.EnrollmentForm is not null)
            (await verify.FormFields.AnyAsync(f => f.EnrollmentFormId == series.EnrollmentForm.Id)).Should().BeFalse();
        (await verify.LessonSeriePrices.AnyAsync(p => p.LessonSerieId == series.Id)).Should().BeFalse();
        (await verify.Lessons.AnyAsync(l => l.LessonSerieId == series.Id)).Should().BeFalse();
        (await verify.WeeklyTemplateEntries.AnyAsync(w => w.LessonSerieId == series.Id)).Should().BeFalse();
        (await verify.LessonSeries.AnyAsync(s => s.Id == other.Id)).Should().BeTrue();
        (await verify.FormFields.AnyAsync(f => f.EnrollmentFormId == other.EnrollmentForm!.Id)).Should().BeTrue();
        (await verify.LessonSeriePrices.AnyAsync(p => p.LessonSerieId == other.Id)).Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_WithEnrollment_PreservesConfiguration()
    {
        LessonSerie series = await SeedSeriesAsync();
        await using (ApplicationDbContext seed = Db(series.OrganizationId))
        {
            seed.Enrollments.Add(new Enrollment
            {
                OrganizationId = series.OrganizationId,
                LessonSerieId = series.Id,
                StudentName = "Synthetic student",
                ContactEmail = "student@example.test",
                SelectedPriceOptionId = series.Prices.Single().Id,
                FormResponses = [new FormResponse { FormFieldId = series.EnrollmentForm!.Fields.Single().Id, Value = "Synthetic answer" }],
            });
            await seed.SaveChangesAsync();
        }
        await using (ApplicationDbContext db = Db(series.OrganizationId))
        {
            Result result = await Service(db).DeleteAsync(series.Id, series.OrganizationId);
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        }
        await using ApplicationDbContext verify = Db();
        (await verify.LessonSeries.AnyAsync(s => s.Id == series.Id)).Should().BeTrue();
        (await verify.FormResponses.AnyAsync(r => r.FormFieldId == series.EnrollmentForm!.Fields.Single().Id)).Should().BeTrue();
        (await verify.LessonSeriePrices.AnyAsync(p => p.LessonSerieId == series.Id)).Should().BeTrue();
        (await verify.Lessons.AnyAsync(l => l.LessonSerieId == series.Id)).Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_WithDifferentOrganization_ReturnsNotFoundAndPreservesConfiguration()
    {
        LessonSerie series = await SeedSeriesAsync();
        // No ambient tenant filter: the repository must still enforce the organization argument.
        await using (ApplicationDbContext db = Db())
        {
            Result result = await Service(db).DeleteAsync(series.Id, Guid.NewGuid());
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        }
        await using ApplicationDbContext verify = Db();
        (await verify.LessonSeries.AnyAsync(s => s.Id == series.Id)).Should().BeTrue();
        (await verify.FormFields.AnyAsync(f => f.EnrollmentFormId == series.EnrollmentForm!.Id)).Should().BeTrue();
        (await verify.LessonSeriePrices.AnyAsync(p => p.LessonSerieId == series.Id)).Should().BeTrue();
    }
}
