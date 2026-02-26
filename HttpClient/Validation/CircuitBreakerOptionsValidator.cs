using FluentValidation;

namespace Http.Client.Validation;

/// <summary>
/// Validates <see cref="CircuitBreakerOptions"/> when the circuit breaker is enabled.
/// </summary>
/// <remarks>
/// This validator is only executed when <see cref="CircuitBreakerOptions.Enabled"/> is
/// <see langword="true"/> (the conditional check is in <see cref="ServiceClientOptionsValidator"/>).
/// <list type="bullet">
///   <item><see cref="CircuitBreakerOptions.FailureRatio"/> — must be exclusively between <c>0</c> and <c>1</c>.</item>
///   <item><see cref="CircuitBreakerOptions.MinimumThroughput"/> — must be at least <c>10</c>.</item>
///   <item><see cref="CircuitBreakerOptions.SamplingDurationSeconds"/> — must be greater than <c>10</c>.</item>
///   <item><see cref="CircuitBreakerOptions.BreakDurationSeconds"/> — must be at least <c>5</c>.</item>
///   <item><see cref="CircuitBreakerOptions.FailureStatusCodes"/> — must contain at least one status code.</item>
/// </list>
/// </remarks>
public sealed class CircuitBreakerOptionsValidator : AbstractValidator<CircuitBreakerOptions>
{
    public CircuitBreakerOptionsValidator()
    {
        RuleFor(x => x.FailureRatio)
            .ExclusiveBetween(0, 1)
            .WithMessage("'{PropertyName}' must be between 0 (exclusive) and 1 (exclusive). Set it in 'ServiceClients:<service>:CircuitBreaker:FailureRatio' in appsettings");

        RuleFor(x => x.MinimumThroughput)
            .GreaterThanOrEqualTo(10)
            .WithMessage("'{PropertyName}' must be at least 10. Set it in 'ServiceClients:<service>:CircuitBreaker:MinimumThroughput' in appsettings");

        RuleFor(x => x.SamplingDurationSeconds)
            .GreaterThan(10)
            .WithMessage("'{PropertyName}' must be greater than 10. Set it in 'ServiceClients:<service>:CircuitBreaker:SamplingDurationSeconds' in appsettings");

        RuleFor(x => x.BreakDurationSeconds)
            .GreaterThanOrEqualTo(5)
            .WithMessage("'{PropertyName}' must be at least 5. Set it in 'ServiceClients:<service>:CircuitBreaker:BreakDurationSeconds' in appsettings");

        RuleFor(x => x.FailureStatusCodes)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must not be empty. Set it in 'ServiceClients:<service>:CircuitBreaker:FailureStatusCodes' in appsettings");
    }
}
