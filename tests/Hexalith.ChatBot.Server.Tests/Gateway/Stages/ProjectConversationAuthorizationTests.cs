using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ProjectConversationAuthorizationTests
{
    [Theory]
    [InlineData("projectId")]
    [InlineData("ProjectId")]
    public async Task ProjectWriteShouldAuthorizeCanonicalAndGeneratedClientPropertyCasing(string propertyName)
    {
        using JsonDocument document = JsonDocument.Parse($$"""{"{{propertyName}}":"project-alpha"}""");

        ChatBotAuthorizationResult result = await new ParticipantAuthorizationStage().AuthorizeAsync(
            Submission(document.RootElement.Clone()),
            Actor("project-alpha"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectWriteShouldFailClosedForAmbiguousCaseVariantProperties()
    {
        using JsonDocument document = JsonDocument.Parse(
            """{"projectId":"project-alpha","ProjectId":"project-alpha"}""");

        ChatBotAuthorizationResult result = await new ParticipantAuthorizationStage().AuthorizeAsync(
            Submission(document.RootElement.Clone()),
            Actor("project-alpha"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
    }

    [Fact]
    public async Task LowRiskExecutionShouldAuthorizeOwnedProjectFromWirePayload()
    {
        using JsonDocument document = JsonDocument.Parse("""{"projectId":"project-alpha"}""");

        ChatBotAuthorizationResult result = await new ParticipantAuthorizationStage().AuthorizeAsync(
            Submission(
                document.RootElement.Clone(),
                nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance)),
            Actor("project-alpha"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task LowRiskExecutionShouldBindTenantWithoutTreatingCompositeProposalIdAsTenantScoped()
    {
        using JsonDocument document = JsonDocument.Parse(
            """{"projectId":"project-alpha","proposalId":"ai-proposal:composer-ai:request-001:composer-transition:request-001"}""");
        ChatBotCommandSubmission submission = Submission(
            document.RootElement.Clone(),
            nameof(Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance));

        ChatBotTenantBindingResult result = await new ClaimsTenantBindingStage().BindTenantAsync(
            submission,
            Actor("project-alpha"),
            TestContext.Current.CancellationToken);

        result.IsBound.ShouldBeTrue();
        result.Binding.ShouldNotBeNull().TenantId.ShouldBe("tenant-alpha");
    }

    private static ChatBotCommandSubmission Submission(
        JsonElement command,
        string commandType = nameof(RecordProjectConversationMessage))
        => new(
            Actor("project-alpha").Principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ChatBotSurfaceOrigin.Ui);

    private static ChatBotAuthenticatedActor Actor(string projectId)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim("eventstore:tenant", "tenant-alpha"),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectId),
            ],
            "test"));
        return new ChatBotAuthenticatedActor("actor-alpha", principal);
    }
}
