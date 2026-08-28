namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>Canonical Memories correction boundary carrying governed association, intake, and case identities.</summary>
internal interface ICanonicalVectorReindexer
{
    ValueTask<VectorReindexOutcome> ReindexCanonicalVectorsAsync(
        string tenantId,
        string associationId,
        string intakeId,
        string correctionId,
        long sourceVersion,
        string correctedCaseId,
        string? remoteOperationId,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken);
}
