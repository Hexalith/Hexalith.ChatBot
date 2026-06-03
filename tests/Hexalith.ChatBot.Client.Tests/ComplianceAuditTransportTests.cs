using System.Net;
using System.Text;

using Hexalith.ChatBot.Client.Generated;

using Shouldly;

using GeneratedClient = Hexalith.ChatBot.Client.Generated.Client;

namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Story 9.3 (deferral-2 seam): the hand-written <see cref="ComplianceAuditQuery"/> transport over the generated
/// <see cref="Client"/> must reach the S9 read endpoints with the correlation header and the FR56 filter dimensions
/// intact, parse a metadata-only success body, and — crucially — collapse any non-success (safe-not-found) response to
/// the empty <c>Denied</c>/<c>Restricted</c> views rather than throwing or leaking, so the UI renders the redacted
/// blocked state without learning whether a restricted resource exists (NFR2).
/// </summary>
public sealed class ComplianceAuditTransportTests
{
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task SearchShouldPostFr56FiltersWithCorrelationHeaderAndParseTheMetadataOnlyResult()
    {
        const string responseBody =
            """
            {
              "queryRef": "audit-query-s9",
              "rows": [
                {
                  "auditRecordRef": "audit-record-001",
                  "actorRef": "actor-alpha",
                  "actorType": "human",
                  "commandRef": "SubmitRetentionConfigurationChange",
                  "resourceRef": "audit-record-001",
                  "decision": "allow",
                  "reasonCode": "pre_commit_gate",
                  "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                  "recordedAtUtc": "2026-06-02T04:00:00+00:00",
                  "policySnapshotId": "policy-snapshot-admin-v1",
                  "redactionState": "restricted",
                  "escalationStatus": "not-requested",
                  "safeNextAction": "request-access"
                }
              ],
              "resultFingerprint": "sha256:1",
              "generatedAtUtc": "2026-06-02T05:00:00+00:00",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW"
            }
            """;
        CapturingHandler handler = new(HttpStatusCode.OK, responseBody);
        GeneratedClient client = NewClient(handler);
        ComplianceAuditQuery query = new(
            "audit-query-s9",
            [
                new ComplianceAuditFilter("audit-filter-message", "message-id", "intake-007"),
                new ComplianceAuditFilter("audit-filter-surface", "surface", "ui"),
            ],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero),
            100);

        ComplianceAuditSearchView view = await client.SearchComplianceAuditRecordsAsync(query, CorrelationId, TestContext.Current.CancellationToken);

        handler.LastMethod.ShouldBe(HttpMethod.Post);
        handler.LastPath.ShouldBe("/api/v1/compliance/audit/search");
        handler.LastCorrelationId.ShouldBe(CorrelationId);
        handler.LastRequestBody.ShouldContain("message-id");
        handler.LastRequestBody.ShouldContain("surface");

        view.QueryRef.ShouldBe("audit-query-s9");
        view.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");
        view.Rows[0].RedactionState.ShouldBe("restricted");
    }

    [Fact]
    public async Task SearchShouldCollapseSafeNotFoundResponsesToTheDeniedView()
    {
        foreach (HttpStatusCode status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound })
        {
            GeneratedClient client = NewClient(new CapturingHandler(status, "{\"code\":\"authorization_denied\"}"));

            ComplianceAuditSearchView view = await client.SearchComplianceAuditRecordsAsync(
                Query(), CorrelationId, TestContext.Current.CancellationToken);

            view.ShouldBeSameAs(ComplianceAuditSearchView.Denied);
            view.Rows.ShouldBeEmpty();
            view.QueryRef.ShouldBe("denied");
        }
    }

    [Fact]
    public async Task DetailShouldParseSuccessAndCollapseDenialsToTheRestrictedView()
    {
        const string detailBody =
            """
            {
              "auditRecordRef": "audit-record-001",
              "commandRef": "SubmitRetentionConfigurationChange",
              "resourceRef": "audit-record-001",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "recordedAtUtc": "2026-06-02T04:00:00+00:00",
              "policySnapshotId": "policy-snapshot-admin-v1",
              "redactionState": "detail-available",
              "escalationStatus": "not-requested",
              "visibleMetadataRefs": ["project:redacted-ref"],
              "safeNextAction": "view-metadata",
              "redactionReasonCode": "metadata-visible"
            }
            """;
        GeneratedClient okClient = NewClient(new CapturingHandler(HttpStatusCode.OK, detailBody));
        GeneratedClient deniedClient = NewClient(new CapturingHandler(HttpStatusCode.Forbidden, "{\"code\":\"authorization_denied\"}"));

        ComplianceAuditDetailView available = await okClient.GetComplianceAuditDetailAsync("audit-record-001", CorrelationId, TestContext.Current.CancellationToken);
        ComplianceAuditDetailView restricted = await deniedClient.GetComplianceAuditDetailAsync("audit-record-001", CorrelationId, TestContext.Current.CancellationToken);

        available.SafeNextAction.ShouldBe("view-metadata");
        available.RedactionState.ShouldBe("detail-available");
        restricted.ShouldBeSameAs(ComplianceAuditDetailView.Restricted);
        restricted.SafeNextAction.ShouldBe("request-access");
    }

    private static ComplianceAuditQuery Query()
        => new(
            "audit-query-s9",
            [new ComplianceAuditFilter("audit-filter-time", "time", "all")],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero),
            100);

    private static GeneratedClient NewClient(CapturingHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private sealed class CapturingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public string? LastPath { get; private set; }

        public string? LastCorrelationId { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastPath = request.RequestUri?.AbsolutePath;
            LastCorrelationId = request.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? values)
                ? values.SingleOrDefault()
                : null;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
