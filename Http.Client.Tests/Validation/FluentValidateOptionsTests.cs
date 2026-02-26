using Http.Client;
using Http.Client.Validation;

namespace Http.Client.Tests.Validation;

public class FluentValidateOptionsTests
{
    private readonly ServiceClientOptionsValidator _innerValidator = new();

    private static ServiceClientOptions CreateValid() => new()
    {
        BaseUrl = "https://example.com",
        TimeoutSeconds = 30,
        ConcurrencyLimitOptions = new ConcurrencyLimitOptions { Limit = 100, QueueSize = 0 },
        Retry = new RetryOptions { Enabled = false },
        CircuitBreaker = new CircuitBreakerOptions { Enabled = false }
    };

    [Fact]
    public void Validate_WhenValid_ReturnsSuccess()
    {
        var validateOptions = new FluentValidateOptions<ServiceClientOptions>(_innerValidator);

        var result = validateOptions.Validate("TestSection", CreateValid());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenInvalid_ReturnsFailWithErrors()
    {
        var validateOptions = new FluentValidateOptions<ServiceClientOptions>(_innerValidator);
        var options = CreateValid();
        options.BaseUrl = "";

        var result = validateOptions.Validate("TestSection", options);

        Assert.True(result.Failed);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures, e => e.Contains("[TestSection]") && e.Contains("BaseUrl"));
    }

    [Fact]
    public void Validate_WhenNullName_IncludesNullInError()
    {
        var validateOptions = new FluentValidateOptions<ServiceClientOptions>(_innerValidator);
        var options = CreateValid();
        options.BaseUrl = "";

        var result = validateOptions.Validate(null, options);

        Assert.True(result.Failed);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures, e => e.Contains("[]"));
    }
}
