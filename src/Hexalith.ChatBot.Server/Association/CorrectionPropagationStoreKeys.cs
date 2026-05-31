namespace Hexalith.ChatBot.Server.Association;

internal static class CorrectionPropagationStoreKeys
{
    public const string AssociationRouting = "association-routing";
    public const string EvidenceSnapshot = "evidence-snapshot";
    public const string OperationStatus = "operation-status";
    public const string AiContextReadiness = "ai-context-readiness";
    public const string VectorReindex = "vector-reindex";

    public static IReadOnlyList<string> RequiredM0 { get; } =
    [
        AssociationRouting,
        EvidenceSnapshot,
        OperationStatus,
        AiContextReadiness,
    ];

    public static IReadOnlySet<string> RequiredM0Set { get; } = RequiredM0.ToHashSet(StringComparer.Ordinal);
}
