using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

using Microsoft.AspNetCore.Http;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Always-run contracts for the live Tier-3 startup boundary.</summary>
public sealed class LiveContinuityAspireE2eContractTests
{
    /// <summary>Accepts the exact mailbox client, tenant, and operation used by the owner grants.</summary>
    [Fact]
    public void MailboxBearerWithExpectedIdentityShouldBeAccepted()
        => RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity(MailboxToken(new Dictionary<string, string>
        {
            ["chatbot:service-client-id"] = RecoveryValidationTopology.MailboxClientId,
            ["eventstore:tenant"] = RecoveryValidationTopology.StorageTenantRef,
            ["chatbot:service-client-command"] = RecoveryMailboxOwnerProvisioner.ProbeOperation,
        }));

    /// <summary>Rejects a wrong claim without disclosing the bearer or the unexpected identity.</summary>
    /// <param name="claim">The claim replaced by a mismatched value.</param>
    [Theory]
    [InlineData("chatbot:service-client-id")]
    [InlineData("eventstore:tenant")]
    [InlineData("chatbot:service-client-command")]
    public void MailboxBearerWithMismatchedIdentityShouldBeRejected(string claim)
    {
        const string privateValue = "private-unexpected-identity";
        Dictionary<string, string> claims = new()
        {
            ["chatbot:service-client-id"] = RecoveryValidationTopology.MailboxClientId,
            ["eventstore:tenant"] = RecoveryValidationTopology.StorageTenantRef,
            ["chatbot:service-client-command"] = RecoveryMailboxOwnerProvisioner.ProbeOperation,
        };
        claims[claim] = privateValue;
        string token = MailboxToken(claims);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity(token));

