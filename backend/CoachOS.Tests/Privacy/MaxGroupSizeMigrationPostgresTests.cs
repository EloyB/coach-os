using AwesomeAssertions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Opt-in (zet <c>COACHOS_TEST_PG</c>): bewijst de dataconversie van de migratie
/// <c>ReplaceIsOpenToGroupingWithMaxGroupSize</c> op echte data. Migreert een wegwerpdatabase tot vlak vóór de
/// migratie, zet inschrijvingen met de oude vlag erin, migreert door en controleert dat iedereen het maximum van de
/// reeks krijgt (nooit lager dan de eigen groep), en de weg terug.
/// </summary>
[TestFixture]
public class MaxGroupSizeMigrationPostgresTests
{
    private const string PreviousMigration = "20261121142455_AddCampDateOfBirthAndActiveSince";
    private const string ThisMigration = "20261124200719_ReplaceIsOpenToGroupingWithMaxGroupSize";

    private string? _admin;
    private string? _database;
    private string _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private int _unique;

    private sealed class NoTenant : CoachOS.Application.Abstractions.ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG is vereist voor PostgreSQL-integratietests");
        _database = "fix387_migration_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        _connection = new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connection).Options;
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

    private async Task MigrateToAsync(string target)
    {
        await using ApplicationDbContext db = new(_options, new NoTenant());
        await db.GetService<IMigrator>().MigrateAsync(target);
    }

    /// <summary>
    /// Voegt een rij in met eigen waarden; alle andere verplichte kolommen zonder standaardwaarde krijgen een
    /// onschuldige waarde op basis van hun type. Zo blijft de test los van kolommen die er later bijkomen.
    /// </summary>
    private async Task InsertAsync(NpgsqlConnection connection, string table, Dictionary<string, object?> values)
    {
        List<(string Name, string Type)> required = [];
        await using (NpgsqlCommand schema = new(
            "SELECT column_name, data_type FROM information_schema.columns " +
            "WHERE table_name = @t AND is_nullable = 'NO' AND column_default IS NULL AND is_identity = 'NO'", connection))
        {
            schema.Parameters.AddWithValue("t", table);
            await using NpgsqlDataReader reader = await schema.ExecuteReaderAsync();
            while (await reader.ReadAsync()) required.Add((reader.GetString(0), reader.GetString(1)));
        }

        Dictionary<string, object?> row = new(values);
        foreach ((string name, string type) in required.Where(r => !row.ContainsKey(r.Name)))
        {
            _unique++;
            row[name] = type switch
            {
                "uuid" => Guid.NewGuid(),
                "text" or "character varying" => $"x{_unique}",
                "integer" or "smallint" => 0,
                "bigint" => 0L,
                "boolean" => false,
                "numeric" => 0m,
                "timestamp with time zone" => DateTime.UtcNow,
                "timestamp without time zone" => DateTime.UtcNow,
                "date" => DateOnly.FromDateTime(DateTime.UtcNow),
                "time without time zone" => new TimeOnly(10, 0),
                "jsonb" or "json" => "{}",
                _ => throw new InvalidOperationException($"Onbekend kolomtype {type} voor {table}.{name}"),
            };
        }

        string columns = string.Join(", ", row.Keys.Select(k => $"\"{k}\""));
        string parameters = string.Join(", ", row.Keys.Select((_, i) => $"@p{i}"));
        await using NpgsqlCommand insert = new($"INSERT INTO \"{table}\" ({columns}) VALUES ({parameters})", connection);
        int index = 0;
        foreach (object? value in row.Values)
            insert.Parameters.AddWithValue($"p{index++}", value ?? DBNull.Value);
        await insert.ExecuteNonQueryAsync();
    }

