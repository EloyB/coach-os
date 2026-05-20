using CoachOS.Domain.Models;

namespace CoachOS.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToErrorResult(this Result result)
    {
        var statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    public static IResult ToErrorResult<T>(this Result<T> result)
    {
        var statusCode = MapStatusCode(result.Errors);
        return Results.Json(
            result.Errors.Select(e => e.Message),
            statusCode: statusCode);
    }

    private static int MapStatusCode(IReadOnlyList<Error> errors)
    {
        var code = errors.Count > 0 ? errors[0].Code : ErrorCodes.Unexpected;
        return code switch
        {
            ErrorCodes.Validation => StatusCodes.Status400BadRequest,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorCodes.ExternalService => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest,
        };
    }
}
