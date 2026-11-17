using System.Text.Json;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Email;
using CoachOS.Infrastructure.Persistence;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Elke mail gaat via de outbox: de <see cref="OutboxEmailService"/> plant hem in, de
/// <see cref="EmailOutboxWorker"/> speelt hem later af tegen de echte mailservice. Deze
/// tests bewijzen per mailtype dat die rondreis (inplannen → JSON → afspelen) exact
/// dezelfde aanroep met dezelfde argumenten oplevert als rechtstreeks versturen.
/// </summary>
[TestFixture]
public class OutboxEmailServiceTests
{
    // Velden meenemen: anders serialiseert System.Text.Json tuple-lijsten als lege objecten
    // en zou de vergelijking hieronder een fout in de trainer-mail niet zien.
    private static readonly JsonSerializerOptions CompareOptions = new() { IncludeFields = true };

    private sealed class CapturingWriter : IEmailOutboxWriter
    {
        public List<IOutboxEmail> Mails { get; } = [];

        public Task EnqueueAsync(IOutboxEmail mail)
        {
            Mails.Add(mail);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTenant(Guid organizationId) : ITenantContext
    {
        public Guid OrganizationId => organizationId;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => organizationId != Guid.Empty;
    }

    private static readonly DateOnly D1 = new(2026, 11, 3);
    private static readonly DateOnly D2 = new(2026, 11, 17);
    private static readonly TimeOnly T1 = new(18, 0);
    private static readonly TimeOnly T2 = new(19, 30);
    private static readonly TimeOnly T3 = new(20, 30);

    /// <summary>Eén aanroep per methode van IEmailService, met unieke waarden per argument.</summary>
    private static IEnumerable<TestCaseData> Calls()
    {
        static TestCaseData Case(string name, Func<IEmailService, Task> call)
            => new TestCaseData(call).SetName(name);

        yield return Case("WebsiteWaitlist", s => s.SendWebsiteWaitlistAsync("w@x.be", "tok-1"));
        yield return Case("WebsiteContactNotification", s => s.SendWebsiteContactNotificationAsync(
            "admin@x.be", new WebsiteContactNotification("Ann", "ann@x.be", "TC Ace", "Hallo",
                new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc))));
        yield return Case("TrainerInvite", s => s.SendTrainerInviteAsync("t@x.be", "Tom", "https://x/invite/1"));
        yield return Case("AdminInvite", s => s.SendAdminInviteAsync("a@x.be", "Ada", "TC Ace", "https://x/invite/2"));
        yield return Case("EnrollmentConfirmation", s => s.SendEnrollmentConfirmationAsync(
            "s@x.be", "Sam", "Zomerlessen", ["Sam", "Sara"]));
        yield return Case("EnrollmentConfirmationWithoutNames", s => s.SendEnrollmentConfirmationAsync(
            "s@x.be", "Sam", "Zomerlessen"));
        yield return Case("GroupMemberAdded", s => s.SendGroupMemberAddedAsync("g@x.be", "Gil", "Reeks", "Groep A"));
        yield return Case("EnrollmentPendingCash", s => s.SendEnrollmentPendingCashAsync(
            "c@x.be", "Cas", "Reeks", 137.50m, ["Cas", "Cor"]));
        yield return Case("TrainerEnrollmentNotification", s => s.SendEnrollmentNotificationToTrainerAsync(
            "tr@x.be", "Trees", "Stan", "stan@x.be", "Reeks",
            [("Niveau", "Gevorderd"), ("Allergie", "Geen")]));
        yield return Case("ScheduleConfirmation", s => s.SendScheduleConfirmationAsync(
            "sc@x.be", "Sofie", "Reeks", 2, "18:00", "19:00", "Baan 3", "https://x/confirm/abc", ["Sofie", "Stef"]));
        yield return Case("ScheduleConfirmationBundle", s => s.SendScheduleConfirmationBundleAsync(
            "ouder@x.be", "Reeks",
            [
                new ScheduleConfirmationItem("Lien", 1, "17:00", "18:00", null, "https://x/confirm/1"),
                new ScheduleConfirmationItem("Lars", 4, "10:00", "11:00", "Baan 2", "https://x/confirm/2"),
            ]));
        yield return Case("StudentMagicLink", s => s.SendStudentMagicLinkAsync("m@x.be", "https://x/magic/zz"));
        yield return Case("LessonCancellation", s => s.SendLessonCancellationAsync(
            "lc@x.be", "Lou", "Reeks", D1, T1, "Regen"));
        yield return Case("LessonRescheduled", s => s.SendLessonRescheduledAsync(
            "lr@x.be", "Rik", "Reeks", D1, T1, D2, T2, T3, "Baan 1", "Trainer ziek"));
        yield return Case("ScheduleInfo", s => s.SendScheduleInfoAsync(
            "si@x.be", "Ine", "Reeks", "Groep B", 3, "09:00", "10:00", "Baan 4"));
        yield return Case("AssignmentMoved", s => s.SendAssignmentMovedAsync(
            "am@x.be", "Mo", "Reeks", 0, "18:00", "19:00", 5, "11:00", "12:00", "Baan 5", ["Mo", "Mia"]));
        yield return Case("StandaloneLessonInvitation", s => s.SendStandaloneLessonInvitationAsync(
            "sl@x.be", "Saar", D2, T1, T2, "Baan 6", "Beginner", "Neem water mee", "https://x/inv/9"));
        yield return Case("PasswordReset", s => s.SendPasswordResetAsync("pr@x.be", "Pieter", "https://x/reset/r1"));
        yield return Case("CampEnrollmentPaymentLink", s => s.SendCampEnrollmentPaymentLinkAsync(
            "cp@x.be", "Cleo", "Paaskamp", D1, D2, "https://x/checkout/c1"));
        yield return Case("CampEnrollmentConfirmed", s => s.SendCampEnrollmentConfirmedAsync(
            "cc@x.be", "Cleo", "Paaskamp", D1, D2));
        yield return Case("EmailVerification", s => s.SendEmailVerificationAsync("ev@x.be", "Eva", "482913"));
        yield return Case("TrialReminder", s => s.SendTrialReminderAsync(
            "tr2@x.be", "TC Ace", 3, D2, "https://app.x"));
        yield return Case("SubscriptionStarted", s => s.SendSubscriptionStartedAsync(
            "ss@x.be", "TC Ace", "Set", "jaarlijks", 1199.88m, D1, true, D2, "https://x/manage"));
        yield return Case("SubscriptionStartedWithoutDueDate", s => s.SendSubscriptionStartedAsync(
            "ss@x.be", "TC Ace", "Game", "maandelijks", 49.99m, D1, false, null, "https://x/manage"));
        yield return Case("SubscriptionPaymentFailed", s => s.SendSubscriptionPaymentFailedAsync(
            "sp@x.be", "TC Ace", 99.99m, "https://x/pay"));
        yield return Case("SubscriptionInvitation", s => s.SendSubscriptionInvitationAsync(
            "si2@x.be", "TC Ace", "https://x/subscribe"));
        yield return Case("BillingSuperAdminNotice", s => s.SendBillingSuperAdminNoticeAsync(
            "sa@x.be", "Betaling mislukt", "Details hier", "https://x/admin"));
    }

