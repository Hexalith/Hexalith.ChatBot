using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC3 / AC5 — the current M0 read surfaces collapse foreign, unknown, malformed, stale, missing, ambiguous,
/// and unsafe context to an indistinguishable safe denial, while a same-tenant positive control proves the
/// foreign record actually exists (so the denial is not a false pass from an unseeded store). Every case runs
/// through the REAL server (<c>WebApplicationFactory&lt;Program&gt;</c>) and the same public endpoints a UI/client
/// uses, comparing status, correlation header, and metadata-only body — never status codes alone — and routes
/// the rendered body through the shared leakage gate.
/// </summary>
public sealed class CrossTenantReadSurfaceIsolationTests
{
    // ---- governed-operations projection read ----

    [Fact]
    public async Task GovernedOperationReadShouldCollapseForeignUnknownAndMalformedToIndistinguishableSafeDenial()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        // The bound caller (default tenant-alpha) reads a real foreign note, an unknown id, and a malformed id.
        Response foreign = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId), token);
        Response unknown = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.UnknownId), token);
        Response malformed = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest("not-a-valid-ulid-zzz"), token);
        Response missingTenant = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, IsolationHttpHost.MissingTenantContext), token);
        Response ambiguousTenant = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, IsolationHttpHost.AmbiguousTenantContext), token);
        Response staleTenant = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, IsolationHttpHost.StaleTenantContext), token);
        Response unsafeTenant = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, IsolationHttpHost.UnsafeTenantContext), token);

        AssertIndistinguishableSafeDenial("governed-operation", foreign, unknown, malformed, missingTenant, ambiguousTenant, staleTenant, unsafeTenant);
    }

    [Fact]
    public async Task GovernedOperationForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        // Positive control: the foreign note demonstrably exists when read AS its owning tenant.
        Response owner = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, CrossTenantLeakageCorpus.ForeignTenant), token);
        owner.Status.ShouldBe(HttpStatusCode.OK);
        NoteIdOf(owner.Body).ShouldBe(CrossTenantLeakageCorpus.ForeignNoteId);
        // The owner's 200 body legitimately carries its own note id; scan excluding only that id.
        CrossTenantLeakageScanner.Scan("owner", "governed-operation-owner-200", owner.Body, CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.ForeignNoteId));

        // Isolation: the bound caller is denied that SAME, real, foreign note.
        Response denied = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.ForeignNoteId, CrossTenantLeakageCorpus.BoundTenant), token);
        denied.Status.ShouldBe(HttpStatusCode.Forbidden);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "governed-operation-denial", denied.Body);

        // Own-tenant positive: the bound caller CAN read its own note (proves the read path works — not a false pass).
        Response own = await SendAsync(client, IsolationHttpHost.GovernedOperationRequest(CrossTenantLeakageCorpus.OwnNoteId, CrossTenantLeakageCorpus.BoundTenant), token);
        own.Status.ShouldBe(HttpStatusCode.OK);
        NoteIdOf(own.Body).ShouldBe(CrossTenantLeakageCorpus.OwnNoteId);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "governed-operation-own-200", own.Body);
    }

    // ---- operation-status read ----

    [Fact]
    public async Task OperationStatusReadShouldCollapseForeignUnknownAndMalformedToIndistinguishableSafeDenial()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response foreign = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId), token);
        Response unknown = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.UnknownId), token);
        Response malformed = await SendAsync(client, IsolationHttpHost.OperationStatusRequest("not-a-valid-ulid-zzz"), token);
        Response missingTenant = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.MissingTenantContext), token);
        Response ambiguousTenant = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.AmbiguousTenantContext), token);
        Response staleTenant = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.StaleTenantContext), token);
        Response unsafeTenant = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.UnsafeTenantContext), token);

        AssertIndistinguishableSafeDenial("operation-status", foreign, unknown, malformed, missingTenant, ambiguousTenant, staleTenant, unsafeTenant);
    }

    [Fact]
    public async Task OperationStatusForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response owner = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.ForeignTenant), token);
        owner.Status.ShouldBe(HttpStatusCode.OK);
        OperationIdOf(owner.Body).ShouldBe(CrossTenantLeakageCorpus.ForeignOperationId);
        CrossTenantLeakageScanner.Scan("owner", "operation-status-owner-200", owner.Body, CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.ForeignOperationId));

        Response denied = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        denied.Status.ShouldBe(HttpStatusCode.Forbidden);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "operation-status-denial", denied.Body);

        Response own = await SendAsync(client, IsolationHttpHost.OperationStatusRequest(CrossTenantLeakageCorpus.OwnOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        own.Status.ShouldBe(HttpStatusCode.OK);
        OperationIdOf(own.Body).ShouldBe(CrossTenantLeakageCorpus.OwnOperationId);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "operation-status-own-200", own.Body);
    }

    // ---- audit-history read ----

    [Fact]
    public async Task AuditHistoryReadShouldCollapseForeignUnknownAndMalformedToIndistinguishableSafeDenial()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response foreign = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId), token);
        Response unknown = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.UnknownId), token);
        Response malformed = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest("not-a-valid-ulid-zzz"), token);
        Response missingTenant = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.MissingTenantContext), token);
        Response ambiguousTenant = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.AmbiguousTenantContext), token);
        Response staleTenant = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.StaleTenantContext), token);
        Response unsafeTenant = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.UnsafeTenantContext), token);

        AssertIndistinguishableSafeDenial("audit-history", foreign, unknown, malformed, missingTenant, ambiguousTenant, staleTenant, unsafeTenant);
    }

    [Fact]
    public async Task AuditHistoryForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response owner = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.ForeignTenant), token);
        owner.Status.ShouldBe(HttpStatusCode.OK);
        OperationIdOf(owner.Body).ShouldBe(CrossTenantLeakageCorpus.ForeignOperationId);
        CrossTenantLeakageScanner.Scan("owner", "audit-history-owner-200", owner.Body, CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.ForeignOperationId));

        Response denied = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        denied.Status.ShouldBe(HttpStatusCode.Forbidden);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "audit-history-denial", denied.Body);

        // Own-tenant positive: the bound caller CAN read its own operation's audit history (proves the read path
        // works on the shared status store — the foreign denial is not a false pass from an unseeded store).
        Response own = await SendAsync(client, IsolationHttpHost.AuditHistoryRequest(CrossTenantLeakageCorpus.OwnOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        own.Status.ShouldBe(HttpStatusCode.OK);
        OperationIdOf(own.Body).ShouldBe(CrossTenantLeakageCorpus.OwnOperationId);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "audit-history-own-200", own.Body);
    }

    // ---- project conversation read ----

    [Fact]
    public async Task ProjectConversationReadShouldCollapseForeignMalformedAndTenantFailuresToIndistinguishableSafeDenial()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response foreign = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project"), token);
        Response malformed = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("unsafe project value"), token);
        Response missingTenant = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", IsolationHttpHost.MissingTenantContext), token);
        Response ambiguousTenant = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", IsolationHttpHost.AmbiguousTenantContext), token);
        Response staleTenant = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", IsolationHttpHost.StaleTenantContext), token);
        Response unsafeTenant = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", IsolationHttpHost.UnsafeTenantContext), token);

        AssertIndistinguishableSafeDenial("project-conversation", foreign, malformed, missingTenant, ambiguousTenant, staleTenant, unsafeTenant);
    }

    [Fact]
    public async Task ProjectConversationForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response owner = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", CrossTenantLeakageCorpus.ForeignTenant), token);
        owner.Status.ShouldBe(HttpStatusCode.OK);
        ProjectIdOf(owner.Body).ShouldBe("foreign-project");
        // The owner's 200 body legitimately carries its OWN project id, operation id, and tenant context — the
        // project-conversation response surfaces the requester's own (kebab) tenant id so the UI can join its
        // tenant-scoped projection-changed streaming group (Story 10.6b). Scan excluding only those same-tenant
        // tokens; the BOUND (foreign-to-this-owner) tenant id remains a sentinel, so a cross-tenant id leak is still caught.
        CrossTenantLeakageScanner.Scan(
            "owner",
            "project-conversation-owner-200",
            owner.Body,
            CrossTenantLeakageCorpus.SentinelsExcluding("foreign-project", CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.ForeignTenant));

        Response denied = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("foreign-project", CrossTenantLeakageCorpus.BoundTenant), token);
        denied.Status.ShouldBe(HttpStatusCode.Forbidden);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "project-conversation-denial", denied.Body);

        Response own = await SendAsync(client, IsolationHttpHost.ProjectConversationRequest("own-project", CrossTenantLeakageCorpus.BoundTenant), token);
        own.Status.ShouldBe(HttpStatusCode.OK);
        ProjectIdOf(own.Body).ShouldBe("own-project");
        own.Body.ShouldContain("statusSummary");
        own.Body.ShouldContain("\"domain\":\"association\"");
        own.Body.ShouldContain("\"domain\":\"task\"");
        own.Body.ShouldContain("\"health\":\"unknown\"");
        own.Body.ShouldContain("sourceProviderMessageId");
        own.Body.ShouldContain($"provider-{CrossTenantLeakageCorpus.OwnOperationId}");
        own.Body.ShouldNotContain($"provider-{CrossTenantLeakageCorpus.ForeignOperationId}");
        own.Body.ShouldNotContain("commandPayload", Case.Insensitive);
        own.Body.ShouldNotContain("auditEnvelope", Case.Insensitive);
        own.Body.ShouldNotContain("localPath", Case.Insensitive);
        // The own 200 body legitimately carries the bound caller's OWN tenant context (Story 10.6b — the UI joins its
        // tenant-scoped streaming group from it). Scan excluding only that same-tenant id; the FOREIGN tenant id stays a
        // sentinel, so a cross-tenant tenant-id leak into this 200 body is still caught.
        CrossTenantLeakageScanner.Scan(
            "bound-caller",
            "project-conversation-own-200",
            own.Body,
            CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.BoundTenant));
    }

    // ---- association routing-status read ----

    [Fact]
    public async Task AssociationRoutingStatusReadShouldCollapseForeignUnknownMalformedAndTenantFailuresToIndistinguishableSafeDenial()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response foreign = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId), token);
        Response unknown = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.UnknownId), token);
        Response malformed = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest("not-a-valid-ulid-zzz"), token);
        Response missingTenant = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.MissingTenantContext), token);
        Response ambiguousTenant = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.AmbiguousTenantContext), token);
        Response staleTenant = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.StaleTenantContext), token);
        Response unsafeTenant = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, IsolationHttpHost.UnsafeTenantContext), token);

        AssertIndistinguishableSafeDenial("association-routing-status", foreign, unknown, malformed, missingTenant, ambiguousTenant, staleTenant, unsafeTenant);
    }

    [Fact]
    public async Task AssociationRoutingStatusForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller()
    {
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = IsolationHttpHost.CreateSeeded();
        using HttpClient client = factory.CreateClient();
        CancellationToken token = TestContext.Current.CancellationToken;

        Response owner = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.ForeignTenant), token);
        owner.Status.ShouldBe(HttpStatusCode.OK);
        AssociationIdOf(owner.Body).ShouldBe(CrossTenantLeakageCorpus.ForeignOperationId);
        owner.Body.ShouldContain("matchedValueDisplayToken");
        owner.Body.ShouldNotContain("\"decisionNote\":", Case.Sensitive);
        owner.Body.ShouldNotContain("\"correctionRationale\":", Case.Sensitive);
        CrossTenantLeakageScanner.Scan(
            "owner",
            "association-routing-status-owner-200",
            owner.Body,
            CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.ForeignOperationId, "foreign-project"));

        Response denied = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.ForeignOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        denied.Status.ShouldBe(HttpStatusCode.Forbidden);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "association-routing-status-denial", denied.Body);

        Response own = await SendAsync(client, IsolationHttpHost.AssociationRoutingStatusRequest(CrossTenantLeakageCorpus.OwnOperationId, CrossTenantLeakageCorpus.BoundTenant), token);
        own.Status.ShouldBe(HttpStatusCode.OK);
        AssociationIdOf(own.Body).ShouldBe(CrossTenantLeakageCorpus.OwnOperationId);
        own.Body.ShouldContain("matchedValueDisplayToken");
        own.Body.ShouldContain("metadata_only");
        own.Body.ShouldNotContain(CrossTenantLeakageCorpus.ForeignOperationId, Case.Sensitive);
        own.Body.ShouldNotContain("foreign-project", Case.Sensitive);
        own.Body.ShouldNotContain("\"decisionNote\":", Case.Sensitive);
        own.Body.ShouldNotContain("\"correctionRationale\":", Case.Sensitive);
        CrossTenantLeakageScanner.ScanAll("bound-caller", "association-routing-status-own-200", own.Body);
    }

    private static void AssertIndistinguishableSafeDenial(string surface, Response foreign, params Response[] collapsed)
    {
        foreign.Status.ShouldBe(HttpStatusCode.Forbidden);

        // Indistinguishable after allowed correlation normalization: same status, same correlation header, same body.
        collapsed.ShouldNotBeEmpty();
        foreach (Response response in collapsed)
        {
            response.Status.ShouldBe(HttpStatusCode.Forbidden);
            response.Body.ShouldBe(foreign.Body);
            response.Correlation.ShouldBe(foreign.Correlation);
        }

        using JsonDocument problem = JsonDocument.Parse(foreign.Body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");

        CrossTenantLeakageScanner.ScanAll("bound-caller", $"{surface}-denial", foreign.Body);
    }

    private static string? NoteIdOf(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("noteId").GetString();
    }

    private static string? OperationIdOf(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("operationId").GetString();
    }

    private static string? ProjectIdOf(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("projectId").GetString();
    }

    private static string? AssociationIdOf(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("associationId").GetString();
    }

    private static async Task<Response> SendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using HttpRequestMessage owned = request;
        using HttpResponseMessage response = await client.SendAsync(owned, cancellationToken).ConfigureAwait(true);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
        string correlation = response.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? values)
            ? values.Single()
            : string.Empty;
        return new Response(response.StatusCode, body, correlation);
    }

    private sealed record Response(HttpStatusCode Status, string Body, string Correlation);
}
