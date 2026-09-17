using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed per-class status dimension (AC3, NFR17). <see cref="FailedRetryable"/>/<see cref="FailedTerminal"/>
/// are produced by the deferred extraction runtime via the one <c>RetryFailurePolicy</c> taxonomy. Mirrors
/// <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportClassStatuses
{
    public const string Succeeded = "succeeded";
    public const string FailedRetryable = "failed-retryable";
    public const string FailedTerminal = "failed-terminal";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Succeeded, FailedRetryable, FailedTerminal], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
