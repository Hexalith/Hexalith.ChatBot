using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 9.10 (AC2/AC3, NFR1/FR75f) fail-closed gating for the consent/lawful-basis recording command. Mirrors the
/// Story 9.9 deletion gating: only a human compliance-admin may record consent/lawful-basis metadata — writing no
/// durable state on denial; service clients, AI actors, and every other admin role fail closed; and invalid/stale
/// payloads fail closed too.
/// </summary>
public sealed class ConsentLawfulBasisAuthorizationTests
{
    [Fact]
    public async Task ConsentRecordShouldAllowOnlyHumanComplianceScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(ConsentRequest()),
            Actor("human", "compliance-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "compliance-admin"),
                     Actor("ai", "compliance-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(ConsentRequest()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task ConsentRecordShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitConsentLawfulBasisRecord invalid in new[]
                 {
                     ConsentRequest() with { SourceVersion = -1 },
                     ConsentRequest() with { SchemaVersion = "consent-lawful-basis-schema.custom" },
                     ConsentRequest() with { ReasonCode = "unsafe reason" },
                     ConsentRequest() with { RecordFingerprint = "not-a-fingerprint" },
                     ConsentRequest() with { SubjectKind = "subject-x" },
                     ConsentRequest() with { LawfulBasis = "because" },
                     ConsentRequest() with { RecordStatus = "pending" },
                     ConsentRequest() with { RedactionSensitivity = "top-secret" },
                     ConsentRequest() with { SubjectLocator = "raw subject!" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "compliance-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static SubmitConsentLawfulBasisRecord ConsentRequest()
        => new(
            "consent-record-001",
            4,
            ConsentSubjectKinds.ExternalParticipant,
            "subject-locator-001",
            "project-authorized-001",
            ConsentLawfulBases.Consent,
            ConsentRecordStatuses.Active,
            "basis-source-dpia-001",
            DataClassRedactionSensitivities.Restricted,
            "consent-lawful-basis-request",
            "admin-requester",
            ConsentLawfulBasisSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:consentrecordfingerprint001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

    private static ChatBotCommandSubmission Submission(object command, string? commandType = null)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType ?? command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);

    private static ChatBotAuthenticatedActor Actor(string actorType, string role)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
        return new ChatBotAuthenticatedActor("actor-alpha", principal);
    }
}
