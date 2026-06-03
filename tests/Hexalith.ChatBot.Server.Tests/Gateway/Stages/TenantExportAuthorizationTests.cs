using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 9.8 (AC2, NFR1/FR75f) fail-closed gating for the tenant export request command. Mirrors the Story 9.7
/// data-class inventory gating: only a human compliance-admin may submit an export request; service clients, AI
/// actors, and every other admin role fail closed; invalid/stale payloads fail closed too.
/// </summary>
public sealed class TenantExportAuthorizationTests
{
    [Fact]
    public async Task ExportRequestShouldAllowOnlyHumanComplianceScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(ExportRequest()),
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
                Submission(ExportRequest()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task ExportRequestShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitTenantExportRequest invalid in new[]
                 {
                     ExportRequest() with { SourceVersion = -1 },
                     ExportRequest() with { SchemaVersion = "tenant-export-schema.custom" },
                     ExportRequest() with { ReasonCode = "unsafe reason" },
                     ExportRequest() with { ManifestFingerprint = "not-a-fingerprint" },
                     ExportRequest() with
                     {
                         RequestSpec = new TenantExportRequestSpec([], new TenantExportScope("tenant-alpha", [])),
                     },
                     ExportRequest() with
                     {
                         RequestSpec = new TenantExportRequestSpec(
                             [ComplianceRetentionClassIds.Attachments, ComplianceRetentionClassIds.Attachments],
                             new TenantExportScope("tenant-alpha", [])),
                     },
                     ExportRequest() with
                     {
                         RequestSpec = new TenantExportRequestSpec(
                             [ComplianceRetentionClassIds.Attachments], new TenantExportScope("unsafe tenant!", [])),
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

    private static SubmitTenantExportRequest ExportRequest()
        => new(
            "export-run-001",
            "inventory-snapshot-current",
            4,
            new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new TenantExportScope("tenant-alpha", ["project-authorized-001"])),
            "tenant-export-request",
            "admin-requester",
            TenantExportSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:exportmanifestfingerprint001",
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
