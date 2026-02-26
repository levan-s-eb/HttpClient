using FluentValidation;
using Http.Client.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Refit;
using System.Net;

namespace Http.Client;

/// <summary>
/// Extension methods for registering resilient, configuration-driven HTTP service
/// clients with the <see cref="IServiceCollection"/> dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// This class is the main entry point for the <c>Http.Client</c> library. Call
/// <see cref="AddServiceClient{TApi}"/> once per downstream service to register a
/// Refit-based typed client backed by a Polly resilience pipeline.
/// </para>
/// <para>
/// Configuration is read from <c>ServiceClients:{serviceName}</c> in the application's
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> and validated at
/// startup. Invalid configuration prevents the application from starting, providing
/// fast feedback during deployment.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Refit-based HTTP service client for the interface <typeparamref name="TApi"/>
    /// with full resilience support (concurrency limiting, timeouts, retries, and circuit breaking).
    /// </summary>
    /// <typeparam name="TApi">
    /// A Refit interface that defines the HTTP endpoints for the downstream service.
    /// </typeparam>
    /// <param name="services">The service collection to register the client with.</param>
    /// <param name="serviceName">
    /// A unique name that identifies the downstream service. This name is used to:
    /// <list type="bullet">
    ///   <item>Resolve the configuration section <c>ServiceClients:{serviceName}</c></item>
    ///   <item>Create a named <see cref="ServiceClientOptions"/> instance</item>
    /// </list>
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="OptionsValidationException">
    /// Thrown at startup when <see cref="ServiceClientOptions"/> validation fails.
    /// </exception>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddServiceClient&lt;IOrdersApi&gt;("OrdersApi");
    /// </code>
    /// </example>
    public static IServiceCollection AddServiceClient<TApi>(
        this IServiceCollection services,
        string serviceName)
        where TApi : class
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentException("Service name must be provided", nameof(serviceName));
        }

        services.TryAddSingleton<IValidator<ServiceClientOptions>, ServiceClientOptionsValidator>();
        services.TryAddSingleton<IValidateOptions<ServiceClientOptions>, FluentValidateOptions<ServiceClientOptions>>();

        services.AddOptions<ServiceClientOptions>(serviceName)
            .BindConfiguration($"ServiceClients:{serviceName}")
            .ValidateOnStart();

        services.RegisterClient<TApi>(serviceName);

        return services;
    }

    /// <summary>
    /// Creates the Refit client and attaches the Polly resilience pipeline.
    /// </summary>
    /// <remarks>
    /// The resilience pipeline is built in the following order:
    /// <list type="number">
    ///   <item><description>Concurrency limiter — caps in-flight requests.</description></item>
    ///   <item><description>Outer timeout — total time allowed for the request (including all retries).</description></item>
    ///   <item><description>Retry (optional) — retries transient failures and configured status codes.</description></item>
    ///   <item><description>Circuit breaker (optional) — opens the circuit on sustained failures.</description></item>
    ///   <item><description>Per-attempt timeout — cancels a single attempt that exceeds the limit.</description></item>
    /// </list>
    /// </remarks>
    private static void RegisterClient<TApi>(
        this IServiceCollection services,
        string serviceName)
        where TApi : class
    {
        services
            .AddRefitClient<TApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>().Get(serviceName);
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddResilienceHandler(serviceName, (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptionsMonitor<ServiceClientOptions>>().Get(serviceName);

                pipeline.AddConcurrencyLimiter(options.ConcurrencyLimitOptions.Limit, options.ConcurrencyLimitOptions.QueueSize);

                pipeline.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));

                if (options.Retry.Enabled)
                {
                    pipeline.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.Retry.MaxAttempts,
                        Delay = TimeSpan.FromMilliseconds(options.Retry.BaseDelayMilliseconds),
                        BackoffType = options.Retry.DelayBackoffType,
                        UseJitter = true,
                        ShouldHandle = args => args.Outcome switch
                        {
                            { Exception: HttpRequestException } => PredicateResult.True(),
                            { Exception: TimeoutRejectedException } => PredicateResult.True(),
                            { Result: { } response } when options.Retry.RetryableStatusCodes.Contains(response.StatusCode) => PredicateResult.True(),
                            _ => PredicateResult.False()
                        }
                    });
                }

                if (options.CircuitBreaker.Enabled)
                {
                    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.CircuitBreaker.FailureRatio,
                        MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                        SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                        BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                        ShouldHandle = args => args.Outcome switch
                        {
                            { Exception: HttpRequestException } => PredicateResult.True(),
                            { Exception: TimeoutRejectedException } => PredicateResult.True(),
                            { Result: { } response } when options.CircuitBreaker.FailureStatusCodes.Contains(response.StatusCode) => PredicateResult.True(),
                            _ => PredicateResult.False()
                        }
                    });
                }

                if (options.Retry.Enabled)
                {
                    pipeline.AddTimeout(TimeSpan.FromSeconds(options.Retry.TimeoutSecondsPerRetry));
                }
            });
    }
}