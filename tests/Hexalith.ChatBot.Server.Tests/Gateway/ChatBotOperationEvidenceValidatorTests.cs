using System.Security.Claims;
using System.Text.Json;

using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies operation identity, revision and cached owner proofs through the real evidence validator.</summary>
public sealed class ChatBotOperationEvidenceValidatorTests
{
    private const string OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string ValidPayload = "{\"noteId\":\"note-alpha\",\"expectedSourceVersion\":8}";

    /// <summary>Maps owner-reader exceptions to dependency unavailability rather than caller contract errors.</summary>
    [Theory]
    [InlineData("json")]
    [InlineData("invalid-operation")]
    [InlineData("argument")]
    [InlineData("dependency-cancellation")]
    public async Task OwnerReaderExceptionsShouldRemainCanonicalUnavailable(string failure)
    {
        ChatBotGatewayContext context = Context();
        SyntheticOperationOwnerEvidenceSource source = new(context)
        {
            ReadFailure = failure switch
            {
                "json" => new JsonException("Synthetic owner parsing failure."),
                "invalid-operation" => new InvalidOperationException("Synthetic owner state failure."),
                "argument" => new ArgumentException("Synthetic owner argument failure."),
                _ => new OperationCanceledException("Synthetic dependency cancellation."),
            },
        };

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        source.ReadCount.ShouldBe(1);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Preserves caller cancellation without converting it into an admission denial.</summary>
    [Fact]
    public async Task CallerCancellationShouldPropagateWithoutReadingOwnerEvidence()
    {
        ChatBotGatewayContext context = Context();
        SyntheticOperationOwnerEvidenceSource source = new(context);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, cancellation.Token).ConfigureAwait(true));
        source.ReadCount.ShouldBe(0);
    }

    /// <summary>Rejects missing or malformed stable identities before requesting owner evidence.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("malformed identity")]
    [InlineData("control\nidentity")]
    public async Task MissingOrMalformedOperationIdentityShouldFailBeforeOwnerRead(string? operationId)
    {
        ChatBotGatewayContext context = Context(operationId: operationId);
        SyntheticOperationOwnerEvidenceSource source = new(context);

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CommandContractInvalid);

        source.ReadCount.ShouldBe(0);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Rejects malformed payloads and revision values before requesting owner evidence.</summary>
    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"expectedSourceVersion\":-1}")]
    [InlineData("{\"expectedSourceVersion\":null}")]
    [InlineData("{\"expectedVersion\":\"8\"}")]
    [InlineData("{\"expectedSourceVersion\":1.5}")]
    [InlineData("{\"expectedSourceVersion\":9223372036854775808}")]
    [InlineData("{\"expectedSourceVersion\":8,\"ExpectedSourceVersion\":8}")]
    public async Task MalformedPayloadOrRevisionShouldFailBeforeOwnerRead(string payload)
    {
        ChatBotGatewayContext context = Context(payload);
        SyntheticOperationOwnerEvidenceSource source = new(context);

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CommandContractInvalid);

        source.ReadCount.ShouldBe(0);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Rejects absent or stale revisions without a qualifying owner concurrency guard.</summary>
    [Theory]
    [InlineData("{}", null, ChatBotMessageCodes.CanonicalExecutionUnavailable)]
    [InlineData("{\"expectedSourceVersion\":7}", null, ChatBotMessageCodes.CommandContractInvalid)]
    [InlineData(ValidPayload, -1L, ChatBotMessageCodes.CommandContractInvalid)]
    [InlineData(ValidPayload, 7L, ChatBotMessageCodes.CommandContractInvalid)]
    public async Task MissingOrStaleRevisionWithoutAnOwnerGuardShouldDeny(string payload, long? concurrency, string expected)
    {
        ChatBotGatewayContext context = Context(payload, concurrency: concurrency);
        SyntheticOperationOwnerEvidenceSource source = new(context);

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(expected);

        source.ReadCount.ShouldBe(concurrency == -1 ? 0 : 1);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Rejects mismatched, stale and invalid owner bindings without caching them.</summary>
    [Theory]
    [InlineData("tenant")]
    [InlineData("actor")]
    [InlineData("command")]
    [InlineData("operation")]
    [InlineData("profile")]
    [InlineData("negative-revision")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("invalid-lifetime")]
    public async Task OwnerProofMustMatchTheExactCurrentOperation(string mismatch)
    {
        ChatBotGatewayContext context = Context();
        SyntheticOperationOwnerEvidenceSource source = new(context);
        ChatBotProfileAdmissionEvidence proof = source.Proof!;
        source.Proof = mismatch switch
        {
            "tenant" => proof with { TenantId = "other-tenant" },
            "actor" => proof with { ActorId = "other-actor" },
            "command" => proof with { CommandType = nameof(ProposeAIAction) },
            "operation" => proof with { OperationId = "other-operation" },
            "profile" => proof with { Profile = ChatBotAdmissionProfile.AiRead },
            "negative-revision" => proof with { CurrentRevision = -1 },
            "future" => proof with { ObservedAt = source.UtcNow.AddSeconds(1) },
            "expired" => proof with { ExpiresAt = source.UtcNow },
            _ => proof with { ObservedAt = source.UtcNow, ExpiresAt = source.UtcNow },
        };

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);

        source.ReadCount.ShouldBe(1);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Contains missing and failed owner reads without retaining admission evidence.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrUnavailableOwnerProofShouldFailWithoutCaching(bool throws)
    {
        ChatBotGatewayContext context = Context();
        SyntheticOperationOwnerEvidenceSource source = new(context) { Proof = null, ThrowOnRead = throws };

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);

        source.ReadCount.ShouldBe(1);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>Reuses one validated owner read for profile checks while atomic participation stays unavailable.</summary>
    [Theory]
    [InlineData(ValidPayload, null, false)]
    [InlineData("{}", 8L, false)]
    [InlineData("{}", null, true)]
    public async Task ValidatedOperationProofShouldBeReadOnceAndReusedByProfileChecksWithoutEnablingCommit(string payload, long? concurrency, bool ownerGuard)
    {
        ChatBotGatewayContext context = Context(payload, concurrency: concurrency);
        SyntheticOperationOwnerEvidenceSource source = new(context);
        source.Proof = source.Proof! with { OwnerConcurrencyGuardReference = ownerGuard ? "synthetic:owner-concurrency-guard" : null };
        ChatBotProfileAdmissionEvidence expected = source.Proof;
        ChatBotAdmissionEvidenceValidator validator = new(source, source);

        (await validator.ValidateOperationAsync(context, TestContext.Current.CancellationToken)).ShouldBeNull();
        context.AdmissionEvidence.ShouldBeSameAs(expected);
        source.Proof = null;
        source.ThrowOnRead = true;
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await validator.ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);

        source.ReadCount.ShouldBe(1);
        context.AdmissionEvidence.ShouldBeSameAs(expected);
        context.AdmissionEnvelope.ShouldBeNull();
        context.Idempotency.ShouldBeNull();
    }

    /// <summary>Caller-invented expected fields cannot replace a contract revision or an owner concurrency guard.</summary>
    [Theory]
    [InlineData("{\"expectedFooRevision\":8}")]
    [InlineData("{\"expectedRevision\":8}")]
    [InlineData("{\"expectedOther\":8}")]
    public async Task UndeclaredExpectedFieldsCannotBypassOwnerConcurrency(string payload)
    {
        ChatBotGatewayContext context = Context(payload);
        context.Submission.Request.CommandType = nameof(RecordGovernedNote);
        SyntheticOperationOwnerEvidenceSource source = new(context);
        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CommandContractInvalid);
        source.ReadCount.ShouldBe(0);
        context.AdmissionEvidence.ShouldBeNull();
    }

    /// <summary>The real validator refuses unknown intended and non-allowlisted execution targets before owner reads.</summary>
    [Theory]
    [InlineData(nameof(ProposeAIAction), "{\"intendedCommandName\":\"Unknown.Command\"}")]
    [InlineData(nameof(ExecuteApprovedAIAction), "{\"commandName\":\"Unknown.Command\"}")]
    public async Task NonAllowlistedCommandMustFailBeforeOwnerEvidence(string commandType, string payload)
    {
        ChatBotGatewayContext context = Context(payload);
        context.Submission.Request.CommandType = commandType;
        SyntheticOperationOwnerEvidenceSource source = new(context);
        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateOperationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
        source.ReadCount.ShouldBe(0);
        context.AdmissionEvidence.ShouldBeNull();
    }

    private static ChatBotGatewayContext Context(string payload = ValidPayload, string? operationId = OperationId, long? concurrency = null)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "synthetic-requester")], "synthetic"));
        using JsonDocument document = JsonDocument.Parse(payload);
        ChatBotCommandSubmission submission = new(principal, new CommandSubmissionRequest
        {
            CommandId = operationId!, CommandType = nameof(RecordProjectConversationMessage), Command = document.RootElement.Clone(),
            Concurrency = concurrency.HasValue ? new() { ExpectedRevision = concurrency.Value } : null,
        }, "01ARZ3NDEKTSV4RRFFQ69G5FAW", null);
        ChatBotGatewayContext context = new(submission, new("synthetic-requester", principal), new("synthetic-tenant"));
        context.SelectAdmissionProfile(ChatBotAdmissionProfile.GovernedChat);
        return context;
    }
}
