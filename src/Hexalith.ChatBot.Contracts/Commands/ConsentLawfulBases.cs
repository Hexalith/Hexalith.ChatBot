using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed GDPR lawful-basis dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token mirroring
/// the GDPR Article 6 bases. Callers select within the set; they never invent a basis.
/// </summary>
public static class ConsentLawfulBases
{
    public const string Consent = "consent";
    public const string Contract = "contract";
    public const string LegalObligation = "legal-obligation";
    public const string VitalInterests = "vital-interests";
    public const string PublicTask = "public-task";
    public const string LegitimateInterests = "legitimate-interests";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(
            [Consent, Contract, LegalObligation, VitalInterests, PublicTask, LegitimateInterests],
            StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