    [TestCaseSource(nameof(Calls))]
    public async Task Mail_roundtrips_through_outbox_to_identical_smtp_call(Func<IEmailService, Task> call)
    {
        // Verwacht: de aanroep zoals hij vroeger rechtstreeks bij de mailservice aankwam.
        Mock<IEmailService> direct = new();
        await call(direct.Object);
        IInvocation expected = direct.Invocations.Single();

        // Via de outbox: inplannen, als rij bewaren (type + JSON) en door de worker laten afspelen.
        CapturingWriter writer = new();
        await call(new OutboxEmailService(writer));
        IOutboxEmail queued = writer.Mails.Single();
        (string type, string payload) = OutboxEmails.Serialize(queued);

        Mock<IEmailService> smtp = new();
        await EmailOutboxWorker.DispatchAsync(type, payload, smtp.Object, CancellationToken.None);
        IInvocation actual = smtp.Invocations.Single();

        actual.Method.Name.Should().Be(expected.Method.Name);
        ArgumentsJson(actual).Should().Be(ArgumentsJson(expected));
    }

    [Test]
    public async Task Every_email_service_method_is_covered_by_a_roundtrip_case()
    {
        // Wie een nieuwe mail toevoegt aan IEmailService, moet hem ook via de outbox laten lopen
        // (record + registratie + case hierboven). Anders faalt deze test.
        HashSet<string> covered = [];
        foreach (TestCaseData data in Calls())
        {
            Mock<IEmailService> mock = new();
            await ((Func<IEmailService, Task>)data.Arguments[0]!)(mock.Object);
            covered.Add(mock.Invocations.Single().Method.Name);
        }

        typeof(IEmailService).GetMethods().Select(m => m.Name).Distinct()
            .Should().BeEquivalentTo(covered);
    }

