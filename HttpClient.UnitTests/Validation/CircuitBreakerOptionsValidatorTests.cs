using HttpClient.Validation;

namespace HttpClient.UnitTests.Validation;

public class CircuitBreakerOptionsValidatorTests
{
    private readonly CircuitBreakerOptionsValidator _validator = new();

    private static CircuitBreakerOptions CreateValid() => new()
    {
        FailureRatio = 0.1,
        MinimumThroughput = 100,
        SamplingDurationSeconds = 30,
        BreakDurationSeconds = 30,
        FailureStatusCodes = [System.Net.HttpStatusCode.InternalServerError]
    };

    [Fact]
    public void ValidOptions_PassesValidation()
    {
        var result = _validator.Validate(CreateValid());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void FailureRatio_WhenOutOfExclusiveRange_FailsValidation(double ratio)
    {
        var options = CreateValid();
        options.FailureRatio = ratio;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.FailureRatio));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(0.99)]
    public void FailureRatio_WhenWithinExclusiveRange_PassesValidation(double ratio)
    {
        var options = CreateValid();
        options.FailureRatio = ratio;

        var result = _validator.Validate(options);
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.FailureRatio));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void MinimumThroughput_WhenLessThanThreshold_FailsValidation(int throughput)
    {
        var options = CreateValid();
        options.MinimumThroughput = throughput;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.MinimumThroughput));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MinimumThroughput_WhenAtLeastThreshold_PassesValidation(int throughput)
    {
        var options = CreateValid();
        options.MinimumThroughput = throughput;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.MinimumThroughput));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void SamplingDurationSeconds_WhenNotGreaterThanThreshold_FailsValidation(int duration)
    {
        var options = CreateValid();
        options.SamplingDurationSeconds = duration;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.SamplingDurationSeconds));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(30)]
    [InlineData(60)]
    public void SamplingDurationSeconds_WhenGreaterThanThreshold_PassesValidation(int duration)
    {
        var options = CreateValid();
        options.SamplingDurationSeconds = duration;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.SamplingDurationSeconds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void BreakDurationSeconds_WhenLessThanThreshold_FailsValidation(int duration)
    {
        var options = CreateValid();
        options.BreakDurationSeconds = duration;

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.BreakDurationSeconds));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    public void BreakDurationSeconds_WhenAtLeastThreshold_PassesValidation(int duration)
    {
        var options = CreateValid();
        options.BreakDurationSeconds = duration;

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.BreakDurationSeconds));
    }

    [Fact]
    public void FailureStatusCodes_WhenEmpty_FailsValidation()
    {
        var options = CreateValid();
        options.FailureStatusCodes = [];

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.FailureStatusCodes));
    }

    [Fact]
    public void FailureStatusCodes_WhenHasValues_PassesValidation()
    {
        var options = CreateValid();
        options.FailureStatusCodes = [System.Net.HttpStatusCode.InternalServerError, System.Net.HttpStatusCode.BadGateway];

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CircuitBreakerOptions.FailureStatusCodes));
    }
}
