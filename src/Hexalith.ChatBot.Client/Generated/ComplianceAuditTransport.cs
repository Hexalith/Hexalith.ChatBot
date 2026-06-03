using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Client;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Story 9.3: a hand-written transport seam for the S9 compliance audit investigation read endpoints. These routes
/// post-date the generated OpenAPI client (and the generated <c>ComplianceAuditFilterRefFilterKey</c> enum predates
/// the FR56 <c>message-id</c>/<c>surface</c> dimensions), so the surface reaches them through this typed seam over the
/// same <see cref="System.Net.Http.HttpClient"/> rather than a regenerated client. It only reads metadata-only tokens.
/// </summary>
public partial interface IClient
{
    System.Threading.Tasks.Task<ComplianceAuditSearchView> SearchComplianceAuditRecordsAsync(
        ComplianceAuditQuery query,
        string correlationId,
        System.Threading.CancellationToken cancellationToken);

    System.Threading.Tasks.Task<ComplianceAuditDetailView> GetComplianceAuditDetailAsync(
        string auditRecordRef,
        string correlationId,
        System.Threading.CancellationToken cancellationToken);
}

public partial class Client
{
    private static readonly JsonSerializerOptions ComplianceAuditJsonOptions = new(JsonSerializerDefaults.Web);

    public async System.Threading.Tasks.Task<ComplianceAuditSearchView> SearchComplianceAuditRecordsAsync(
        ComplianceAuditQuery query,
        string correlationId,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        using System.Net.Http.HttpRequestMessage request = new(System.Net.Http.HttpMethod.Post, "api/v1/compliance/audit/search")
        {
            Content = JsonContent.Create(query, options: ComplianceAuditJsonOptions),
        };
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        using System.Net.Http.HttpResponseMessage response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        // A denial (safe-not-found) is metadata-only by construction: surface a denied, empty result rather than an
        // exception so the UI renders the redacted blocked state without leaking whether a resource exists.
        if (!response.IsSuccessStatusCode)
        {
            return ComplianceAuditSearchView.Denied;
        }

        return await response.Content
            .ReadFromJsonAsync<ComplianceAuditSearchView>(ComplianceAuditJsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? ComplianceAuditSearchView.Denied;
    }

    public async System.Threading.Tasks.Task<ComplianceAuditDetailView> GetComplianceAuditDetailAsync(
        string auditRecordRef,
        string correlationId,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditRecordRef);

        using System.Net.Http.HttpRequestMessage request = new(
            System.Net.Http.HttpMethod.Get,
            $"api/v1/compliance/audit/{Uri.EscapeDataString(auditRecordRef)}");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        using System.Net.Http.HttpResponseMessage response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return ComplianceAuditDetailView.Restricted;
        }

        return await response.Content
            .ReadFromJsonAsync<ComplianceAuditDetailView>(ComplianceAuditJsonOptions, cancellationToken)
            .ConfigureAwait(false) ?? ComplianceAuditDetailView.Restricted;
    }
}
