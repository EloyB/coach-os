using FluentValidation;
using MediatR;

namespace CoachOS.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior: valideert alle Commands via FluentValidation.
/// Wordt automatisch uitgevoerd vóór elke handler.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        ValidationContext<TRequest> context = new(request);

        FluentValidation.Results.ValidationResult[] validationResults =
            await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<FluentValidation.Results.ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
