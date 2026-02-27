using FluentValidation;
using Microsoft.Extensions.Options;

namespace HttpClient.Validation;

/// <summary>
/// Bridges FluentValidation validators into the <see cref="IValidateOptions{TOptions}"/>
/// pipeline so that <see cref="ServiceClientOptions"/> (and any other options type) are
/// validated automatically by the .NET Options framework.
/// </summary>
/// <typeparam name="T">The options type to validate.</typeparam>
/// <param name="validator">
/// The FluentValidation <see cref="IValidator{T}"/> resolved from the DI container.
/// </param>
/// <remarks>
/// When <see cref="Microsoft.Extensions.Options.OptionsBuilder{TOptions}.ValidateOnStart"/>
/// is configured, this adapter runs the FluentValidation rules at application startup.
/// Validation errors are prefixed with the named options section (e.g., <c>[OrdersApi]</c>)
/// to make it straightforward to locate the offending configuration.
/// </remarks>
public sealed class FluentValidateOptions<T>(IValidator<T> validator) : IValidateOptions<T> where T : class
{
    /// <summary>
    /// Validates the given <paramref name="options"/> instance using the injected
    /// FluentValidation validator.
    /// </summary>
    /// <param name="name">
    /// The named options instance (e.g., the service name). Included in error messages
    /// to identify which configuration section failed validation.
    /// </param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success"/> when all rules pass; otherwise a
    /// <see cref="ValidateOptionsResult"/> containing one error string per failed rule.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = validator.Validate(options);

        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = result.Errors.Select(e => $"[{name}] {e.PropertyName}: {e.ErrorMessage}");

        return ValidateOptionsResult.Fail(errors);
    }
}
