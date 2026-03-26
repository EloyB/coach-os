using CoachOS.Domain.Models;

namespace CoachOS.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToErrorResult(this Result result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    public static IResult ToErrorResult<T>(this Result<T> result)
    {
        int statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    private static int MapStatusCode(IReadOnlyList<Error> errors)
    {
        string code = errors.Count > 0 ? errors[0].Code : ErrorCodes.Unexpected;
        return code switch
        {
            ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.Conflict => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };
    }
}
