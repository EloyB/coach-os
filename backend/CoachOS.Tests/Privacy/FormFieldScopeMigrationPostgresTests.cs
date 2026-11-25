using AwesomeAssertions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Opt-in (zet <c>COACHOS_TEST_PG</c>): bewijst de migratie <c>AddFormFieldScope</c> op echte data. Bestaande vragen
/// worden "algemeen" (de leider beantwoordde ze tot nu toe), nieuwe vragen zijn standaard "per speler", en de weg terug
/// laat de kolom weer weg.
/// </summary>
[TestFixture]
public class FormFieldScopeMigrationPostgresTests
{
    private const string PreviousMigration = "20261121142455_AddCampDateOfBirthAndActiveSince";
    private const string ThisMigration = "20261125202912_AddFormFieldScope";

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
        _database = "fix411_migration_" + Guid.NewGuid().ToString("N");
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

    private static async Task<int?> ScopeOfAsync(NpgsqlConnection connection, Guid fieldId)
    {
        await using NpgsqlCommand command = new("SELECT \"Scope\" FROM \"FormFields\" WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", fieldId);
        object? value = await command.ExecuteScalarAsync();
        return value as int?;
    }

    [Test]
    public async Task ExistingQuestions_BecomeGeneral_NewOnesDefaultToPerParticipant_AndTheWayBackDropsTheColumn()
    {
        await MigrateToAsync(PreviousMigration);

        Guid orgId = Guid.NewGuid();
        Guid clubId = Guid.NewGuid();
        Guid seriesId = Guid.NewGuid();
        Guid formId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        await using NpgsqlConnection connection = new(_connection);
        await connection.OpenAsync();
        await InsertAsync(connection, "Organizations", new() { ["Id"] = orgId });
        await InsertAsync(connection, "TennisClubs", new() { ["Id"] = clubId, ["OrganizationId"] = orgId });
        await InsertAsync(connection, "LessonSeries", new() { ["Id"] = seriesId, ["OrganizationId"] = orgId, ["TennisClubId"] = clubId });
        await InsertAsync(connection, "EnrollmentForms", new() { ["Id"] = formId, ["OrganizationId"] = orgId, ["LessonSerieId"] = seriesId });
        await InsertAsync(connection, "FormFields", new() { ["Id"] = first, ["EnrollmentFormId"] = formId, ["Label"] = "Ervaring", ["Type"] = 1, ["Order"] = 0 });
        await InsertAsync(connection, "FormFields", new() { ["Id"] = second, ["EnrollmentFormId"] = formId, ["Label"] = "Shirtmaat", ["Type"] = 1, ["Order"] = 1 });

        await MigrateToAsync(ThisMigration);

        (await ScopeOfAsync(connection, first)).Should().Be(1, "bestaande vragen blijven door de leider beantwoord: algemeen");
        (await ScopeOfAsync(connection, second)).Should().Be(1);

        // Een vraag die zonder bereik wordt ingevoegd, is "per speler".
        Guid created = Guid.NewGuid();
        await InsertAsync(connection, "FormFields", new() { ["Id"] = created, ["EnrollmentFormId"] = formId, ["Label"] = "Nieuw", ["Type"] = 1, ["Order"] = 2 });
        (await ScopeOfAsync(connection, created)).Should().Be(0);

        await MigrateToAsync(PreviousMigration);

        await using NpgsqlCommand column = new(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'FormFields' AND column_name = 'Scope'", connection);
        ((long)(await column.ExecuteScalarAsync())!).Should().Be(0);
    }
}
