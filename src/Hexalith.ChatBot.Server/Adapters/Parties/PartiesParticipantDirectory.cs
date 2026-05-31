using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.Parties.Client.Abstractions;
using Hexalith.Parties.Contracts.Models;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal sealed class PartiesParticipantDirectory(IPartiesQueryClient parties) : IParticipantDirectory
{
    public async ValueTask<ParticipantDirectoryResolution> ResolveEmailEvidenceAsync(
        ParticipantDirectoryLookup lookup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        if (!TryCanonicalizeEmailEvidence(lookup.AddressEvidence, out string? canonicalEmail))
        {
            return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.InvalidEvidence);
        }

        try
        {
            PagedResult<PartySearchResult> results = await parties
                .SearchPartiesAsync(
                    canonicalEmail,
                    page: 1,
                    pageSize: 2,
                    cancellationToken,
                    mode: "email",
                    caseId: lookup.TenantId,
                    requestCustomizer: (request, requestCancellationToken) =>
                    {
                        requestCancellationToken.ThrowIfCancellationRequested();
                        _ = request.Headers.TryAddWithoutValidation("X-Hexalith-Tenant-Id", lookup.TenantId);
                        return ValueTask.CompletedTask;
                    })
                .ConfigureAwait(false);

            if (IsDegraded(results.Freshness))
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.DirectoryDegraded);
            }

            if (results.Items.Count == 0)
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.NotFound);
            }

            if (results.Items.Count > 1 || results.TotalCount > 1)
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.AmbiguousMatch);
            }

            PartyIndexEntry index = results.Items[0].Party;
            if (index.IsErased)
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.ErasedParty);
            }

            if (TryReadTenantFromPartyId(index.Id, out string? partyTenant) &&
                !string.Equals(partyTenant, lookup.TenantId, StringComparison.Ordinal))
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.TenantMismatch);
            }

            PartyDetail detail = await parties
                .GetPartyAsync(
                    index.Id,
                    cancellationToken,
                    requestCustomizer: (request, requestCancellationToken) =>
                    {
                        requestCancellationToken.ThrowIfCancellationRequested();
                        _ = request.Headers.TryAddWithoutValidation("X-Hexalith-Tenant-Id", lookup.TenantId);
                        return ValueTask.CompletedTask;
                    })
                .ConfigureAwait(false);

            if (detail.IsErased)
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.ErasedParty);
            }

            if (detail.IsRestricted)
            {
                return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.RestrictedParty);
            }

            return ParticipantDirectoryResolution.FromResolved(
                new ResolvedMailboxParticipantReference(
                    lookup.SourceParticipantId,
                    detail.Id,
                    partyTenant ?? lookup.TenantId,
                    lookup.EvidenceReference,
                    lookup.EvidenceFingerprint,
                    ParticipantResolutionStatus.Resolved));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return ParticipantDirectoryResolution.FromUnresolved(lookup, ParticipantResolutionBlockedReason.DirectoryUnavailable);
        }
    }

    internal static bool TryCanonicalizeEmailEvidence(string? value, [NotNullWhen(true)] out string? canonical)
    {
        canonical = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().Normalize(NormalizationForm.FormC).ToLower(CultureInfo.InvariantCulture);
        int at = normalized.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == normalized.Length - 1 || normalized.IndexOf('@', at + 1) >= 0)
        {
            return false;
        }

        canonical = normalized;
        return true;
    }

    private static bool IsDegraded(ProjectionFreshnessMetadata? freshness)
        => freshness?.Status is ProjectionFreshnessStatus.Stale or
            ProjectionFreshnessStatus.Rebuilding or
            ProjectionFreshnessStatus.Degraded or
            ProjectionFreshnessStatus.Unavailable;

    private static bool TryReadTenantFromPartyId(string partyId, out string? tenantId)
    {
        tenantId = null;
        string[] parts = partyId.Split(':', 3);
        if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        tenantId = parts[0];
        return true;
    }
}