    [Test]
    public void Every_outbox_mail_record_is_registered_with_its_own_type_name()
    {
        List<Type> records = typeof(IOutboxEmail).Assembly.GetTypes()
            .Where(t => typeof(IOutboxEmail).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        OutboxEmails.Keys.Should().HaveCount(records.Count);
        OutboxEmails.Keys.Should().OnlyHaveUniqueItems();
        // Mag nooit botsen met de inschrijvingsmails die EnrollmentService zelf aanmaakt.
        OutboxEmails.Keys.Should().NotContain([
            EmailOutboxMessageTypes.EnrollmentConfirmation,
            EmailOutboxMessageTypes.TrainerNotification,
            EmailOutboxMessageTypes.GroupMemberAdded,
        ]);
    }

    [Test]
    public async Task Dispatch_still_delivers_enrollment_mails_queued_by_enrollment_service()
    {
        string payload = JsonSerializer.Serialize(new EnrollmentConfirmationEmailPayload(
            "s@x.be", "Sam", "Zomerlessen", ["Sam"]));
        Mock<IEmailService> smtp = new();

        await EmailOutboxWorker.DispatchAsync(
            EmailOutboxMessageTypes.EnrollmentConfirmation, payload, smtp.Object, CancellationToken.None);

        smtp.Verify(s => s.SendEnrollmentConfirmationAsync(
            "s@x.be", "Sam", "Zomerlessen",
            It.Is<IReadOnlyList<string>?>(n => n != null && n.Single() == "Sam"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Dispatch_rejects_unknown_type_so_the_message_is_retried_and_kept()
    {
        Func<Task> act = () => EmailOutboxWorker.DispatchAsync(
            "mail:does-not-exist", "{}", Mock.Of<IEmailService>(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Writer_stores_pending_row_with_tenant_and_without_enrollment()
    {
        Guid orgId = Guid.NewGuid();
        (IServiceProvider services, DbContextOptions<ApplicationDbContext> options) = BuildServices();
        EmailOutboxWriter writer = Writer(services, new EmailOrganizationContext(new FixedTenant(orgId)));

        await writer.EnqueueAsync(new PasswordResetMail(" Pieter.R@X.be ", "Pieter", "https://x/reset/r1"));

        await using ApplicationDbContext db = new(options, new FixedTenant(Guid.Empty));
        EmailOutboxMessage row = await db.EmailOutboxMessages.SingleAsync();
        row.Type.Should().Be("mail:password-reset");
        row.Status.Should().Be(EmailOutboxStatuses.Pending);
        row.OrganizationId.Should().Be(orgId);
        row.EnrollmentId.Should().BeNull();
        // Genormaliseerd zodat "speler wissen" de mail via het adres terugvindt.
        row.RecipientEmail.Should().Be("pieter.r@x.be");
        row.Payload.Should().NotContain("Recipient", "het adres staat al in de argumenten van de aanroep");
        OutboxEmails.TryDeserialize(row.Type, row.Payload, out IOutboxEmail? mail).Should().BeTrue();
        mail.Should().Be(new PasswordResetMail(" Pieter.R@X.be ", "Pieter", "https://x/reset/r1"));
    }

    [Test]
    public async Task Writer_leaves_organization_empty_for_platform_mail_without_tenant()
    {
        // Platformmails (inloglink, wachtwoordreset) horen bij geen club.
        (IServiceProvider services, DbContextOptions<ApplicationDbContext> options) = BuildServices();
        Mock<ILogger<EmailOutboxWriter>> logger = new();
        EmailOutboxWriter writer = Writer(services, new EmailOrganizationContext(new FixedTenant(Guid.Empty)), logger);

        await writer.EnqueueAsync(new StudentMagicLinkMail("m@x.be", "https://x/magic/zz"));

        await using ApplicationDbContext db = new(options, new FixedTenant(Guid.Empty));
        (await db.EmailOutboxMessages.SingleAsync()).OrganizationId.Should().BeNull();
        VerifyWarnings(logger, Times.Never());
    }

    [Test]
    public async Task Writer_uses_organization_given_by_public_flow_without_tenant()
    {
        // Bevestigingslink of Mollie-webhook: geen tenant, de flow geeft de club expliciet mee.
        Guid orgId = Guid.NewGuid();
        (IServiceProvider services, DbContextOptions<ApplicationDbContext> options) = BuildServices();
        Mock<ILogger<EmailOutboxWriter>> logger = new();
        EmailOrganizationContext organization = new(new FixedTenant(Guid.Empty));
        organization.Use(orgId);
        EmailOutboxWriter writer = Writer(services, organization, logger);

        await writer.EnqueueAsync(new EnrollmentConfirmationMail("ouder@x.be", "Emma", "Reeks A", null));

        await using ApplicationDbContext db = new(options, new FixedTenant(Guid.Empty));
        (await db.EmailOutboxMessages.SingleAsync()).OrganizationId.Should().Be(orgId);
        VerifyWarnings(logger, Times.Never());
    }

    [Test]
    public void Delivered_mail_loses_its_content_but_keeps_its_recipient()
    {
        // Inhoud (naam, links, codes) weg; het adres blijft voor support en voor "speler wissen",
        // tot de privacy-opruiming de verstuurde mail na 30 dagen verwijdert.
        DateTime now = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);
        EmailOutboxMessage message = new()
        {
            RecipientEmail = "ouder@x.be",
            Type = "mail:password-reset",
            Payload = """{"ToEmail":"ouder@x.be","ResetUrl":"https://x/reset/r1"}""",
            Status = EmailOutboxStatuses.Processing,
            LastError = "eerdere poging mislukt",
        };

        EmailOutboxWorker.MarkDelivered(message, now);

        message.Status.Should().Be(EmailOutboxStatuses.Completed);
        message.ProcessedAt.Should().Be(now);
        message.LastError.Should().BeNull();
        message.Payload.Should().Be(EmailOutboxMessage.ScrubbedPayload);
        message.RecipientEmail.Should().Be("ouder@x.be");
    }

    [Test]
    public void Explicit_organization_wins_over_tenant()
    {
        Guid tenantOrg = Guid.NewGuid();
        Guid linkOrg = Guid.NewGuid();
        EmailOrganizationContext organization = new(new FixedTenant(tenantOrg));

        organization.Current.Should().Be(tenantOrg);
        organization.Use(linkOrg);
        organization.Current.Should().Be(linkOrg);
    }

    [Test]
    public void Use_rejects_empty_organization()
    {
        EmailOrganizationContext organization = new(new FixedTenant(Guid.Empty));

        Action act = () => organization.Use(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task Writer_warns_when_club_mail_has_no_organization()
    {
        // Een vergeten publieke flow: de mail gaat nog altijd weg, maar valt op in de logs.
        (IServiceProvider services, DbContextOptions<ApplicationDbContext> options) = BuildServices();
        Mock<ILogger<EmailOutboxWriter>> logger = new();
        EmailOutboxWriter writer = Writer(services, new EmailOrganizationContext(new FixedTenant(Guid.Empty)), logger);

        await writer.EnqueueAsync(new CampEnrollmentConfirmedMail(
            "kamp@x.be", "Emma", "Paaskamp", new DateOnly(2026, 4, 14), new DateOnly(2026, 4, 16)));

        await using ApplicationDbContext db = new(options, new FixedTenant(Guid.Empty));
        (await db.EmailOutboxMessages.SingleAsync()).OrganizationId.Should().BeNull();
        VerifyWarnings(logger, Times.Once());
    }

    [Test]
    public void Every_mail_about_a_player_or_lesson_is_marked_as_club_mail()
    {
        // Wie een spelersmail toevoegt, moet ze als clubmail markeren; anders blijft een
        // ontbrekende organisatie onopgemerkt (offboarding, speler wissen).
        string[] platformMails =
        [
            "mail:website-waitlist", "mail:website-contact-notification", "mail:trainer-invite",
            "mail:admin-invite", "mail:student-magic-link", "mail:password-reset",
            "mail:email-verification", "mail:trial-reminder", "mail:subscription-started",
            "mail:subscription-payment-failed", "mail:subscription-invitation",
            "mail:billing-super-admin-notice",
        ];

        foreach (string key in OutboxEmails.Keys)
        {
            bool isClubMail = OutboxEmails.TypeOf(key).IsAssignableTo(typeof(IClubOutboxEmail));
            isClubMail.Should().Be(!platformMails.Contains(key), $"{key} moet juist ingedeeld zijn");
        }

        // Speler wissen neemt zonder organisatie enkel deze typen mee.
        OutboxEmails.PlatformKeys.Should().BeEquivalentTo(platformMails);
    }

    private static EmailOutboxWriter Writer(
        IServiceProvider services, EmailOrganizationContext organization,
        Mock<ILogger<EmailOutboxWriter>>? logger = null)
        => new(services.GetRequiredService<IServiceScopeFactory>(), organization,
            (logger ?? new Mock<ILogger<EmailOutboxWriter>>()).Object);

    private static void VerifyWarnings(Mock<ILogger<EmailOutboxWriter>> logger, Times times)
        => logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);

    private static string ArgumentsJson(IInvocation invocation)
        => JsonSerializer.Serialize(
            invocation.Arguments.Where(a => a is not CancellationToken).ToArray(), CompareOptions);

    private static (IServiceProvider, DbContextOptions<ApplicationDbContext>) BuildServices()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        ServiceCollection services = new();
        services.AddScoped(_ => new ApplicationDbContext(options, new FixedTenant(Guid.Empty)));
        return (services.BuildServiceProvider(), options);
    }
}
