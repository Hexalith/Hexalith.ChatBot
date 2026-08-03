namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Represents a fail-closed validation error with a stable reason code.
/// </summary>
public sealed class GateValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GateValidationException"/> class.
    /// </summary>
    /// <param name="reasonCode">The stable reason code.</param>
    /// <param name="subject">The metadata-only failing subject.</param>
    public GateValidationException(string reasonCode, string subject)
        : base($"{reasonCode}: {subject}")
    {
        ReasonCode = reasonCode;
        Subject = subject;
    }

    /// <summary>Gets the stable reason code.</summary>
    public string ReasonCode { get; }

    /// <summary>Gets the metadata-only failing subject.</summary>
    public string Subject { get; }
}