    private async Task<Guid> SeedSeriesAsync(NpgsqlConnection connection, Guid orgId, Guid clubId, params int[] slotCapacities)
    {
        Guid seriesId = Guid.NewGuid();
        await InsertAsync(connection, "LessonSeries", new() { ["Id"] = seriesId, ["OrganizationId"] = orgId, ["TennisClubId"] = clubId });
        foreach (int capacity in slotCapacities)
            await InsertAsync(connection, "WeeklyTemplateEntries", new() { ["Id"] = Guid.NewGuid(), ["LessonSerieId"] = seriesId, ["MaxStudents"] = capacity });
        return seriesId;
    }

    private async Task<Guid> SeedEnrollmentAsync(
        NpgsqlConnection connection, Guid orgId, Guid seriesId, bool open, int status = 1, Guid? groupId = null)
    {
        Guid id = Guid.NewGuid();
        await InsertAsync(connection, "Enrollments", new()
        {
            ["Id"] = id, ["OrganizationId"] = orgId, ["LessonSerieId"] = seriesId, ["IsOpenToGrouping"] = open,
            ["Status"] = status, ["EnrollmentGroupId"] = groupId,
        });
        return id;
    }

    /// <summary>Een groep: de leider wordt eerst ingevoegd (de groep verwijst naar hem), daarna de groep, dan de leden.</summary>
    private async Task<(Guid GroupId, List<Guid> Members)> SeedGroupAsync(
        NpgsqlConnection connection, Guid orgId, Guid seriesId, bool leaderOpen, params int[] memberStatuses)
    {
        Guid leaderId = await SeedEnrollmentAsync(connection, orgId, seriesId, leaderOpen);
        Guid groupId = Guid.NewGuid();
        await InsertAsync(connection, "EnrollmentGroups", new()
        {
            ["Id"] = groupId, ["OrganizationId"] = orgId, ["LessonSerieId"] = seriesId, ["LeaderEnrollmentId"] = leaderId,
        });
        await using (NpgsqlCommand link = new("UPDATE \"Enrollments\" SET \"EnrollmentGroupId\" = @g WHERE \"Id\" = @id", connection))
        {
            link.Parameters.AddWithValue("g", groupId);
            link.Parameters.AddWithValue("id", leaderId);
            await link.ExecuteNonQueryAsync();
        }

        List<Guid> members = [leaderId];
        foreach (int status in memberStatuses)
            members.Add(await SeedEnrollmentAsync(connection, orgId, seriesId, open: false, status, groupId));
        return (groupId, members);
    }

