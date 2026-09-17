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
