using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Synthetic immutable qualification and owner proofs for pure validator tests; no release authority.</summary>
internal sealed class SyntheticAdmissionEvidenceSource : IChatBotAdmissionEvidenceSource, ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    /// <inheritdoc />
    public ChatBotClassifierRuntimeIdentity? RuntimeIdentity { get; set; } = new(
        AiActionRiskClassifier.CurrentVersion, "synthetic-artifact-hash", "synthetic-candidate",
        new Dictionary<string, string> { ["owner"] = "synthetic-dependency" }, "synthetic-test-profile", "synthetic-dataset-v1", "synthetic-dataset-hash");
    /// <summary>Gets or sets the synthetic qualification.</summary>
    public ChatBotClassifierQualification? Qualification { get; set; }
    /// <summary>Gets or sets the synthetic profile evidence.</summary>
    public ChatBotProfileAdmissionEvidence? ProfileEvidence { get; set; }

    /// <summary>Creates an isolated synthetic test fixture.</summary>
    public SyntheticAdmissionEvidenceSource()
    {
        Qualification = new(RuntimeIdentity!, "approved-current", "synthetic-owner", "synthetic-independent-approver",
            UtcNow.AddMinutes(-1), UtcNow.AddMinutes(30), 1000, 0, 0, 0,
            Enum.GetValues<AiActionRiskActionClass>().ToHashSet(), ["synthetic:qualification-signatures-and-evaluation"]);
    }
    /// <inheritdoc />
    public ValueTask<ChatBotClassifierQualification?> ReadQualificationAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(Qualification);
    /// <inheritdoc />
    public ValueTask<ChatBotProfileAdmissionEvidence?> ReadProfileEvidenceAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(ProfileEvidence);
}
