using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The pure per-project read redaction (AC2, NFR2). When the reader lacks the compliance scope OR the record's
/// <see cref="ConsentLawfulBasisRecord.ProjectScopeRef"/> is not in the bounded
/// <see cref="ConsentLawfulBasisAuthorityView.AuthorizedProjectRefs"/>, it drops the subject locator + project ref
/// (and collapses the sensitivity to <c>metadata-only</c>), so an unauthorized read is indistinguishable from
/// safe-not-found — never the resource identity. The server <c>ConsentLawfulBasisAuthorizationPolicy</c> supplies the
/// bounded view; this function has no <c>ClaimsPrincipal</c> dependency.
/// </summary>
public static class ConsentLawfulBasisRedactionPolicy
{
    public static ConsentLawfulBasisRecord Redact(
        ConsentLawfulBasisRecord record,
        ConsentLawfulBasisAuthorityView authority)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(authority);

        bool authorized = authority.HasComplianceScope &&
            !string.IsNullOrEmpty(record.ProjectScopeRef) &&
            authority.AuthorizedProjectRefs.Contains(record.ProjectScopeRef);

        if (authorized)
        {
            return record;
        }

        // Unauthorized: drop the subject locator + project ref entirely; the redacted shape is the only signal (NFR2).
        return record with
        {
            SubjectLocator = string.Empty,
            ProjectScopeRef = string.Empty,
            RedactionSensitivity = DataClassRedactionSensitivities.MetadataOnly,
        };
    }
}
