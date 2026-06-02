namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only inbound authenticity snapshot captured from provider-supplied headers.
/// </summary>
/// <param name="AuthenticationResults">Provider-supplied authentication verdict tokens.</param>
/// <param name="HeaderInspection">Selected internet header presence and disagreement metadata.</param>
/// <param name="StrictnessPolicy">Optional tenant authenticity strictness snapshot.</param>
public sealed record MailboxAuthenticityMetadata(
    MailboxAuthenticationResultSnapshot AuthenticationResults,
    MailboxHeaderInspectionSnapshot HeaderInspection,
    MailboxAuthenticityStrictnessPolicySnapshot? StrictnessPolicy = null);
