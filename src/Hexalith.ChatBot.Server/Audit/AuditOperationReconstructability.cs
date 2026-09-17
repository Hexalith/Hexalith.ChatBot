using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The coarse reconstructability verdict for a single state-mutating operation (Story 9.2, AC1/NFR50a).</summary>
internal enum AuditOperationReconstructability
{
    /// <summary>Default/first member: an unevaluated or unmeasurable operation is honestly "not reconstructable", never a fabricated success.</summary>
    NotReconstructable,

    /// <summary>The operation rebuilds end-to-end from the chain alone (and, after AC2's diff, agrees with the projection).</summary>
    Reconstructable,
}
