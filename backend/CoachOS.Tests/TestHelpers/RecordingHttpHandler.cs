using System.Net;
using System.Text;

namespace CoachOS.Tests.TestHelpers;

/// <summary>
/// HttpMessageHandler die elke request (met body) vastlegt en een vooraf ingesteld antwoord
/// teruggeeft. Voor tests van HTTP-clients zonder netwerk.
/// </summary>
public sealed class RecordingHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

    public static RecordingHttpHandler Json(HttpStatusCode status, string json)
        => new(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.Method, request.RequestUri!.ToString(), body));
        return respond(request);
    }
}
