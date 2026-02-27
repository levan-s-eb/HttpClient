using FluentValidation;

namespace HttpClient.Validation;

/// <summary>
/// Validates <see cref="ConcurrencyLimitOptions"/> to ensure the concurrency limiter
/// is configured with safe, positive values.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="ConcurrencyLimitOptions.Limit"/> must be greater than <c>0</c>.</item>
///   <item><see cref="ConcurrencyLimitOptions.QueueSize"/> must be non-negative (<c>0</c> disables queuing).</item>
/// </list>
/// </remarks>
public sealed class ConcurrencyLimitOptionsValidator : AbstractValidator<ConcurrencyLimitOptions>
{
    public ConcurrencyLimitOptionsValidator()
    {
        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("'{PropertyName}' must be greater than 0. Set it in 'ServiceClients:<service>:ConcurrencyLimitOptions:Limit' in appsettings");

        RuleFor(x => x.QueueSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("'{PropertyName}' must be non-negative. Set it in 'ServiceClients:<service>:ConcurrencyLimitOptions:QueueSize' in appsettings");
    }
}
