using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The bounded requirement value the pure <see cref="ConsentRequirementPolicy"/> and <see cref="ConsentGate"/>
/// consume (AC4). Keys ⊆ <see cref="ConsentSubjectKinds.All"/>; each value ∈ <see cref="ConsentRequirementDispositions"/>.
/// The server seam builds this from <see cref="ConsentRequirementMatrix.Published"/> merged with any tenant override
/// (override wiring deferred — see the <c>ConsentRequirementProfileMapper</c> deferral hook).
/// </summary>
public sealed record ConsentRequirementProfile(
    IReadOnlyDictionary<string, string> DispositionsBySubjectKind);
