using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The as-shipped seed v1 consent-requirement matrix (AC1/AC4) declaring the default regulatory-profile disposition
/// per <see cref="ConsentSubjectKinds"/> member. Immutable, deterministic, token-only — mirrors
/// <see cref="DataClassInventoryCatalog.Published"/> (no <c>UtcNow</c>; fixed values). It biases every governed
/// subject kind to <c>required</c>; a future tenant-policy override may relax a kind (the override mapper is the
/// deferred server seam). The pure <see cref="ConsentRequirementPolicy"/>/<see cref="ConsentGate"/> consume it; they
/// do not redefine it.
/// </summary>
public static class ConsentRequirementMatrix
{
    public static ConsentRequirementProfile Published { get; } = new(
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.ExternalParticipant] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.RetainedContent] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.AiProcessing] = ConsentRequirementDispositions.Required,
        });
}
