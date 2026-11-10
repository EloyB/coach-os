using System.Net;
using System.Threading.RateLimiting;
using CoachOS.API.Extensions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class WebsiteRateLimitsTests
{
    private static DefaultHttpContext Request(string ip, string? policy)
    {
        DefaultHttpContext context = new();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        EndpointMetadataCollection metadata = policy is null ? new() : new(new EnableRateLimitingAttribute(policy));
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test"));
        return context;
    }

    [Test]
    public void PerClientPartition_DifferentIps_GetSeparateBuckets()
    {
        RateLimitPartition<string> a = WebsiteRateLimits.PerClientPartition(Request("203.0.113.1", null), WebsiteRateLimits.ContactPolicy);
        RateLimitPartition<string> b = WebsiteRateLimits.PerClientPartition(Request("203.0.113.2", null), WebsiteRateLimits.ContactPolicy);

        a.PartitionKey.Should().NotBe(b.PartitionKey);
    }

    [Test]
    public void PerClientPartition_SameIpOtherForm_GetsSeparateBucket()
    {
        DefaultHttpContext context = Request("203.0.113.1", null);

        WebsiteRateLimits.PerClientPartition(context, WebsiteRateLimits.ContactPolicy).PartitionKey
            .Should().NotBe(WebsiteRateLimits.PerClientPartition(context, WebsiteRateLimits.WaitlistPolicy).PartitionKey);
    }

    [Test]
    public void PerClientPartition_AllowsFivePerWindowThenRejects()
    {
        RateLimitPartition<string> partition = WebsiteRateLimits.PerClientPartition(Request("203.0.113.9", null), WebsiteRateLimits.ContactPolicy);
        using RateLimiter limiter = partition.Factory(partition.PartitionKey);

        IEnumerable<bool> acquired = Enumerable.Range(0, 6).Select(_ => limiter.AttemptAcquire().IsAcquired).ToList();

        acquired.Should().Equal(true, true, true, true, true, false);
    }

    [Test]
    public void BackstopPartition_WebsiteFormFromAnyIp_SharesOneBucket()
    {
        string a = WebsiteRateLimits.BackstopPartition(Request("203.0.113.1", WebsiteRateLimits.ContactPolicy)).PartitionKey;
        string b = WebsiteRateLimits.BackstopPartition(Request("198.51.100.7", WebsiteRateLimits.ContactPolicy)).PartitionKey;

        a.Should().Be(b).And.Contain(WebsiteRateLimits.ContactPolicy);
    }

    [Test]
    public void BackstopPartition_OtherEndpoints_AreUnlimited()
    {
        RateLimitPartition<string> partition = WebsiteRateLimits.BackstopPartition(Request("203.0.113.1", "public"));
        using RateLimiter limiter = partition.Factory(partition.PartitionKey);

        Enumerable.Range(0, 1000).All(_ => limiter.AttemptAcquire().IsAcquired).Should().BeTrue();
    }

    [Test]
    public void PerClientPartition_UnsubscribeHasOwnBucketPerIp()
    {
        DefaultHttpContext context = Request("203.0.113.1", null);

        WebsiteRateLimits.PerClientPartition(context, WebsiteRateLimits.UnsubscribePolicy).PartitionKey
            .Should().NotBe(WebsiteRateLimits.PerClientPartition(context, WebsiteRateLimits.WaitlistPolicy).PartitionKey)
            .And.Contain("203.0.113.1");
    }

    [Test]
    public void BackstopPartition_Unsubscribe_IsNotGloballyLimited()
    {
        RateLimitPartition<string> partition = WebsiteRateLimits.BackstopPartition(Request("203.0.113.1", WebsiteRateLimits.UnsubscribePolicy));

        partition.PartitionKey.Should().Be("none");
    }
}
