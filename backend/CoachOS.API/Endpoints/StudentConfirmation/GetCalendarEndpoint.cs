using System.Text;
using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;

namespace CoachOS.API.Endpoints.StudentConfirmation;

public class GetCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/confirm/{token}/calendar.ics",
            async (string token, IStudentConfirmationService service, CancellationToken ct) =>
            {
                var result = await service.GenerateCalendarAsync(token, ct);
                if (!result.IsSuccess)
                    return result.ToErrorResult();

                return CreateCalendarDownload(result.Value!);
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }

    public static IResult CreateCalendarDownload(string icsContent)
        => Results.File(
            Encoding.UTF8.GetBytes(icsContent),
            "text/calendar; charset=utf-8",
            "calendar.ics");
}
