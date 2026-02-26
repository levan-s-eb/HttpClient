using Http.Client;
using Http.Client.Validation;

namespace Http.Client.Tests.Validation;

public class ServiceClientOptionsValidatorTests
{
    private readonly ServiceClientOptionsValidator _validator = new();

    private static ServiceClientOptions CreateValid() => new()
    {
        BaseUrl = "https://example.com",
        TimeoutSeconds = 30,
        ConcurrencyLimitOptions = new ConcurrencyLimitOptions { Limit = 100, QueueSize = 0 },
        Retry = new RetryOptions { Enabled = false },
        CircuitBreaker = new CircuitBreakerOptions { Enabled = false }
    };

    [Fact]
    public void ValidOptions_PassesValidation()
    {
        var result = _validator.Validate(CreateValid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void BaseUrl_WhenEmpty_FailsValidation(string? baseUrl)
    {
        var options = CreateValid();
        options.BaseUrl = baseUrl!;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ServiceClientOptions.BaseUrl));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///tmp/test")]
    public void BaseUrl_WhenNotHttpOrHttps_FailsValidation(string baseUrl)
    {
        var options = CreateValid();
        options.BaseUrl = baseUrl;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ServiceClientOptions.BaseUrl));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/api/v1")]
    public void BaseUrl_WhenValidHttpUrl_PassesValidation(string baseUrl)
    {
        var options = CreateValid();
        options.BaseUrl = baseUrl;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ServiceClientOptions.BaseUrl));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TimeoutSeconds_WhenZeroOrNegative_FailsValidation(int timeout)
    {
        var options = CreateValid();
        options.TimeoutSeconds = timeout;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ServiceClientOptions.TimeoutSeconds));
    }

    [Fact]
    public void TimeoutSeconds_WhenPositive_PassesValidation()
    {
        var options = CreateValid();
        options.TimeoutSeconds = 10;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ServiceClientOptions.TimeoutSeconds));
    }

    [Fact]
    public void RetryOptions_WhenDisabled_SkipsValidation()
    {
        var options = CreateValid();
        options.Retry = new RetryOptions
        {
            Enabled = false,
            MaxAttempts = -1, // Invalid but should be skipped
            BaseDelayMilliseconds = 0
        };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName.StartsWith("Retry."));
    }

    [Fact]
    public void RetryOptions_WhenEnabled_ValidatesNestedRules()
    {
        var options = CreateValid();
        options.Retry = new RetryOptions
        {
            Enabled = true,
            MaxAttempts = 0,
            BaseDelayMilliseconds = 0,
            TimeoutSecondsPerRetry = 0,
            RetryableStatusCodes = []
        };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Retry."));
    }

    [Fact]
    public void CircuitBreaker_WhenDisabled_SkipsValidation()
    {
        var options = CreateValid();
        options.CircuitBreaker = new CircuitBreakerOptions
        {
            Enabled = false,
            FailureRatio = 0, // Invalid but should be skipped
            MinimumThroughput = 0
        };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName.StartsWith("CircuitBreaker."));
    }

    [Fact]
    public void CircuitBreaker_WhenEnabled_ValidatesNestedRules()
    {
        var options = CreateValid();
        options.CircuitBreaker = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureRatio = 0,
            MinimumThroughput = 0,
            SamplingDurationSeconds = 0,
            BreakDurationSeconds = 0
        };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("CircuitBreaker."));
    }

    [Fact]
    public void TimeoutSecondsPerRetry_WhenGreaterThanOrEqualToTimeoutSeconds_FailsValidation()
    {
        var options = CreateValid();
        options.TimeoutSeconds = 5;
        options.Retry = new RetryOptions
        {
            Enabled = true,
            TimeoutSecondsPerRetry = 10
        };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Retry.TimeoutSecondsPerRetry");
    }

    [Fact]
    public void TimeoutSecondsPerRetry_WhenEqualToTimeoutSeconds_FailsValidation()
    {
        var options = CreateValid();
        options.TimeoutSeconds = 5;
        options.Retry = new RetryOptions
        {
            Enabled = true,
            TimeoutSecondsPerRetry = 5
        };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Retry.TimeoutSecondsPerRetry");
    }

    [Fact]
    public void TimeoutSecondsPerRetry_WhenLessThanTimeoutSeconds_PassesValidation()
    {
        var options = CreateValid();
        options.TimeoutSeconds = 30;
        options.Retry = new RetryOptions
        {
            Enabled = true,
            TimeoutSecondsPerRetry = 5
        };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Retry.TimeoutSecondsPerRetry");
    }

    [Fact]
    public void TimeoutSecondsPerRetry_WhenRetryDisabled_SkipsCrossValidation()
    {
        var options = CreateValid();
        options.TimeoutSeconds = 5;
        options.Retry = new RetryOptions
        {
            Enabled = false,
            TimeoutSecondsPerRetry = 60
        };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Retry.TimeoutSecondsPerRetry");
    }
}
