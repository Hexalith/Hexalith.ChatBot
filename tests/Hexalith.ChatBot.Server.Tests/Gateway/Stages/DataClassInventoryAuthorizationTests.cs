using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 9.7 (AC2, NFR1/FR75f) fail-closed gating for the data-class inventory change command. Mirrors the
/// Story 7.4 retention-change gating: only a human compliance-admin may edit the inventory; service clients, AI
/// actors, and every other admin role fail closed; invalid/stale payloads fail closed too.
/// </summary>
public sealed class DataClassInventoryAuthorizationTests
{
    [Fact]
    public async Task InventoryChangeShouldAllowOnlyHumanComplianceScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(InventoryChange()),
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
                Submission(InventoryChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task InventoryChangeShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitDataClassInventoryChange invalid in new[]
                 {
                     InventoryChange() with { SourceVersion = -1 },
                     InventoryChange() with { SchemaVersion = "data-class-inventory-schema.custom" },
                     InventoryChange() with { ReasonCode = "unsafe reason" },
                     InventoryChange() with { NewInventorySnapshotFingerprint = "not-a-fingerprint" },
                     InventoryChange() with { ChangeSet = new DataClassInventoryChangeSet([]) },
                     InventoryChange() with
                     {
                         ChangeSet = new DataClassInventoryChangeSet(
                         [.. DataClassInventoryCatalog.Published.Classifications.Skip(1)]),
                     },
                     InventoryChange() with
                     {
                         ChangeSet = new DataClassInventoryChangeSet(
                         [.. DataClassInventoryCatalog.Published.Classifications.Select(classification =>
                             string.Equals(classification.DataClassId, ComplianceRetentionClassIds.AuditRecords, StringComparison.Ordinal)
                                 ? classification with { DeletionBehavior = DataClassDeletionBehaviors.HardDelete }
                                 : classification)]),
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

    private static SubmitDataClassInventoryChange InventoryChange()
        => new(
            "inventory-change-001",
            "inventory-snapshot-current",
            "inventory-snapshot-proposed",
            4,
            new DataClassInventoryChangeSet(DataClassInventoryCatalog.Published.Classifications),
            "data-class-inventory-update",
            "admin-requester",
            DataClassInventorySchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:oldinventoryfingerprint001",
            "sha256:newinventoryfingerprint001",
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
