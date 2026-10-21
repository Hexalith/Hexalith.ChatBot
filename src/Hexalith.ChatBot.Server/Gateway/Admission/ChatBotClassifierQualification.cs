namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Immutable independently governed qualification evidence; never deserialized from a command.</summary>
/// <param name="Identity">The exact qualified artifact, candidate, dependencies, environment and dataset.</param>
/// <param name="ComputedStatus">Current registry-computed gate status.</param>
/// <param name="OwnerId">Accountable qualification owner.</param>
/// <param name="IndependentApproverId">Independent authorized approver.</param>
/// <param name="ApprovedAt">Approval timestamp.</param>
/// <param name="ExpiresAt">Explicit evidence expiry.</param>
/// <param name="EvaluatedCount">Determinate examples including both classes and all six effect partitions.</param>
/// <param name="MisclassificationCount">All classifier mismatches.</param>
/// <param name="BoundaryDowngradeCount">Boundary-crossing examples classified low risk.</param>
/// <param name="InvalidOutputCount">Invalid or indeterminate outputs.</param>
/// <param name="CoveredEffectClasses">Independently adjudicated mandatory-effect partitions.</param>
/// <param name="EvidenceReferences">Registry-verified immutable evaluation and signature references.</param>
internal sealed record ChatBotClassifierQualification(
    ChatBotClassifierRuntimeIdentity Identity,
    string ComputedStatus,
    string OwnerId,
    string IndependentApproverId,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt,
    int EvaluatedCount,
    int MisclassificationCount,
    int BoundaryDowngradeCount,
    int InvalidOutputCount,
    IReadOnlySet<Contracts.Enums.AiActionRiskActionClass> CoveredEffectClasses,
    IReadOnlyList<string> EvidenceReferences);
