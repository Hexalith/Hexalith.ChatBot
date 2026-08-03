using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Validates <see cref="LiveRecoveryValidationOptions"/> at startup by delegating to
/// <see cref="LiveRecoveryValidationOptions.Validate"/>, so the specific configuration error — not a generic message —
/// is what a deployment sees when the destructive lane is misconfigured.
/// <para>
/// Replaces the inline <c>.Validate(static options =&gt; options.Validate() is null, "generic message")</c> registration:
/// the options builder's <c>Validate</c> overload only ever surfaces the fixed failure string supplied at
/// registration time, discarding the specific reason <see cref="LiveRecoveryValidationOptions.Validate"/> already
/// computed. Implementing <see cref="IValidateOptions{TOptions}"/> lets that reason reach the
/// <see cref="OptionsValidationException"/> a fail-closed <c>ValidateOnStart()</c> throws.
/// </para>
/// </summary>
internal sealed class LiveRecoveryValidationOptionsValidator : IValidateOptions<LiveRecoveryValidationOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, LiveRecoveryValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string? failureReason = options.Validate();
        return failureReason is null
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failureReason);
    }
}
