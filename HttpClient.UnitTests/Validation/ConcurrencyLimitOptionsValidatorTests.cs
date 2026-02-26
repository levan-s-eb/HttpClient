using Http.Client;
using Http.Client.Validation;

namespace Http.Client.Tests.Validation;

public class ConcurrencyLimitOptionsValidatorTests
{
    private readonly ConcurrencyLimitOptionsValidator _validator = new();

    [Fact]
    public void DefaultOptions_PassesValidation()
    {
        var result = _validator.Validate(new ConcurrencyLimitOptions());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Limit_WhenZeroOrNegative_FailsValidation(int limit)
    {
        var options = new ConcurrencyLimitOptions { Limit = limit };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConcurrencyLimitOptions.Limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(10000)]
    public void Limit_WhenPositive_PassesValidation(int limit)
    {
        var options = new ConcurrencyLimitOptions { Limit = limit };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ConcurrencyLimitOptions.Limit));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void QueueSize_WhenNegative_FailsValidation(int queueSize)
    {
        var options = new ConcurrencyLimitOptions { QueueSize = queueSize };

        var result = _validator.Validate(options);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ConcurrencyLimitOptions.QueueSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void QueueSize_WhenZeroOrPositive_PassesValidation(int queueSize)
    {
        var options = new ConcurrencyLimitOptions { QueueSize = queueSize };

        var result = _validator.Validate(options);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ConcurrencyLimitOptions.QueueSize));
    }
}
