using System.Text.Json;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Provides infrastructure-free delegates for concrete recovery producer and cleanup regressions.</summary>
internal sealed class RecoverySandboxOperationsTestSeam
{
    /// <summary>Gets or initializes the user-token acquisition delegate.</summary>
    internal Func<CancellationToken, ValueTask<string>>? AcquireUserAccessTokenAsync { get; init; }

    /// <summary>Gets or initializes the control-token acquisition delegate.</summary>
    internal Func<CancellationToken, ValueTask<string>>? AcquireControlAccessTokenAsync { get; init; }

    /// <summary>Gets or initializes the governed-note submission delegate.</summary>
    internal Func<string, string, string, string, string, CancellationToken, Task>? SubmitUntilAcceptedAsync { get; init; }

    /// <summary>Gets or initializes the sandbox-control delegate.</summary>
    internal Func<string, string, bool, CancellationToken, string?, string, ValueTask<JsonDocument>>? SendSandboxControlAsync { get; init; }

    /// <summary>Gets or initializes the EventStore availability delegate.</summary>
    internal Func<CancellationToken, ValueTask<bool>>? IsEventStoreEndpointAvailableAsync { get; init; }

    /// <summary>Gets or initializes the metadata-only cleanup diagnostic writer.</summary>
    internal Func<string, CancellationToken, ValueTask>? WriteDiagnosticAsync { get; init; }
}
