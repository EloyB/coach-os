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

                return Results.Text(result.Value!, "text/calendar", Encoding.UTF8);
            })
        .AllowAnonymous()
        .WithTags("StudentConfirmation");
    }
}
