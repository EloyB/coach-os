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

    /// <summary>
    /// Hoofdtrainers hebben enkel leesrechten op inschrijvingen/planning van hun club(s):
    /// bekijken mag, muteren niet. Spiegelt de frontend-gating (<c>isHeadTrainerViewer</c>).
    /// Admin mag alles; een gewone trainer (zonder hoofdtrainer-club) ook. Blokkeert dus enkel
    /// de niet-admin hoofdtrainer op muterende endpoints — client-side verbergen is geen
    /// autorisatiegrens, dus dwing het hier af.
    /// </summary>
    public static Result EnsureWriteAllowed(HttpContext ctx)
    {
        if (ctx.IsAdmin())
            return Result.Ok();

        if (ctx.GetHeadTrainerClubIds().Count > 0)
            return Result.Fail(new Error(ErrorCodes.Forbidden,
                "Als hoofdtrainer heb je enkel leesrechten: je kan inschrijvingen niet aanpassen of annuleren."));

        return Result.Ok();
    }
}
