using FluentValidation;

namespace HttpClient.Validation;

/// <summary>
/// Validates a <see cref="ServiceClientOptions"/> instance, including conditional
/// validation of nested <see cref="RetryOptions"/> and <see cref="CircuitBreakerOptions"/>
/// when those features are enabled.
/// </summary>
/// <remarks>
/// <para>
/// This is the root validator invoked at startup via <see cref="FluentValidateOptions{T}"/>.
/// It delegates to child validators for each nested options section:
/// </para>
/// <list type="bullet">
///   <item><see cref="ConcurrencyLimitOptionsValidator"/> — always validated.</item>
///   <item><see cref="RetryOptionsValidator"/> — validated only when <see cref="RetryOptions.Enabled"/> is <see langword="true"/>.</item>
///   <item><see cref="CircuitBreakerOptionsValidator"/> — validated only when <see cref="CircuitBreakerOptions.Enabled"/> is <see langword="true"/>.</item>
/// </list>
/// </remarks>
public sealed class ServiceClientOptionsValidator : AbstractValidator<ServiceClientOptions>
{
    public ServiceClientOptionsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .WithMessage("'{PropertyName}' is required. Set it in 'ServiceClients:<service>:BaseUrl' in appsettings")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .WithMessage("'{PropertyName}' must be a valid HTTP or HTTPS URL. Update it in 'ServiceClients:<service>:BaseUrl' in appsettings");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("'{PropertyName}' must be greater than 0. Set it in 'ServiceClients:<service>:TimeoutSeconds' in appsettings");

        RuleFor(x => x.ConcurrencyLimitOptions)
            .SetValidator(new ConcurrencyLimitOptionsValidator());

        RuleFor(x => x.Retry)
            .SetValidator(new RetryOptionsValidator())
            .When(x => x.Retry.Enabled);

        RuleFor(x => x.Retry.TimeoutSecondsPerRetry)
            .LessThan(x => x.TimeoutSeconds)
            .WithMessage("'{PropertyName}' ({PropertyValue}s) must be less than 'TimeoutSeconds' ({ComparisonValue}s). Adjust 'ServiceClients:<service>:Retry:TimeoutSecondsPerRetry' or 'ServiceClients:<service>:TimeoutSeconds' in appsettings")
            .When(x => x.Retry.Enabled);

        RuleFor(x => x.CircuitBreaker)
            .SetValidator(new CircuitBreakerOptionsValidator())
            .When(x => x.CircuitBreaker.Enabled);
    }
}