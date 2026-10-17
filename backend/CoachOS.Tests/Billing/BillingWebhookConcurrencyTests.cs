using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Mollie;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

/// <summary>
/// Gelijktijdige Mollie-webhooks voor dezelfde betaling mogen nooit twee Mollie Subscriptions,
/// twee betalingsrijen of twee facturen opleveren. Vereist echte Postgres (concurrency-token en
/// unieke index); opt-in via COACHOS_TEST_PG, zie <c>TrainerServiceConcurrencyTests</c>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BillingWebhookConcurrencyTests
{
    private const int Rounds = 10;
    private const int ConcurrentWebhooks = 8;

    private string? _adminConnectionString;
    private string _databaseName = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
            Assert.Ignore("COACHOS_TEST_PG niet gezet: concurrency-tests tegen Postgres overgeslagen.");

        _databaseName = $"coachos_conc_{Guid.NewGuid():N}";
        await using (NpgsqlConnection admin = new(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using NpgsqlCommand create = new($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        NpgsqlConnectionStringBuilder builder = new(_adminConnectionString) { Database = _databaseName };
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(builder.ConnectionString).Options;

        using BillingTestContext setup = new(_options, null, null);
        await setup.Db.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString) || _databaseName is null)
            return;

        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_adminConnectionString);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private static async Task RunConcurrentlyAsync(IEnumerable<Func<Task>> actions)
    {
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] tasks = actions.Select(action => Task.Run(async () =>
        {
            await start.Task;
            await action();
        })).ToArray();
        start.SetResult();
        await Task.WhenAll(tasks);
    }

    /// <summary>Elke "webhook-request" krijgt een eigen DbContext, maar deelt Mollie en e-mail.</summary>
    private async Task SyncConcurrentlyAsync(
        FakeBillingMollieClient fake, Mock<IBillingMollieClient> mollie, string molliePaymentId)
    {
        List<BillingTestContext> contexts = Enumerable.Range(0, ConcurrentWebhooks)
            .Select(_ => new BillingTestContext(_options, fake, mollie))
            .ToList();
        try
        {
            await RunConcurrentlyAsync(contexts.Select(c =>
            {
                BillingPaymentSyncService sync = c.CreateSyncService(c.CreateBillingService());
                return (Func<Task>)(() => sync.SyncAsync(molliePaymentId));
            }));
        }
        finally
        {
            contexts.ForEach(c => c.Dispose());
        }
    }

    [Test]
    public async Task ConcurrentWebhooks_SameFirstPayment_ExactlyOneMollieSubscriptionAndOneInvoiceRow()
    {
        for (int round = 0; round < Rounds; round++)
        {
            using BillingTestContext main = new(_options, null, null);
            (Organization org, _) = await main.SeedOrganizationAsync(email: $"admin-{Guid.NewGuid():N}@test.be");
            await main.CreateBillingService().StartCheckoutAsync(org.Id, new CheckoutRequest(
                (int)SubscriptionPlan.Set, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
            SubscriptionPayment first = (await main.ReloadPaymentsAsync(org.Id)).Single();
            main.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");

            await SyncConcurrentlyAsync(main.FakeMollie, main.Mollie, first.MolliePaymentId!);

            main.Mollie.Verify(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()),
                Times.Once, $"ronde {round}");
            List<SubscriptionPayment> rows = await main.ReloadPaymentsAsync(org.Id);
            rows.Should().ContainSingle($"ronde {round}");
            rows[0].Status.Should().Be(SubscriptionPaymentStatus.Paid, $"ronde {round}");
            rows[0].InvoiceStatus.Should().Be(InvoiceStatus.Pending, $"ronde {round}");
            Subscription subscription = await main.ReloadSubscriptionAsync(org.Id);
            subscription.Status.Should().Be(SubscriptionStatus.Active, $"ronde {round}");
            subscription.MollieSubscriptionId.Should().NotBeNull($"ronde {round}");
        }
    }

    [Test]
    public async Task ConcurrentCheckouts_SameOrganization_AtMostOneOpenFirstPayment()
    {
        for (int round = 0; round < Rounds; round++)
        {
            using BillingTestContext main = new(_options, null, null);
            (Organization org, _) = await main.SeedOrganizationAsync(email: $"admin-{Guid.NewGuid():N}@test.be");
            List<BillingTestContext> contexts = Enumerable.Range(0, ConcurrentWebhooks)
                .Select(_ => new BillingTestContext(_options, main.FakeMollie, main.Mollie))
                .ToList();
            try
            {
                await RunConcurrentlyAsync(contexts.Select(c =>
                {
                    BillingService billing = c.CreateBillingService();
                    return (Func<Task>)(() => billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
                        (int)SubscriptionPlan.Set, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie)));
                }));
            }
            finally
            {
                contexts.ForEach(c => c.Dispose());
            }

            (await main.ReloadPaymentsAsync(org.Id)).Count(p => p.Status == SubscriptionPaymentStatus.Open)
                .Should().BeLessThanOrEqualTo(1, $"ronde {round}");
            main.Mollie.Verify(m => m.CreateFirstPaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()),
                Times.AtMostOnce(), $"ronde {round}");
        }
    }

    [Test]
    public async Task ConcurrentWebhooks_SameRecurringPayment_OneRecurringRow()
    {
        for (int round = 0; round < Rounds; round++)
        {
            using BillingTestContext main = new(_options, null, null);
            (Organization org, _) = await main.SeedOrganizationAsync(email: $"admin-{Guid.NewGuid():N}@test.be");
            BillingService billing = main.CreateBillingService();
            await billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
                (int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
            SubscriptionPayment first = (await main.ReloadPaymentsAsync(org.Id)).Single();
            main.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");
            await main.CreateSyncService(billing).SyncAsync(first.MolliePaymentId!);
            Subscription active = await main.ReloadSubscriptionAsync(org.Id);
            string recurringId = main.FakeMollie.AddFakeRecurringPayment(
                active.MollieSubscriptionId!, active.MollieCustomerId!, 30.25m, "paid");

            await SyncConcurrentlyAsync(main.FakeMollie, main.Mollie, recurringId);

            (await main.ReloadPaymentsAsync(org.Id)).Count(p => p.Kind == SubscriptionPaymentKind.Recurring)
                .Should().Be(1, $"ronde {round}");
        }
    }
}
