using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Karakteriseringstests (#171) voor <see cref="EnrollmentService.SubmitEnrollmentAsync"/>.
/// Elk scenario legt het volledige waarneembare gedrag vast: de volgorde van alle aanroepen op
/// repositories, unit of work en gebruikerslookup, de inhoud van elke aangemaakte entiteit
/// (incl. outbox-payloads), het resultaat en de logregels. Het transcript wordt vergeleken met
/// een goedgekeurd bestand onder <c>Characterization/SubmitEnrollment/</c>.
///
/// De goedgekeurde bestanden zijn opgenomen tegen de code VÓÓR de refactor. Een refactor die het
/// gedrag niet wijzigt, laat ze ongewijzigd. Opnieuw genereren kan alleen bewust, met
/// <c>UPDATE_GOLDEN=1</c>.
/// </summary>
[TestFixture]
public partial class SubmitEnrollmentCharacterizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid OrgId = new("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid SeriesId = new("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid TrainerId = new("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid FieldRequiredId = new("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid FieldChoiceId = new("00000000-0000-0000-0000-0000000000d2");
    private static readonly Guid UnknownFieldId = new("00000000-0000-0000-0000-0000000000d9");
    private static readonly Guid SlotMonId = new("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid SlotTueId = new("00000000-0000-0000-0000-0000000000e2");
    private static readonly Guid SlotWedId = new("00000000-0000-0000-0000-0000000000e3");
    private static readonly Guid PriceOptionId = new("00000000-0000-0000-0000-0000000000f1");

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Simuleert een PostgreSQL unique violation (SQLSTATE 23505) zoals Npgsql die gooit.</summary>
    private sealed class FakePostgresException(string sqlState) : Exception("duplicate key")
    {
        public string SqlState { get; } = sqlState;
    }

    /// <summary>Alles wat een scenario kan instellen: de wereld waarin de submit draait.</summary>
    public sealed class Scenario
    {
        public LessonSerie? Series { get; set; }
        public EnrollmentForm? Form { get; set; }
        public OrganizationSettings? OrgSettings { get; set; }
        public SubmitEnrollmentRequest Request { get; set; } = new();
        public int ActiveCount { get; set; }
        public int ExistingGroupCount { get; set; }
        public HashSet<string> DuplicateNames { get; set; } = [];
        public (string FullName, string Email)? Trainer { get; set; } = ("Jan Peeters", "jan@coach.be");
        public bool TrainerLookupThrows { get; set; }

        /// <summary>Gooit bij de n-de aanroep van <c>uow.SaveChangesAsync</c> (1-gebaseerd).</summary>
        public int? ThrowOnSaveNumber { get; set; }
        public Exception? SaveException { get; set; }
        public Exception? CommitException { get; set; }
        public Exception? EnrollmentAddException { get; set; }
    }

    private static LessonSerie BaseSeries() => new()
    {
        Id = SeriesId,
        OrganizationId = OrgId,
        Name = "Beginners A",
        Price = 120m,
        StartDate = new DateOnly(2026, 9, 1),
        EndDate = new DateOnly(2026, 12, 1),
        RegistrationDeadline = Now.UtcDateTime.AddDays(30),
        MaxRegistrations = 20,
        IsActive = true,
        MinAge = 3,
        MaxAge = 99,
        MinimumAvailableSlots = 1,
        Lessons =
        [
            // Bewust niet op volgorde: de service kiest de eerste les op datum + starttijd met een trainer.
            new Lesson { Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = null,
                Date = new DateOnly(2026, 9, 1), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) },
            new Lesson { Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = TrainerId,
                Date = new DateOnly(2026, 9, 8), StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0) },
            new Lesson { Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = Guid.NewGuid(),
                Date = new DateOnly(2026, 9, 15), StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0) },
        ],
        WeeklyTemplate =
        [
            new WeeklyTemplateEntry { Id = SlotMonId, LessonSerieId = SeriesId, DayOfWeek = 1,
                StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0), MaxStudents = 4 },
            new WeeklyTemplateEntry { Id = SlotTueId, LessonSerieId = SeriesId, DayOfWeek = 2,
                StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0), MaxStudents = 4 },
            new WeeklyTemplateEntry { Id = SlotWedId, LessonSerieId = SeriesId, DayOfWeek = 3,
                StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0), MaxStudents = 4 },
        ],
    };

    private static EnrollmentForm FormWithTwoFields() => new()
    {
        Id = new Guid("00000000-0000-0000-0000-0000000000aa"),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        Fields =
        [
            new FormField { Id = FieldRequiredId, Label = "Ervaring", Type = FormFieldType.Text, IsRequired = true, Order = 0 },
            new FormField { Id = FieldChoiceId, Label = "Niveau", Type = FormFieldType.Text, IsRequired = false, Order = 1 },
        ],
    };

    private static List<TimeSlotPreferenceDto> Prefs(params (Guid Slot, int Preference)[] items)
        => items.Select(i => new TimeSlotPreferenceDto { WeeklyTemplateEntryId = i.Slot, Preference = i.Preference }).ToList();

    // ── Scenario's ────────────────────────────────────────────────────────────

    private static IEnumerable<TestCaseData> Scenarios()
    {
        // Succes
        yield return Case("solo_full", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            OrgSettings = new OrganizationSettings { YouthMaxAge = 12 },
            ActiveCount = 3,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Anna Peeters",
                StudentEmail = "  Anna@Test.BE ",
                StudentPhone = "0470 12 34 56",
                DateOfBirth = "2016-05-12",
                MaxGroupSize = 3,
                SelectedPriceOptionId = PriceOptionId,
                Responses =
                [
                    new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Gevorderd" },
                    new FormResponseValueDto { FormFieldId = FieldChoiceId, Value = "Niveau 3" },
                ],
                TimeSlotPreferences = Prefs((SlotMonId, 2), (SlotTueId, 1), (SlotWedId, 0)),
            },
        });

        yield return Case("solo_without_form_trainer_or_settings", () =>
        {
            LessonSerie series = BaseSeries();
            series.Lessons = [];
            series.WeeklyTemplate = [];
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Bram",
                    StudentEmail = "bram@test.be",
                    DateOfBirth = "",
                },
            };
        });

        yield return Case("solo_without_capacity_limit", () =>
        {
            LessonSerie series = BaseSeries();
            series.MaxRegistrations = null;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Carl",
                    StudentEmail = "carl@test.be",
                    DateOfBirth = "1985-01-31",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("solo_adult_trainer_lookup_returns_nothing_to_resolve", () => new Scenario
        {
            Series = BaseSeries(),
            Trainer = null,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Dirk",
                StudentEmail = "dirk@test.be",
                DateOfBirth = "1970-12-31",
                TimeSlotPreferences = Prefs((SlotTueId, 2)),
            },
        });

        yield return Case("solo_type_with_members_ignores_members", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Els",
                StudentEmail = "els@test.be",
                DateOfBirth = "1990-02-02",
                EnrollmentType = "solo",
                GroupMembers = [new GroupMemberDto { StudentName = "Genegeerd", DateOfBirth = "2000-01-01" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("group_full_with_shared_contact_emails", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            OrgSettings = new OrganizationSettings { YouthMaxAge = 17 },
            ActiveCount = 5,
            ExistingGroupCount = 27,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Fien (leider)",
                StudentEmail = "ouder@test.be",
                StudentPhone = "0470 00 00 01",
                DateOfBirth = "1980-03-03",
                EnrollmentType = "group",
                SelectedPriceOptionId = PriceOptionId,
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 2), (SlotTueId, 1)),
                GroupMembers =
                [
                    // Eigen adres.
                    new GroupMemberDto
                    {
                        StudentName = "Gert", StudentEmail = "  Gert@Test.BE ", StudentPhone = "0470 00 00 02",
                        DateOfBirth = "2010-04-04",
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Gert antwoord" }],
                    },
                    // Geen adres: valt terug op de leider.
                    new GroupMemberDto
                    {
                        StudentName = "Hilde", DateOfBirth = "2012-05-05",
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Hilde antwoord" }],
                    },
                    // Alleen spaties: telt als leeg, dus ook terug op de leider.
                    new GroupMemberDto
                    {
                        StudentName = "Ine", StudentEmail = "   ", DateOfBirth = "2014-06-06",
                        Responses =
                        [
                            new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Ine 1" },
                            new FormResponseValueDto { FormFieldId = FieldChoiceId, Value = "Ine 2" },
                        ],
                    },
                ],
            },
        });

        yield return Case("group_trainer_lookup_throws", () => new Scenario
        {
            Series = BaseSeries(),
            TrainerLookupThrows = true,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Jan Leider",
                StudentEmail = "leider@test.be",
                DateOfBirth = "1975-07-07",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotWedId, 1)),
                GroupMembers = [new GroupMemberDto { StudentName = "Kim", StudentEmail = "kim@test.be", DateOfBirth = "1976-08-08" }],
            },
        });

        yield return Case("group_with_empty_member_list_is_treated_as_solo_enrollment", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Lies",
                StudentEmail = "lies@test.be",
                DateOfBirth = "1995-09-09",
                EnrollmentType = "group",
                GroupMembers = [],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("group_first_group_name_is_a", () => new Scenario
        {
            Series = BaseSeries(),
            ExistingGroupCount = 0,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Mo",
                StudentEmail = "mo@test.be",
                DateOfBirth = "1995-09-09",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers = [new GroupMemberDto { StudentName = "Noor", DateOfBirth = "1996-10-10" }],
            },
        });

        // Weigeringen vóór de transactie
        yield return Case("fail_series_not_found", () => new Scenario
        {
            Series = null,
            Request = new SubmitEnrollmentRequest { StudentName = "X", StudentEmail = "x@test.be" },
        });

        yield return Case("fail_registration_deadline_passed", () =>
        {
            LessonSerie series = BaseSeries();
            series.RegistrationDeadline = Now.UtcDateTime.AddSeconds(-1);
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest { StudentName = "X", StudentEmail = "x@test.be" },
            };
        });

        yield return Case("ok_registration_deadline_exactly_now", () =>
        {
            LessonSerie series = BaseSeries();
            series.RegistrationDeadline = Now.UtcDateTime;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Op de Seconde",
                    StudentEmail = "seconde@test.be",
                    DateOfBirth = "1990-01-01",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("fail_required_form_field_missing", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Olaf",
                StudentEmail = "olaf@test.be",
                DateOfBirth = "1990-01-01",
                Responses = [new FormResponseValueDto { FormFieldId = FieldChoiceId, Value = "Enkel optioneel" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        // Antwoorden per groepslid (#411): elk lid beantwoordt dezelfde vragen als de leider.
        yield return Case("fail_group_member_missing_required_answer", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Ward",
                StudentEmail = "ward@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto
                    {
                        StudentName = "Xander", DateOfBirth = "2010-01-01",
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Xander antwoord" }],
                    },
                    new GroupMemberDto { StudentName = "Yvon", DateOfBirth = "2011-01-01" },
                ],
            },
        });

        yield return Case("fail_group_member_blank_answer_counts_as_missing", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Zita",
                StudentEmail = "zita@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto
                    {
                        StudentName = "Arne", DateOfBirth = "2010-01-01",
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "   " }],
                    },
                ],
            },
        });

        yield return Case("fail_group_member_unknown_form_field", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Bavo",
                StudentEmail = "bavo@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto
                    {
                        StudentName = "Cleo", DateOfBirth = "2010-01-01",
                        Responses =
                        [
                            new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Cleo antwoord" },
                            new FormResponseValueDto { FormFieldId = UnknownFieldId, Value = "Onbekend veld" },
                        ],
                    },
                ],
            },
        });

        yield return Case("fail_group_member_answer_without_any_form", () => new Scenario
        {
            Series = BaseSeries(),
            Form = null,
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Dries",
                StudentEmail = "dries@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto
                    {
                        StudentName = "Eline", DateOfBirth = "2010-01-01",
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Antwoord zonder formulier" }],
                    },
                ],
            },
        });

        yield return Case("fail_precedence_leader_answer_before_member_answer", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Fons",
                StudentEmail = "fons@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers = [new GroupMemberDto { StudentName = "Gaby", DateOfBirth = "2010-01-01" }],
            },
        });

        yield return Case("fail_precedence_member_answer_before_mode_error", () =>
        {
            LessonSerie series = BaseSeries();
            series.AllowGroupEnrollment = false;
            return new Scenario
            {
                Series = series,
                Form = FormWithTwoFields(),
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Hugo",
                    StudentEmail = "hugo@test.be",
                    DateOfBirth = "1980-01-01",
                    EnrollmentType = "group",
                    Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                    GroupMembers = [new GroupMemberDto { StudentName = "Ilse", DateOfBirth = "2010-01-01" }],
                },
            };
        });

        yield return Case("solo_type_does_not_validate_member_answers", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Jules",
                StudentEmail = "jules@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "solo",
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Solo antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers = [new GroupMemberDto { StudentName = "Genegeerd", DateOfBirth = "2010-01-01" }],
            },
        });

        yield return Case("group_member_optional_answers_may_be_omitted", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Kato",
                StudentEmail = "kato@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Leider antwoord" }],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto
                    {
                        StudentName = "Lore", DateOfBirth = "2010-01-01",
                        // Alleen het verplichte veld; het optionele "Niveau" blijft leeg.
                        Responses = [new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Lore antwoord" }],
                    },
                ],
            },
        });

        // Maximum aantal personen per tijdslot (#387)
        yield return Case("group_max_group_size_is_inherited_by_members", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Mira",
                StudentEmail = "mira@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                MaxGroupSize = 4,
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto { StudentName = "Nore", DateOfBirth = "2010-01-01" },
                    new GroupMemberDto { StudentName = "Oscar", DateOfBirth = "2011-01-01" },
                ],
            },
        });

        yield return Case("fail_max_group_size_lower_than_own_group_size", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Pol",
                StudentEmail = "pol@test.be",
                DateOfBirth = "1980-01-01",
                EnrollmentType = "group",
                MaxGroupSize = 2,
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto { StudentName = "Quin", DateOfBirth = "2010-01-01" },
                    new GroupMemberDto { StudentName = "Rosa", DateOfBirth = "2011-01-01" },
                ],
            },
        });

        yield return Case("fail_max_group_size_above_series_maximum", () =>
        {
            LessonSerie series = BaseSeries();
            foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate) slot.MaxStudents = 2;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Sam",
                    StudentEmail = "sam2@test.be",
                    DateOfBirth = "1980-01-01",
                    MaxGroupSize = 3,
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("ok_max_group_size_equals_series_maximum", () =>
        {
            LessonSerie series = BaseSeries();
            foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate) slot.MaxStudents = 2;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Tina",
                    StudentEmail = "tina@test.be",
                    DateOfBirth = "1980-01-01",
                    MaxGroupSize = 2,
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("fail_max_group_size_out_of_range", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Ulla",
                StudentEmail = "ulla@test.be",
                DateOfBirth = "1980-01-01",
                MaxGroupSize = 5,
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("ok_group_larger_than_series_maximum_is_not_blocked", () =>
        {
            LessonSerie series = BaseSeries();
            foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate) slot.MaxStudents = 2;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Vic",
                    StudentEmail = "vic@test.be",
                    DateOfBirth = "1980-01-01",
                    EnrollmentType = "group",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                    GroupMembers =
                    [
                        new GroupMemberDto { StudentName = "Wout", DateOfBirth = "2010-01-01" },
                        new GroupMemberDto { StudentName = "Xavi", DateOfBirth = "2011-01-01" },
                    ],
                },
            };
        });

        yield return Case("ok_series_without_weekly_template_allows_up_to_four", () =>
        {
            LessonSerie series = BaseSeries();
            series.WeeklyTemplate = [];
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Yara2",
                    StudentEmail = "yara2@test.be",
                    DateOfBirth = "1980-01-01",
                    MaxGroupSize = 4,
                },
            };
        });

        yield return Case("fail_precedence_minimum_slots_before_max_group_size", () => new Scenario
        {
            Series = BaseSeries(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Zeno",
                StudentEmail = "zeno@test.be",
                DateOfBirth = "1980-01-01",
                MaxGroupSize = 5,
            },
        });

        yield return Case("fail_unknown_form_field_is_rejected", () => new Scenario
        {
            Series = BaseSeries(),
            Form = FormWithTwoFields(),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Tom",
                StudentEmail = "tom@test.be",
                DateOfBirth = "1990-01-01",
                Responses =
                [
                    new FormResponseValueDto { FormFieldId = FieldRequiredId, Value = "Ja" },
                    new FormResponseValueDto { FormFieldId = UnknownFieldId, Value = "Onbekend veld" },
                ],
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("fail_group_not_allowed", () =>
        {
            LessonSerie series = BaseSeries();
            series.AllowGroupEnrollment = false;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Pia",
                    StudentEmail = "pia@test.be",
                    DateOfBirth = "1990-01-01",
                    EnrollmentType = "group",
                    GroupMembers = [new GroupMemberDto { StudentName = "Quinn", DateOfBirth = "1991-01-01" }],
                },
            };
        });

        yield return Case("fail_solo_not_allowed", () =>
        {
            LessonSerie series = BaseSeries();
            series.AllowSoloEnrollment = false;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Rik",
                    StudentEmail = "rik@test.be",
                    DateOfBirth = "1990-01-01",
                },
            };
        });

        yield return Case("fail_age_below_minimum_for_member", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinAge = 8;
            series.MaxAge = 14;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Sam",
                    StudentEmail = "sam@test.be",
                    DateOfBirth = "2015-01-01",
                    EnrollmentType = "group",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                    GroupMembers = [new GroupMemberDto { StudentName = "Tess", DateOfBirth = "2020-01-01" }],
                },
            };
        });

        yield return Case("fail_age_above_maximum_for_leader", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinAge = 8;
            series.MaxAge = 14;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Uma",
                    StudentEmail = "uma@test.be",
                    DateOfBirth = "1990-01-01",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("ok_age_unparseable_birth_date_is_not_blocked", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinAge = 8;
            series.MaxAge = 14;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Vera",
                    StudentEmail = "vera@test.be",
                    DateOfBirth = "geen-datum",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("fail_minimum_slots_not_met_unavailable_slots_do_not_count", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinimumAvailableSlots = 2;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Wim",
                    StudentEmail = "wim@test.be",
                    DateOfBirth = "1990-01-01",
                    TimeSlotPreferences = Prefs((SlotMonId, 1), (SlotTueId, 0), (UnknownFieldId, 2)),
                },
            };
        });

        yield return Case("fail_minimum_slots_required_is_capped_by_template_size", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinimumAvailableSlots = 10;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Xena",
                    StudentEmail = "xena@test.be",
                    DateOfBirth = "1990-01-01",
                    TimeSlotPreferences = Prefs((SlotMonId, 1), (SlotTueId, 2)),
                },
            };
        });

        // Weigeringen binnen de transactie
        yield return Case("fail_capacity_exceeded_by_group_size", () =>
        {
            LessonSerie series = BaseSeries();
            series.MaxRegistrations = 10;
            return new Scenario
            {
                Series = series,
                ActiveCount = 8,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Yara",
                    StudentEmail = "yara@test.be",
                    DateOfBirth = "1990-01-01",
                    EnrollmentType = "group",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                    GroupMembers =
                    [
                        new GroupMemberDto { StudentName = "Zoe", DateOfBirth = "1991-01-01" },
                        new GroupMemberDto { StudentName = "Aron", DateOfBirth = "1992-01-01" },
                    ],
                },
            };
        });

        yield return Case("ok_capacity_exactly_reached", () =>
        {
            LessonSerie series = BaseSeries();
            series.MaxRegistrations = 10;
            return new Scenario
            {
                Series = series,
                ActiveCount = 9,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Bo",
                    StudentEmail = "bo@test.be",
                    DateOfBirth = "1990-01-01",
                    TimeSlotPreferences = Prefs((SlotMonId, 1)),
                },
            };
        });

        yield return Case("fail_duplicate_member_stops_at_first_duplicate", () => new Scenario
        {
            Series = BaseSeries(),
            DuplicateNames = ["Cato", "Dana"],
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Cas",
                StudentEmail = "cas@test.be",
                DateOfBirth = "1990-01-01",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers =
                [
                    new GroupMemberDto { StudentName = "Cato", StudentEmail = "cato@test.be", DateOfBirth = "1991-01-01" },
                    new GroupMemberDto { StudentName = "Dana", StudentEmail = "dana@test.be", DateOfBirth = "1992-01-01" },
                ],
            },
        });

        yield return Case("fail_duplicate_leader", () => new Scenario
        {
            Series = BaseSeries(),
            DuplicateNames = ["Eva"],
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Eva",
                StudentEmail = "eva@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        // Voorrang tussen foutsoorten: formulier > inschrijfwijze > leeftijd > minimum tijdvakken.
        yield return Case("fail_precedence_form_error_before_mode_error", () =>
        {
            LessonSerie series = BaseSeries();
            series.AllowGroupEnrollment = false;
            return new Scenario
            {
                Series = series,
                Form = FormWithTwoFields(),
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Lou",
                    StudentEmail = "lou@test.be",
                    DateOfBirth = "1990-01-01",
                    EnrollmentType = "group",
                    GroupMembers = [new GroupMemberDto { StudentName = "Mats", DateOfBirth = "1991-01-01" }],
                },
            };
        });

        yield return Case("fail_precedence_mode_error_before_age_error", () =>
        {
            LessonSerie series = BaseSeries();
            series.AllowGroupEnrollment = false;
            series.MinAge = 8;
            series.MaxAge = 14;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Nils",
                    StudentEmail = "nils@test.be",
                    DateOfBirth = "1990-01-01",
                    EnrollmentType = "group",
                    GroupMembers = [new GroupMemberDto { StudentName = "Oona", DateOfBirth = "1991-01-01" }],
                },
            };
        });

        yield return Case("fail_precedence_age_error_before_minimum_slots", () =>
        {
            LessonSerie series = BaseSeries();
            series.MinAge = 8;
            series.MaxAge = 14;
            return new Scenario
            {
                Series = series,
                Request = new SubmitEnrollmentRequest
                {
                    StudentName = "Pien",
                    StudentEmail = "pien@test.be",
                    DateOfBirth = "1990-01-01",
                },
            };
        });

        yield return Case("fail_precedence_deadline_before_everything", () =>
        {
            LessonSerie series = BaseSeries();
            series.RegistrationDeadline = Now.UtcDateTime.AddDays(-1);
            series.AllowSoloEnrollment = false;
            return new Scenario
            {
                Series = series,
                Form = FormWithTwoFields(),
                Request = new SubmitEnrollmentRequest { StudentName = "Quint", StudentEmail = "quint@test.be" },
            };
        });

        yield return Case("fail_capacity_checked_before_duplicates", () => new Scenario
        {
            Series = BaseSeries(),
            ActiveCount = 20,
            DuplicateNames = ["Roos"],
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Roos",
                StudentEmail = "roos@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        // Fouten tijdens het opslaan
        yield return Case("fail_unique_violation_on_first_save", () => new Scenario
        {
            Series = BaseSeries(),
            ThrowOnSaveNumber = 1,
            SaveException = new InvalidOperationException("update failed", new FakePostgresException("23505")),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Finn",
                StudentEmail = "finn@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("fail_unexpected_error_on_group_save", () => new Scenario
        {
            Series = BaseSeries(),
            ThrowOnSaveNumber = 2,
            SaveException = new InvalidOperationException("boom"),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Gus",
                StudentEmail = "gus@test.be",
                DateOfBirth = "1990-01-01",
                EnrollmentType = "group",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
                GroupMembers = [new GroupMemberDto { StudentName = "Hannah", DateOfBirth = "1991-01-01" }],
            },
        });

        yield return Case("fail_unexpected_error_on_last_save_after_outbox", () => new Scenario
        {
            Series = BaseSeries(),
            ThrowOnSaveNumber = 3,
            SaveException = new InvalidOperationException("outbox boom"),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Ivo",
                StudentEmail = "ivo@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("fail_commit_throws", () => new Scenario
        {
            Series = BaseSeries(),
            CommitException = new InvalidOperationException("commit boom"),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Jade",
                StudentEmail = "jade@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });

        yield return Case("fail_enrollment_add_throws", () => new Scenario
        {
            Series = BaseSeries(),
            EnrollmentAddException = new InvalidOperationException("add boom"),
            Request = new SubmitEnrollmentRequest
            {
                StudentName = "Kobe",
                StudentEmail = "kobe@test.be",
                DateOfBirth = "1990-01-01",
                TimeSlotPreferences = Prefs((SlotMonId, 1)),
            },
        });
    }

    private static TestCaseData Case(string name, Func<Scenario> build)
        => new TestCaseData(name, build).SetName(name);

    // ── De test ───────────────────────────────────────────────────────────────

    [TestCaseSource(nameof(Scenarios))]
    public async Task Submit_behaviour_is_unchanged(string name, Func<Scenario> build)
    {
        Scenario scenario = build();
        List<string> log = [];
        // Alles wat aan de "context" is toegevoegd. Bij elke SaveChanges loggen we de toestand ervan,
        // zodat ook wijzigingen ná de Add (bv. de groep van de leider) deel van het gedrag zijn.
        List<object> tracked = [];
        long nextId = 1000;
        Guid NewId() => new($"00000000-0000-0000-0000-{++nextId:D12}");

        Mock<IEnrollmentRepository> enrollmentRepo = new();
        Mock<IEnrollmentFormRepository> formRepo = new();
        Mock<ILessonSerieRepository> seriesRepo = new();
        Mock<IEnrollmentGroupRepository> groupRepo = new();
        Mock<IPaymentRepository> paymentRepo = new();
        Mock<ITimeSlotPreferenceRepository> prefRepo = new();
        Mock<IOrganizationSettingsRepository> settingsRepo = new();
        Mock<IUserLookupService> userLookup = new();
        Mock<IEmailOutboxRepository> outboxRepo = new();
        Mock<ILessonSeriePriceRepository> priceRepo = new();
        Mock<IScheduleAssignmentRepository> assignmentRepo = new();
        Mock<IUnitOfWork> uow = new();
        Mock<ILogger<EnrollmentService>> logger = new();

        seriesRepo.Setup(r => r.GetByIdPublicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
            {
                log.Add($"series.GetByIdPublic({Fmt(id)})");
                return Task.FromResult(scenario.Series);
            });

        formRepo.Setup(r => r.GetBySeriesIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
            {
                log.Add($"form.GetBySeriesIdReadOnly({Fmt(id)})");
                return Task.FromResult(scenario.Form);
            });

        settingsRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
            {
                log.Add($"settings.GetByOrganizationReadOnly({Fmt(id)})");
                return Task.FromResult(scenario.OrgSettings);
            });

        userLookup.Setup(u => u.GetUserInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
            {
                log.Add($"userLookup.GetUserInfoById({Fmt(id)})");
                if (scenario.TrainerLookupThrows)
                    throw new InvalidOperationException("lookup kapot");
                return Task.FromResult(scenario.Trainer);
            });

        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .Returns((IsolationLevel level, CancellationToken _) =>
            {
                log.Add($"uow.BeginTransaction({level})");
                return Task.CompletedTask;
            });
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                log.Add("uow.BeginTransaction()");
                return Task.CompletedTask;
            });
        int saveCount = 0;
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                saveCount++;
                log.Add($"uow.SaveChanges#{saveCount}");
                foreach (object entity in tracked)
                    log.Add($"  state {Snapshot(entity)}");
                if (scenario.ThrowOnSaveNumber == saveCount && scenario.SaveException is not null)
                    throw scenario.SaveException;
                return Task.CompletedTask;
            });
        uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                log.Add("uow.CommitTransaction");
                if (scenario.CommitException is not null)
                    throw scenario.CommitException;
                return Task.CompletedTask;
            });
        uow.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                log.Add("uow.RollbackTransaction");
                return Task.CompletedTask;
            });

        enrollmentRepo.Setup(r => r.CountActiveBySeriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) =>
            {
                log.Add($"enrollments.CountActiveBySeries({Fmt(id)}) => {scenario.ActiveCount}");
                return Task.FromResult(scenario.ActiveCount);
            });
        enrollmentRepo.Setup(r => r.IsDuplicateParticipantAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, string email, string participant, DateOnly? dob, CancellationToken _) =>
            {
                bool duplicate = scenario.DuplicateNames.Contains(participant);
                log.Add($"enrollments.IsDuplicateParticipant({Fmt(id)}, '{email}', '{participant}', {dob?.ToString("yyyy-MM-dd") ?? "null"}) => {duplicate}");
                return Task.FromResult(duplicate);
            });
        enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Returns((Enrollment enrollment, CancellationToken _) =>
            {
                if (scenario.EnrollmentAddException is not null)
                {
                    log.Add("enrollments.Add => throws");
                    throw scenario.EnrollmentAddException;
                }

                // Simuleert de client-side sleutelgeneratie van EF, zodat de koppelingen tussen
                // leider, leden, groep en antwoorden in het transcript zichtbaar zijn.
                if (enrollment.Id == Guid.Empty) enrollment.Id = NewId();
                tracked.Add(enrollment);
                log.Add($"enrollments.Add {Snapshot(enrollment)}");
                return Task.CompletedTask;
            });
        enrollmentRepo.Setup(r => r.AddFormResponseAsync(It.IsAny<FormResponse>(), It.IsAny<CancellationToken>()))
            .Returns((FormResponse response, CancellationToken _) =>
            {
                if (response.Id == Guid.Empty) response.Id = NewId();
                tracked.Add(response);
                log.Add($"enrollments.AddFormResponse {Snapshot(response)}");
                return Task.CompletedTask;
            });

        groupRepo.Setup(r => r.CountBySeriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid seriesId, Guid orgId, CancellationToken _) =>
            {
                log.Add($"groups.CountBySeries({Fmt(seriesId)}, {Fmt(orgId)}) => {scenario.ExistingGroupCount}");
                return Task.FromResult(scenario.ExistingGroupCount);
            });
        groupRepo.Setup(r => r.AddAsync(It.IsAny<EnrollmentGroup>(), It.IsAny<CancellationToken>()))
            .Returns((EnrollmentGroup group, CancellationToken _) =>
            {
                if (group.Id == Guid.Empty) group.Id = NewId();
                tracked.Add(group);
                log.Add($"groups.Add {Snapshot(group)}");
                return Task.CompletedTask;
            });

        prefRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TimeSlotPreference>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<TimeSlotPreference> preferences, CancellationToken _) =>
            {
                List<TimeSlotPreference> items = preferences.ToList();
                log.Add($"preferences.AddRange [{string.Join(", ", items.Select(Snapshot))}]");
                return Task.CompletedTask;
            });

        outboxRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EmailOutboxMessage>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<EmailOutboxMessage> messages, CancellationToken _) =>
            {
                List<EmailOutboxMessage> items = messages.ToList();
                log.Add($"outbox.AddRange [{string.Join(", ", items.Select(Snapshot))}]");
                return Task.CompletedTask;
            });

        EnrollmentService service = new(
            enrollmentRepo.Object, formRepo.Object, seriesRepo.Object, groupRepo.Object, paymentRepo.Object,
            prefRepo.Object, settingsRepo.Object, userLookup.Object, outboxRepo.Object, priceRepo.Object,
            assignmentRepo.Object, uow.Object, new ApplicationMapper(), logger.Object, new FixedClock());

        Result<Guid> result = await service.SubmitEnrollmentAsync(SeriesId, scenario.Request);

        log.Add(result.IsSuccess
            ? $"RESULT ok value={Fmt(result.Value)}"
            : $"RESULT fail [{string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Message}"))}]");

        foreach (IInvocation invocation in logger.Invocations.Where(i => i.Method.Name == nameof(ILogger.Log)))
        {
            LogLevel level = (LogLevel)invocation.Arguments[0]!;
            object? state = invocation.Arguments[2];
            Exception? exception = (Exception?)invocation.Arguments[3];
            Delegate formatter = (Delegate)invocation.Arguments[4]!;
            string message = (string)formatter.DynamicInvoke(state, exception)!;
            log.Add($"LOG {level}: {message}{(exception is null ? "" : $" [{exception.GetType().Name}: {exception.Message}]")}");
        }

        string transcript = Normalize(string.Join("\n", log)) + "\n";
        AssertApproved(name, transcript);
    }

    // ── Hulpmiddelen ──────────────────────────────────────────────────────────

    private static string Fmt(Guid id) => id.ToString();

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    // Audit-tijdstempels en AvailableAt (entiteit-default op de wandklok) zijn niet deterministisch;
                    // de service zet ze niet, dus ze maken geen deel uit van het gedrag.
                    foreach (JsonPropertyInfo property in typeInfo.Properties.ToList())
                    {
                        if (property.Name is "createdAt" or "updatedAt" or "availableAt")
                            typeInfo.Properties.Remove(property);
                    }
                },
            },
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string Snapshot(object value) => $"{value.GetType().Name} {JsonSerializer.Serialize(value, value.GetType(), SnapshotOptions)}";

    /// <summary>Vervangt de willekeurige guids (nieuwe lessen) door stabiele nummers.</summary>
    private static string Normalize(string transcript)
    {
        Dictionary<string, string> known = new(StringComparer.OrdinalIgnoreCase);
        int counter = 0;
        return GuidPattern().Replace(transcript, match =>
        {
            string value = match.Value;
            if (value.StartsWith("00000000-0000-0000-0000-", StringComparison.Ordinal))
                return value;
            if (!known.TryGetValue(value, out string? replacement))
            {
                replacement = $"<guid-{++counter}>";
                known[value] = replacement;
            }
            return replacement;
        });
    }

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();

    private static string GoldenDirectory([CallerFilePath] string sourceFile = "")
        => Path.Combine(Path.GetDirectoryName(sourceFile)!, "Characterization", "SubmitEnrollment");

    private static void AssertApproved(string name, string transcript)
    {
        string path = Path.Combine(GoldenDirectory(), $"{name}.approved.txt");

        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, transcript, new UTF8Encoding(false));
            return;
        }

        File.Exists(path).Should().BeTrue($"het goedgekeurde bestand {path} moet bestaan (UPDATE_GOLDEN=1 om te genereren)");
        string approved = File.ReadAllText(path).Replace("\r\n", "\n");
        transcript.Should().Be(approved, "het gedrag van SubmitEnrollmentAsync mag niet veranderen");
    }
}
