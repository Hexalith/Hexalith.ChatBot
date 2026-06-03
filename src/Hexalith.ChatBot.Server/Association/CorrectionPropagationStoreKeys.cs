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

    // Story 9.6 (AC1): the M2 correction-propagation scope = the four metadata-only M0 stores PLUS the vector-reindex
    // derived-store activity. A deployment that registers the vector-reindex activity runs this scope; an M0 deployment
    // without it keeps RequiredM0 behavior unchanged (backward-compatible with Story 2.8).
    public static IReadOnlyList<string> RequiredM2 { get; } = [.. RequiredM0, VectorReindex];

    public static IReadOnlySet<string> RequiredM2Set { get; } = RequiredM2.ToHashSet(StringComparer.Ordinal);
}
