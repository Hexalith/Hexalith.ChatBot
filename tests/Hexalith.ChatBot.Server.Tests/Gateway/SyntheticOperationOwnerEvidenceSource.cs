using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Read-only synthetic owner evidence for direct validator tests, never production authority.</summary>
internal sealed class SyntheticOperationOwnerEvidenceSource : IChatBotAdmissionEvidenceSource, ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; } = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    /// <inheritdoc />
    public ChatBotClassifierRuntimeIdentity? RuntimeIdentity => null;
    /// <summary>Synthetic owner proof returned by the next read.</summary>
    public ChatBotProfileAdmissionEvidence? Proof { get; set; }
    /// <summary>Whether an owner read throws a synthetic dependency failure.</summary>
    public bool ThrowOnRead { get; set; }
    /// <summary>Optional synthetic owner failure, independent of caller input parsing.</summary>
    public Exception? ReadFailure { get; set; }
    /// <summary>Number of profile owner reads attempted.</summary>
    public int ReadCount { get; private set; }

    /// <summary>Creates an exact, current synthetic owner binding for one test operation.</summary>
    public SyntheticOperationOwnerEvidenceSource(ChatBotGatewayContext context)
    {
        Proof = new(context.TenantBinding.TenantId, context.Actor.ActorId, context.Submission.Request.CommandType, context.Submission.Request.CommandId,
            context.AdmissionProfile!.Value, 8, null,
            ChatBotCommandAdmissionCatalog.RequiredStages(context.AdmissionProfile.Value).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            UtcNow.AddSeconds(-1), UtcNow.AddMinutes(1));
    }

    /// <inheritdoc />
    public ValueTask<ChatBotClassifierQualification?> ReadQualificationAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult<ChatBotClassifierQualification?>(null);

    /// <inheritdoc />
    public ValueTask<ChatBotProfileAdmissionEvidence?> ReadProfileEvidenceAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ReadCount++;
        if (ReadFailure is not null) { throw ReadFailure; }
        return ThrowOnRead ? throw new IOException("Synthetic owner source unavailable.") : ValueTask.FromResult(Proof);
    }
}
