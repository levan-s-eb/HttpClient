using Polly;
using System.Net;

namespace Http.Client;

/// <summary>
/// Top-level configuration for a named HTTP service client.
/// Bind this from the <c>ServiceClients:{ServiceName}</c> section in appsettings.
/// </summary>
/// <remarks>
/// Each registered service client gets its own named instance of these options,
/// allowing multiple downstream services to be configured independently.
/// All options are validated at startup via <see cref="Validation.ServiceClientOptionsValidator"/>.
/// </remarks>
/// <example>
/// <code>
/// // appsettings.json
/// {
///   "ServiceClients": {
///     "OrdersApi": {
///       "BaseUrl": "https://orders.internal.example.com",
///       "TimeoutSeconds": 15,
///       "ConcurrencyLimitOptions": { "Limit": 200, "QueueSize": 50 },
///       "Retry": { "Enabled": true, "MaxAttempts": 3 },
///       "CircuitBreaker": { "Enabled": true, "FailureRatio": 0.25 }
///     }
///   }
/// }
/// </code>
/// </example>
public sealed class ServiceClientOptions
{
    /// <summary>
    /// The absolute base URL of the downstream service (must use <c>http</c> or <c>https</c> scheme).
    /// </summary>
    public string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Overall request timeout in seconds applied to the entire resilience pipeline.
    /// This acts as the outer timeout — if retries are enabled, individual attempts use
    /// <see cref="RetryOptions.TimeoutSecondsPerRetry"/> instead.
    /// </summary>
    /// <value>Defaults to <c>30</c> seconds.</value>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Concurrency limiter settings that cap the number of concurrent outbound requests.
    /// </summary>
    public ConcurrencyLimitOptions ConcurrencyLimitOptions { get; set; } = new();

    /// <summary>
    /// Retry policy settings. Set <see cref="RetryOptions.Enabled"/> to <see langword="true"/>
    /// to activate automatic retries for transient failures.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker settings. Set <see cref="CircuitBreakerOptions.Enabled"/> to
    /// <see langword="true"/> to activate circuit-breaking behaviour on sustained failures.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Controls the concurrency limiter that restricts how many outbound HTTP requests
/// can be in flight simultaneously for a given service client.
/// </summary>
/// <remarks>
/// The limiter is always active. When the <see cref="Limit"/> is reached, additional
/// requests are queued up to <see cref="QueueSize"/>. Once the queue is also full,
/// requests are rejected immediately.
/// </remarks>
public sealed class ConcurrencyLimitOptions
{
    /// <summary>
    /// Maximum number of concurrent requests allowed. Must be greater than <c>0</c>.
    /// </summary>
    /// <value>Defaults to <c>1000</c>.</value>
    public int Limit { get; set; } = 1000;

    /// <summary>
    /// Maximum number of requests that can wait in the queue when
    /// <see cref="Limit"/> is reached. Set to <c>0</c> to disable queuing.
    /// </summary>
    /// <value>Defaults to <c>0</c> (no queuing).</value>
    public int QueueSize { get; set; } = 0;
}

/// <summary>
/// Retry policy settings for a service client.
/// When <see cref="Enabled"/> is <see langword="true"/>, transient HTTP failures and
/// responses with status codes in <see cref="RetryableStatusCodes"/> are retried
/// automatically.
/// </summary>
/// <remarks>
/// Validation rules are only enforced when <see cref="Enabled"/> is <see langword="true"/>.
/// The retry strategy supports configurable backoff via <see cref="DelayBackoffType"/>
/// and adds jitter to prevent thundering-herd problems.
/// </remarks>
public sealed class RetryOptions
{
    /// <summary>
    /// Whether the retry policy is active for this service client.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum number of retry attempts (not counting the initial request).
    /// Must be between <c>1</c> and <c>10</c> inclusive.
    /// </summary>
    /// <value>Defaults to <c>3</c>.</value>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds, before backoff and jitter are applied.
    /// Must be greater than <c>0</c>.
    /// </summary>
    /// <value>Defaults to <c>500</c> ms.</value>
    public int BaseDelayMilliseconds { get; set; } = 500;

    /// <summary>
    /// The backoff strategy used to increase delay between successive retries.
    /// </summary>
    /// <value>Defaults to <see cref="DelayBackoffType.Constant"/>.</value>
    public DelayBackoffType DelayBackoffType { get; set; } = DelayBackoffType.Constant;

    /// <summary>
    /// HTTP status codes that should trigger a retry.
    /// The list must contain at least one entry when retries are enabled.
    /// </summary>
    /// <value>Defaults to <c>[429 TooManyRequests]</c>.</value>
    public List<HttpStatusCode> RetryableStatusCodes { get; set; } = [ HttpStatusCode.TooManyRequests ];

    /// <summary>
    /// Per-attempt timeout in seconds. Each individual retry attempt is cancelled
    /// if it exceeds this duration.
    /// Must be greater than <c>0</c>.
    /// </summary>
    /// <value>Defaults to <c>5</c> seconds.</value>
    public int TimeoutSecondsPerRetry { get; set; } = 5;
}

/// <summary>
/// Circuit breaker settings for a service client.
/// When <see cref="Enabled"/> is <see langword="true"/>, the circuit opens after a
/// sustained failure rate is detected, temporarily blocking all outbound requests
/// to the downstream service.
/// </summary>
/// <remarks>
/// Validation rules are only enforced when <see cref="Enabled"/> is <see langword="true"/>.
/// The circuit breaker uses a sliding window (<see cref="SamplingDurationSeconds"/>)
/// to calculate the current failure ratio.
/// </remarks>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Whether the circuit breaker policy is active for this service client.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The failure-to-success ratio threshold that triggers the circuit to open.
    /// Must be exclusively between <c>0</c> and <c>1</c> (e.g., <c>0.1</c> = 10 %).
    /// </summary>
    /// <value>Defaults to <c>0.1</c> (10 %).</value>
    public double FailureRatio { get; set; } = 0.1;

    /// <summary>
    /// Minimum number of requests within the sampling window before the failure ratio
    /// is evaluated. Prevents the circuit from tripping on low-traffic services.
    /// Must be at least <c>10</c>.
    /// </summary>
    /// <value>Defaults to <c>100</c>.</value>
    public int MinimumThroughput { get; set; } = 100;

    /// <summary>
    /// Duration of the sliding sampling window in seconds over which the failure
    /// ratio is calculated. Must be greater than <c>10</c>.
    /// </summary>
    /// <value>Defaults to <c>30</c> seconds.</value>
    public int SamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Duration in seconds the circuit stays open before transitioning to half-open
    /// and allowing a probe request. Must be at least <c>5</c>.
    /// </summary>
    /// <value>Defaults to <c>30</c> seconds.</value>
    public int BreakDurationSeconds { get; set; } = 30;

    /// <summary>
    /// HTTP status codes that count as failures for the circuit breaker.
    /// Responses with these status codes increment the failure counter.
    /// The list must contain at least one entry when the circuit breaker is enabled.
    /// </summary>
    /// <value>Defaults to <c>[429 TooManyRequests, 500 InternalServerError, 502 BadGateway, 503 ServiceUnavailable]</c>.</value>
    public List<HttpStatusCode> FailureStatusCodes { get; set; } =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable
    ];
}