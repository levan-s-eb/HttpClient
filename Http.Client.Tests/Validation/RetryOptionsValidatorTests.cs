using Http.Client.Validation;

namespace Http.Client.Tests.Validation;

public class RetryOptionsValidatorTests
{
    private readonly RetryOptionsValidator _validator = new();

    private static RetryOptions CreateValid() => new()
    {
        MaxAttempts = 3,
        BaseDelayMilliseconds = 500,
        TimeoutSecondsPerRetry = 5,
        RetryableStatusCodes = [System.Net.HttpStatusCode.TooManyRequests]
    };

    [Fact]
    public void ValidOptions_PassesValidation()
    {
        var result = _validator.Validate(CreateValid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void MaxAttempts_WhenOutOfRange_FailsValidation(int maxAttempts)
    {
        var options = CreateValid();
        options.MaxAttempts = maxAttempts;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RetryOptions.MaxAttempts));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxAttempts_WhenInRange_PassesValidation(int maxAttempts)
    {
        var options = CreateValid();
        options.MaxAttempts = maxAttempts;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RetryOptions.MaxAttempts));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BaseDelayMilliseconds_WhenZeroOrNegative_FailsValidation(int delay)
    {
        var options = CreateValid();
        options.BaseDelayMilliseconds = delay;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RetryOptions.BaseDelayMilliseconds));
    }

    [Fact]
    public void BaseDelayMilliseconds_WhenPositive_PassesValidation()
    {
        var options = CreateValid();
        options.BaseDelayMilliseconds = 100;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RetryOptions.BaseDelayMilliseconds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TimeoutSecondsPerRetry_WhenZeroOrNegative_FailsValidation(int timeout)
    {
        var options = CreateValid();
        options.TimeoutSecondsPerRetry = timeout;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RetryOptions.TimeoutSecondsPerRetry));
    }

    [Fact]
    public void TimeoutSecondsPerRetry_WhenPositive_PassesValidation()
    {
        var options = CreateValid();
        options.TimeoutSecondsPerRetry = 10;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RetryOptions.TimeoutSecondsPerRetry));
    }

    [Fact]
    public void RetryableStatusCodes_WhenEmpty_FailsValidation()
    {
        var options = CreateValid();
        options.RetryableStatusCodes = [];

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RetryOptions.RetryableStatusCodes));
    }

    [Fact]
    public void RetryableStatusCodes_WhenHasValues_PassesValidation()
    {
        var options = CreateValid();
        options.RetryableStatusCodes = [System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.TooManyRequests];

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RetryOptions.RetryableStatusCodes));
    }
}
