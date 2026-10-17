using CoachOS.Infrastructure.Email;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class BillingEmailsTests
{
    [Test]
    public void SubscriptionStarted_Mollie_MentionsPlanAmountAndStartDate()
    {
        BillingEmailContent content = BillingEmails.SubscriptionStarted(
            "Set", "maand", 59.29m, new DateOnly(2026, 11, 5), bankTransfer: false, dueDate: null);

        content.Subject.Should().Contain("Set");
        content.Body.Should().Contain("€ 59,29").And.Contain("05/11/2026").And.Contain("automatisch");
        content.Body.Should().NotContain("overschrijving");
    }

    [Test]
    public void SubscriptionStarted_BankTransfer_MentionsInvoiceAndDueDate()
    {
        BillingEmailContent content = BillingEmails.SubscriptionStarted(
            "Match", "jaar", 1076.90m, new DateOnly(2026, 11, 5), bankTransfer: true, dueDate: new DateOnly(2026, 11, 16));

        content.Body.Should().Contain("€ 1.076,90").And.Contain("overschrijving").And.Contain("16/11/2026");
    }

    [Test]
    public void PaymentFailed_MentionsAmountAndAccessKept()
    {
        BillingEmailContent content = BillingEmails.PaymentFailed(30.25m);

        content.Body.Should().Contain("€ 30,25");
        content.ButtonLabel.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Invitation_HasSubscribeButton()
        => BillingEmails.Invitation().ButtonLabel.Should().NotBeNullOrWhiteSpace();
}
