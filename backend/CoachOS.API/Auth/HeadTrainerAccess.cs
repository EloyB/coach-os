using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;

namespace CoachOS.API.Auth;

/// <summary>
/// Fijne per-reeks autorisatie voor de verhoogde read-endpoints (inschrijvingen + planning).
/// Admin mag alles; een hoofdtrainer enkel reeksen van z'n hoofdtrainer-club(s).
/// </summary>
public static class HeadTrainerAccess
{
    public static async Task<Result> EnsureSerieAccessAsync(
        HttpContext ctx,
        ILessonSerieService series,
        Guid serieId,
        CancellationToken ct)
    {
        if (ctx.IsAdmin())
            return Result.Ok();

        Result<Guid> clubResult = await series.GetClubIdAsync(serieId, ctx.GetOrganizationId(), ct);
        if (!clubResult.IsSuccess)
            return Result.Fail(clubResult.Errors);

        IReadOnlyList<Guid> allowed = ctx.GetHeadTrainerClubIds();
        if (allowed.Contains(clubResult.Value))
            return Result.Ok();

        return Result.Fail(new Error(ErrorCodes.Forbidden,
            "Geen toegang tot deze reeks: je bent geen hoofdtrainer van de bijhorende club."));
    }
}