    private static async Task<int> MaxOfAsync(NpgsqlConnection connection, Guid enrollmentId)
    {
        await using NpgsqlCommand command = new("SELECT \"MaxGroupSize\" FROM \"Enrollments\" WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", enrollmentId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> OpenOfAsync(NpgsqlConnection connection, Guid enrollmentId)
    {
        await using NpgsqlCommand command = new("SELECT \"IsOpenToGrouping\" FROM \"Enrollments\" WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", enrollmentId);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    [Test]
    public async Task ExistingEnrollments_GetTheSeriesMaximum_AndTheWayBackDerivesTheFlag()
    {
        await MigrateToAsync(PreviousMigration);

        Guid orgId = Guid.NewGuid();
        Guid clubId = Guid.NewGuid();
        await using NpgsqlConnection connection = new(_connection);
        await connection.OpenAsync();
        await InsertAsync(connection, "Organizations", new() { ["Id"] = orgId });
        await InsertAsync(connection, "TennisClubs", new() { ["Id"] = clubId, ["OrganizationId"] = orgId });

        Guid seriesThree = await SeedSeriesAsync(connection, orgId, clubId, 3, 2);   // reeksmaximum 3
        Guid seriesNone = await SeedSeriesAsync(connection, orgId, clubId);          // geen weekslots: 4
        Guid seriesOne = await SeedSeriesAsync(connection, orgId, clubId, 1);        // privélessen: 1
        Guid seriesBig = await SeedSeriesAsync(connection, orgId, clubId, 10);       // begrensd tot 4

        Guid closedSolo = await SeedEnrollmentAsync(connection, orgId, seriesThree, open: false);
        Guid openSolo = await SeedEnrollmentAsync(connection, orgId, seriesThree, open: true);
        Guid closedSoloNoSlots = await SeedEnrollmentAsync(connection, orgId, seriesNone, open: false);
        Guid openSoloNoSlots = await SeedEnrollmentAsync(connection, orgId, seriesNone, open: true);
        Guid openSoloPrivate = await SeedEnrollmentAsync(connection, orgId, seriesOne, open: true);
        Guid openSoloBig = await SeedEnrollmentAsync(connection, orgId, seriesBig, open: true);
        (_, List<Guid> closedGroup) = await SeedGroupAsync(connection, orgId, seriesThree, leaderOpen: false, 1);
        (_, List<Guid> openGroup) = await SeedGroupAsync(connection, orgId, seriesThree, leaderOpen: true, 1);
        (_, List<Guid> groupOfThreeInPrivateSeries) = await SeedGroupAsync(connection, orgId, seriesOne, leaderOpen: true, 1, 1);
        (_, List<Guid> groupWithCancelledMember) = await SeedGroupAsync(connection, orgId, seriesThree, leaderOpen: false, 3);

        await MigrateToAsync(ThisMigration);

        // Iedereen krijgt het maximum van de reeks, ook wie "niet open voor groepering" was.
        (await MaxOfAsync(connection, closedSolo)).Should().Be(3, "reeksmaximum 3, ook voor een gesloten solo");
        (await MaxOfAsync(connection, openSolo)).Should().Be(3, "het maximum van de reeks");
        (await MaxOfAsync(connection, closedSoloNoSlots)).Should().Be(4, "reeks zonder weekslots: 4, ook voor een gesloten solo");
        (await MaxOfAsync(connection, openSoloNoSlots)).Should().Be(4, "reeks zonder weekslots: 4");
        (await MaxOfAsync(connection, openSoloPrivate)).Should().Be(1, "de reeks laat maximum 1 toe");
        (await MaxOfAsync(connection, openSoloBig)).Should().Be(4, "begrensd tot 4");
        foreach (Guid id in closedGroup) (await MaxOfAsync(connection, id)).Should().Be(3, "reeksmaximum, voor iedereen in de groep");
        foreach (Guid id in openGroup) (await MaxOfAsync(connection, id)).Should().Be(3, "reeksmaximum, voor iedereen in de groep");
        foreach (Guid id in groupOfThreeInPrivateSeries) (await MaxOfAsync(connection, id)).Should().Be(3, "nooit lager dan de eigen groep");
        foreach (Guid id in groupWithCancelledMember) (await MaxOfAsync(connection, id)).Should().Be(3, "een geannuleerd lid telt niet mee voor de groepsgrootte, het reeksmaximum geldt");

        // De weg terug: open = het maximum is groter dan de eigen groep; leden krijgen de vlag niet. De oorspronkelijke
        // vlaggen zijn niet te herstellen, want alles stond op het reeksmaximum.
        await MigrateToAsync(PreviousMigration);

        (await OpenOfAsync(connection, closedSolo)).Should().BeTrue();
        (await OpenOfAsync(connection, openSolo)).Should().BeTrue();
        (await OpenOfAsync(connection, openSoloNoSlots)).Should().BeTrue();
        (await OpenOfAsync(connection, openSoloPrivate)).Should().BeFalse("maximum 1 is niet groter dan de eigen groep");
        (await OpenOfAsync(connection, closedGroup[0])).Should().BeTrue("de leider: 3 is groter dan de groep van 2");
        (await OpenOfAsync(connection, closedGroup[1])).Should().BeFalse("een lid had de vlag nooit");
        (await OpenOfAsync(connection, groupOfThreeInPrivateSeries[0])).Should().BeFalse("maximum 3 is gelijk aan de groep van 3");
        (await OpenOfAsync(connection, groupWithCancelledMember[0])).Should().BeTrue();
    }
}
