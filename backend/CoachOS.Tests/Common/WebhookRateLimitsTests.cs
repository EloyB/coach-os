using System.Net;
using System.Threading.RateLimiting;
using CoachOS.API.Extensions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class WebhookRateLimitsTests
{
    private static DefaultHttpContext Request(string ip)
    {
        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }

    [Test]
    public void MolliePartition_DifferentMollieIps_ShareOneBucket()
    {
        string a = WebhookRateLimits.MolliePartition(Request("203.0.113.1")).PartitionKey;
        string b = WebhookRateLimits.MolliePartition(Request("198.51.100.7")).PartitionKey;

        a.Should().Be(b).And.Be(WebhookRateLimits.MolliePolicy);
    }

    [Test]
    public void MolliePartition_AllowsFarMoreThanPublicLimit()
    {
        RateLimitPartition<string> partition = WebhookRateLimits.MolliePartition(Request("203.0.113.1"));
        using RateLimiter limiter = partition.Factory(partition.PartitionKey);

        int acquired = Enumerable.Range(0, WebhookRateLimits.MolliePermitsPerMinute + 1)
            .Count(_ => limiter.AttemptAcquire().IsAcquired);

        WebhookRateLimits.MolliePermitsPerMinute.Should().BeGreaterThan(30);
        acquired.Should().Be(WebhookRateLimits.MolliePermitsPerMinute);
    }
}
