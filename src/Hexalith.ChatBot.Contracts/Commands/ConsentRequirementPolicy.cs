using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The pure requirement-decision function (AC1/AC4). Looks up <paramref name="subjectKind"/> in the bounded profile;
/// an UNKNOWN subject kind or a missing/empty profile entry biases to <see cref="ConsentRequirementDispositions.Required"/>
/// (fail-closed, AC4). It carries no <c>ClaimsPrincipal</c> dependency and is a real, testable function.
/// </summary>
public static class ConsentRequirementPolicy
{
    public static string Evaluate(string? subjectKind, ConsentRequirementProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Fail-closed: an unknown subject kind is always treated as requiring a basis.
        if (!ConsentSubjectKinds.Contains(subjectKind))
        {
            return ConsentRequirementDispositions.Required;
        }

        // Fail-closed: a missing/empty/unrecognized disposition entry biases to required.
        if (profile.DispositionsBySubjectKind is null ||
            !profile.DispositionsBySubjectKind.TryGetValue(subjectKind!, out string? disposition) ||
            !ConsentRequirementDispositions.Contains(disposition))
        {
            return ConsentRequirementDispositions.Required;
        }

        return disposition;
    }
}
