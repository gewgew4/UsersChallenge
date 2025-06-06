using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehaviour<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            logger.LogWarning("Validation failed for {RequestType}. Errors: {Errors}",
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new Common.Exceptions.ValidationException(
                [.. failures.Select(f => f.ErrorMessage)]);
        }

        return await next(cancellationToken);
    }
}