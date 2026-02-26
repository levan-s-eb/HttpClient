using FluentValidation;

namespace Http.Client.Validation;

/// <summary>
/// Validates <see cref="RetryOptions"/> when the retry policy is enabled.
/// </summary>
/// <remarks>
/// This validator is only executed when <see cref="RetryOptions.Enabled"/> is
/// <see langword="true"/> (the conditional check is in <see cref="ServiceClientOptionsValidator"/>).
/// <list type="bullet">
///   <item><see cref="RetryOptions.MaxAttempts"/> — must be between <c>1</c> and <c>10</c> inclusive.</item>
///   <item><see cref="RetryOptions.BaseDelayMilliseconds"/> — must be greater than <c>0</c>.</item>
///   <item><see cref="RetryOptions.TimeoutSecondsPerRetry"/> — must be greater than <c>0</c>.</item>
///   <item><see cref="RetryOptions.RetryableStatusCodes"/> — must contain at least one status code.</item>
/// </list>
/// </remarks>
public sealed class RetryOptionsValidator : AbstractValidator<RetryOptions>
{
    public RetryOptionsValidator()
    {
        RuleFor(x => x.MaxAttempts)
            .InclusiveBetween(1, 10)
            .WithMessage("'{PropertyName}' must be between 1 and 10. Set it in 'ServiceClients:<service>:Retry:MaxAttempts' in appsettings");

        RuleFor(x => x.BaseDelayMilliseconds)
            .GreaterThan(0)
            .WithMessage("'{PropertyName}' must be greater than 0. Set it in 'ServiceClients:<service>:Retry:BaseDelayMilliseconds' in appsettings");

        RuleFor(x => x.TimeoutSecondsPerRetry)
            .GreaterThan(0)
            .WithMessage("'{PropertyName}' must be greater than 0. Set it in 'ServiceClients:<service>:Retry:TimeoutSecondsPerRetry' in appsettings");

        RuleFor(x => x.RetryableStatusCodes)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must not be empty when retry is enabled. Set it in 'ServiceClients:<service>:Retry:RetryableStatusCodes' in appsettings");
    }
}
