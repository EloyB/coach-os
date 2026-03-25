using FluentValidation;

namespace CoachOS.API.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        T? argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(argument);
        if (result.IsValid)
            return await next(context);

        return Results.BadRequest(result.Errors.Select(e => e.ErrorMessage));
    }
}
