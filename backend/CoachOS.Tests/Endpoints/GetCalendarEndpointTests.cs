using System.Text;
using CoachOS.API.Endpoints.StudentConfirmation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CoachOS.Tests.Endpoints;

[TestFixture]
public class GetCalendarEndpointTests
{
    [Test]
    public async Task CreateCalendarDownload_ReturnsCalendarAttachmentWithStableFilename()
    {
        const string ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nEND:VCALENDAR\r\n";
        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();

        IResult result = GetCalendarEndpoint.CreateCalendarDownload(ics);
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.ContentType.Should().StartWith("text/calendar");
        context.Response.Headers.ContentDisposition.ToString()
            .Should().Contain("attachment")
            .And.Contain("calendar.ics");
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        (await reader.ReadToEndAsync()).Should().Be(ics);
    }
}