        exception.Message.ShouldBe("The mailbox token identity does not match the recovery mailbox.");
        exception.ToString().ShouldNotContain(token);
        exception.ToString().ShouldNotContain(privateValue);
    }

    /// <summary>Reports invalid syntax and missing claim evidence with fixed diagnostics.</summary>
    /// <param name="payload">The malformed or incomplete token payload.</param>
    /// <param name="encodePayload">Whether to encode the payload as base64url.</param>
    /// <param name="expectedMessage">The bounded public diagnostic.</param>
    [Theory]
    [InlineData("private%", false, "The mailbox token JWT payload is invalid.")]
    [InlineData("private invalid JSON", true, "The mailbox token JWT payload is invalid.")]
    [InlineData("[\"private-value\"]", true, "The mailbox token JWT payload is invalid.")]
    [InlineData("{\"other-claim\":\"private-value\"}", true, "The mailbox token is missing a required identity claim.")]
    [InlineData("{\"chatbot:service-client-id\":42}", true, "The mailbox token is missing a required identity claim.")]
    public void MalformedMailboxBearerShouldHaveBoundedDiagnostic(string payload, bool encodePayload, string expectedMessage)
    {
        string token = $"header.{(encodePayload ? Base64Url(payload) : payload)}.signature";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity(token));

        exception.Message.ShouldBe(expectedMessage);
        exception.InnerException.ShouldBeNull();
        exception.ToString().ShouldNotContain(token);
        exception.ToString().ShouldNotContain(payload);
    }

    /// <summary>Decoding invalid Unicode in a claim must not expose the decoder exception or token contents.</summary>
    /// <param name="invalidUtf8">Whether the payload contains invalid UTF-8 instead of an unpaired JSON surrogate.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MailboxBearerWithInvalidClaimUnicodeShouldHideDecoderDetails(bool invalidUtf8)
    {
        byte[] payload = invalidUtf8
            ? [.. "{\"chatbot:service-client-id\":\"private"u8, 0xFF, .. "\"}"u8]
            : """{"chatbot:service-client-id":"private\uD800"}"""u8.ToArray();
        string encoded = Base64Url(payload);
        string token = $"header.{encoded}.signature";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity(token));

        exception.Message.ShouldBe("The mailbox token JWT payload is invalid.");
        exception.InnerException.ShouldBeNull();
        exception.ToString().ShouldNotContain(token);
        exception.ToString().ShouldNotContain(encoded);
        exception.ToString().ShouldNotContain("private");
    }

    private static string MailboxToken(IReadOnlyDictionary<string, string> claims)
        => $"header.{Base64Url(JsonSerializer.Serialize(claims))}.signature";

    private static string Base64Url(string value)
        => Base64Url(Encoding.UTF8.GetBytes(value));

    private static string Base64Url(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Theory]
    [InlineData(RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.PostRecoveryPhase)]
    public void ControlledLossRouteUsesOnlyClosedNotificationPhases(string phase)
    {
        string identity = RecoveryNotificationIdentity.Compose(
            "provider-message",
            RecoveryNotificationIdentity.ControlledLossLane,
            phase);

        identity.ShouldBe($"provider-message-controlled-loss-{phase}");
    }

    [Fact]
    public void ControlledLossRouteRejectsOpenEndedNotificationPhase()
        => Should.Throw<InvalidOperationException>(() => RecoveryNotificationIdentity.Compose(
            "provider-message",
            RecoveryNotificationIdentity.ControlledLossLane,
            "caller-supplied"));

    [Theory]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.ContinuityLane, RecoveryNotificationIdentity.PostRecoveryPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.PreFaultPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.LossPhase)]
    [InlineData(RecoveryNotificationIdentity.GraphLane, RecoveryNotificationIdentity.PostRecoveryPhase)]
    [InlineData(RecoveryNotificationIdentity.ControlledLossLane, RecoveryNotificationIdentity.CheckpointPhase)]
    [InlineData(RecoveryNotificationIdentity.ControlledLossLane, RecoveryNotificationIdentity.RecoveryPhase)]
    public void NotificationPhasesCannotCrossClosedLaneOwnership(string lane, string phase)
        => Should.Throw<InvalidOperationException>(() => RecoveryNotificationIdentity.Compose(
            "provider-message",
            lane,
            phase));

    [Theory]
    [InlineData(null, 300)]
    [InlineData("265", 265)]
    [InlineData(" 265 ", 265)]
    [InlineData("1", 1)]
    [InlineData("300", 300)]
    public void RecoveryWorkflowTimeoutHonorsTheBoundedCompletionOverride(string? configured, int expectedMinutes)
    {
        LiveRecoverySweep.RecoveryWorkflowTimeout(configured)
            .ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("not-a-number")]
    public void RecoveryWorkflowTimeoutRejectsValuesOutsideTheRunnerBudget(string configured)
    {
        Should.Throw<InvalidOperationException>(() =>
            LiveRecoverySweep.RecoveryWorkflowTimeout(configured));
    }

    [Fact]
    public void HostedControlledLossBudgetCanMeasurePastRpoTargetWithinSmallestWorkflowWindow()
    {
        LiveRecoveryValidationOptions options = new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "schema-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "test-secret",
            PerScenarioTimeout = TimeSpan.FromMinutes(20),
            RestorationTimeout = TimeSpan.FromMinutes(3),
            WorkflowTimeout = TimeSpan.FromMinutes(250),
            EvidenceDirectory = Path.GetTempPath(),
            EvidenceLocator = "artifact://live-recovery/test",
        };

        options.PerScenarioTimeout.ShouldBeGreaterThan(RecoveryTargets.MaxRpo);
        options.Validate().ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ControlledRunnerOrSinkFailureWritesFourthJobMarkerBeforeRethrow(bool sinkFails)
    {
        InvalidOperationException primary = new(sinkFails ? "sink failed" : "runner failed");
        CapturingRetentionFailureSink markerSink = new();

        InvalidOperationException observed = await Should.ThrowAsync<InvalidOperationException>(() =>
            LiveRecoverySweep.RunControlledLossAndRetainAsync(
                sinkFails
                    ? _ => ValueTask.FromResult<ControlledLossPathReport>(null!)
                    : _ => ValueTask.FromException<ControlledLossPathReport>(primary),
                (_, _) => sinkFails ? ValueTask.FromException(primary) : ValueTask.CompletedTask,
                markerSink,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken).AsTask());

        observed.ShouldBeSameAs(primary);
        RecoveryValidationEvidenceRetentionFailureMarker marker = markerSink.Markers.ShouldHaveSingleItem();
        marker.JobId.ShouldBe(LiveRecoveryValidationJobs.ControlledLossPath);
        marker.Scenario.ShouldBe(ControlledLossPathReport.SubscriptionNotificationRejectionScenario);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]

    // The three statuses the enumeration exists for. Without them, restoring the `>= 500` catch-all passed all
    // nine original rows, so the change's whole point was asserted only by prose.
    [InlineData(HttpStatusCode.NotImplemented, false)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, false)]
    [InlineData(HttpStatusCode.InsufficientStorage, true)]
    public void MailboxAdmissionStartupStatusClassificationIsFailClosed(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        LiveRecoverySweep.IsTransientMailboxAdmissionStatus(statusCode).ShouldBe(expectedTransient);
    }

    /// <summary>
    /// Canonical-execution-unavailable and historical dispatch-unavailable prove the mailbox caller cleared the
    /// authentication and authority checks. Neither proves mutation admission; audit or authorization failures,
    /// malformed documents and matching catalog codes under unrelated types prove nothing.
    /// </summary>
    /// <param name="problemDetails">The verbatim response body.</param>
    /// <param name="expected">Whether the body proves the caller was admitted.</param>
    [Theory]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        true)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/audit-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        false)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/authorization-denied\",\"status\":403,\"code\":\"authorization_denied\"}",
        false)]
    [InlineData("{\"code\":\"audit_unavailable\"}", false)]
    [InlineData("{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable-x\"}", false)]
    [InlineData("[\"dispatch-unavailable\"]", false)]
    [InlineData("not json at all", false)]
    [InlineData("", false)]
    [InlineData("{\"type\":\"https://hexalith.dev/errors/chatbot/canonical-execution-unavailable\",\"status\":503}", true)]
    [InlineData("{\"type\":\"https://hexalith.dev/errors/chatbot/canonical-execution-unavailable-x\"}", false)]
    public void MailboxAdmissionProofRequiresAPostAuthorizationUnavailableProblemType(string problemDetails, bool expected)
        => LiveRecoverySweep.IsPostAuthorizationUnavailableProblem(problemDetails).ShouldBe(expected);

    /// <summary>
    /// The live authenticated admission probe is only meaningful when its body passes the OpenAPI request contract,
    /// which is validated after authentication and before admission: an authenticated contract-invalid body answers
    /// the validation 400, which proves authentication but nothing about admission. The forged-bearer probe's 401
    /// precedes validation and does not depend on the body. Drives the PRODUCTION request adapter over the exact
    /// probe body.
    /// </summary>
    [Fact]
    public async Task MailboxAdmissionProbeBodyShouldPassTheProductionRequestContract()
    {
        const string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        DefaultHttpContext context = new();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(LiveRecoverySweep.MailboxAdmissionProbeBody(commandId)));

        (CommandSubmissionRequest? request, string? origin) = await CommandSubmissionContractAdapter
            .ReadAsync(context, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        CommandSubmissionRequest admitted = request.ShouldNotBeNull();
        admitted.CommandId.ShouldBe(commandId);
        admitted.CommandType.ShouldBe("CaptureMailboxMessageIntake");
        origin.ShouldBe("mailbox");
    }

    /// <summary>
    /// The mailbox proof requires BOTH a recognized post-authorization problem type AND a 503; the body predicate alone
    /// would accept that document under any status, which is not what the production check does.
    /// </summary>
    /// <param name="statusCode">The status observed alongside the problem body.</param>
    /// <param name="canonical">Whether to use the current canonical-execution boundary or historical dispatch boundary.</param>
    /// <param name="expectedAdmissionProof">Whether that pair proves admission.</param>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, false, true)]
    [InlineData(HttpStatusCode.InternalServerError, false, false)]
    [InlineData(HttpStatusCode.BadGateway, false, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true, true)]
    [InlineData(HttpStatusCode.InternalServerError, true, false)]
    [InlineData(HttpStatusCode.BadGateway, true, false)]
    public void MailboxAdmissionProofRequiresServiceUnavailableAlongsideTheProblemType(
        HttpStatusCode statusCode,
        bool canonical,
        bool expectedAdmissionProof)
    {
        string body = JsonSerializer.Serialize(new
        {
            type = canonical ? ChatBotProblemTypes.CanonicalExecutionUnavailable : ChatBotProblemTypes.DispatchUnavailable,
            status = 503,
        });

        // Drives the PRODUCTION predicate. Recomputing the conjunction inline here asserted the test against its
        // own re-implementation: deleting the ServiceUnavailable half of the production check left this green.
        LiveRecoverySweep
            .ProvesMailboxAdmission(statusCode, body)
            .ShouldBe(expectedAdmissionProof);
    }

    /// <summary>
    /// The owner seed authorizes exactly the resource and command the admission probe submits. A drifted literal on
    /// either side would key the grant elsewhere and surface only as a non-transient 403 hours into the live lane.
    /// </summary>
    [Fact]
    public void MailboxOwnerSeedShouldTargetTheExactAdmissionProbeIntake()
    {
        using JsonDocument body = JsonDocument.Parse(
            LiveRecoverySweep.MailboxAdmissionProbeBody("01ARZ3NDEKTSV4RRFFQ69G5FAY"));
        JsonElement root = body.RootElement;

        root.GetProperty("command").GetProperty("intakeId").GetString().ShouldBe(RecoveryMailboxOwnerProvisioner.ProbeIntakeId);
        root.GetProperty("commandType").GetString().ShouldBe(RecoveryMailboxOwnerProvisioner.ProbeOperation);
        root.GetProperty("origin").GetString().ShouldBe("mailbox");
    }

    /// <summary>
    /// The seed stops on ChatBot's fail-closed canonical-execution refusal instead of retrying it for its whole
    /// budget; any other 503, status or body keeps the bounded retry.
    /// </summary>
    /// <param name="statusCode">The observed status.</param>
    /// <param name="problemType">The problem type, or <see langword="null"/> for a non-problem body.</param>
    /// <param name="expected">Whether the pair is the non-retryable canonical-execution refusal.</param>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, ChatBotProblemTypes.CanonicalExecutionUnavailable, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ChatBotProblemTypes.DispatchUnavailable, false)]
    [InlineData(HttpStatusCode.Forbidden, ChatBotProblemTypes.CanonicalExecutionUnavailable, false)]
    [InlineData(HttpStatusCode.InternalServerError, ChatBotProblemTypes.CanonicalExecutionUnavailable, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, null, false)]
    public void MailboxOwnerSeedFailsFastOnlyOnTheCanonicalExecutionRefusal(
        HttpStatusCode statusCode,
        string? problemType,
        bool expected)
    {
        string body = problemType is null
            ? "Service Unavailable"
            : JsonSerializer.Serialize(new { type = problemType, status = (int)statusCode, retryable = false });

        RecoveryMailboxOwnerProvisioner.IsCanonicalExecutionRefusal(statusCode, body).ShouldBe(expected);
    }

    private sealed class CapturingRetentionFailureSink : IRecoveryValidationEvidenceRetentionFailureSink
    {
        public List<RecoveryValidationEvidenceRetentionFailureMarker> Markers { get; } = [];

        public ValueTask RecordAsync(
            RecoveryValidationEvidenceRetentionFailureMarker marker,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Markers.Add(marker);
            return ValueTask.CompletedTask;
        }
    }
}
