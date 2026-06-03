using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 9.9 (AC2, NFR1/FR75f) fail-closed gating for the deletion/erasure request command. Mirrors the Story 9.8
/// tenant export gating: only a human compliance-admin may submit a deletion/erasure request — destroying nothing and
/// writing no durable state on denial; service clients, AI actors, and every other admin role fail closed; and
/// invalid/stale payloads fail closed too.
/// </summary>
public sealed class DeletionErasureAuthorizationTests
{
    [Fact]
    public async Task DeletionRequestShouldAllowOnlyHumanComplianceScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(DeletionRequest()),
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
                Submission(DeletionRequest()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task DeletionRequestShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitDeletionErasureRequest invalid in new[]
                 {
                     DeletionRequest() with { SourceVersion = -1 },
                     DeletionRequest() with { SchemaVersion = "deletion-erasure-schema.custom" },
                     DeletionRequest() with { ReasonCode = "unsafe reason" },
                     DeletionRequest() with { ProofFingerprint = "not-a-fingerprint" },
                     DeletionRequest() with
                     {
                         RequestSpec = new DeletionErasureRequestSpec(
                             DeletionErasureModes.Deletion, [], new DeletionErasureScope("tenant-alpha", [])),
                     },
                     DeletionRequest() with
                     {
                         RequestSpec = new DeletionErasureRequestSpec(
                             "purge", [ComplianceRetentionClassIds.Attachments], new DeletionErasureScope("tenant-alpha", [])),
                     },
                     DeletionRequest() with
                     {
                         RequestSpec = new DeletionErasureRequestSpec(
                             DeletionErasureModes.Deletion,
                             [ComplianceRetentionClassIds.Attachments, ComplianceRetentionClassIds.Attachments],
                             new DeletionErasureScope("tenant-alpha", [])),
                     },
                     DeletionRequest() with
                     {
                         RequestSpec = new DeletionErasureRequestSpec(
                             DeletionErasureModes.Deletion, [ComplianceRetentionClassIds.Attachments], new DeletionErasureScope("unsafe tenant!", [])),
                     },
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

    private static SubmitDeletionErasureRequest DeletionRequest()
        => new(
            "deletion-run-001",
            "inventory-snapshot-current",
            4,
            new DeletionErasureRequestSpec(
                DeletionErasureModes.Erasure,
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new DeletionErasureScope("tenant-alpha", ["project-authorized-001"])),
            "deletion-erasure-request",
            "admin-requester",
            DeletionErasureSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:deletionprooffingerprint001",
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
