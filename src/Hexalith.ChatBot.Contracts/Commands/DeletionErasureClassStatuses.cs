using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed per-class status dimension (AC4, NFR17). <see cref="FailedRetryable"/>/<see cref="FailedTerminal"/>
/// are produced by the deferred destruction runtime via the one <c>RetryFailurePolicy</c> taxonomy. Mirrors
/// <see cref="TenantExportClassStatuses"/>.
/// </summary>
public static class DeletionErasureClassStatuses
{
    public const string Succeeded = "succeeded";
    public const string FailedRetryable = "failed-retryable";
    public const string FailedTerminal = "failed-terminal";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Succeeded, FailedRetryable, FailedTerminal], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
