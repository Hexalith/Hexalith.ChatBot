using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies ordered common admission stages and failure containment without accepted commit authority.</summary>
public sealed class ChatBotCommandAdmissionSequenceTests
{
    /// <summary>Proves applicable stage order, single invocation and envelope construction before atomic denial.</summary>
    [Theory]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), (int)ChatBotAdmissionProfile.AiRead, "operation,classify,qualification,approval,profile,envelope,atomic")]
    [InlineData(nameof(ProposeAIAction), (int)ChatBotAdmissionProfile.AiEffect, "operation,classify,qualification,approval,profile,envelope,atomic")]
    [InlineData(nameof(ExecuteApprovedOutboundDraft), (int)ChatBotAdmissionProfile.HumanEffect, "operation,approval,profile,envelope,atomic")]
    [InlineData(nameof(RecordGovernedNote), (int)ChatBotAdmissionProfile.CommandExecution, "operation,profile,envelope,atomic")]
    public async Task CommonAdmissionShouldRunEachRequiredStageOnceBeforeTheUnavailableAtomicBoundary(
        string commandType, int profile, string expectedCalls)
    {
        ArgumentNullException.ThrowIfNull(expectedCalls);
        SyntheticAdmissionSequenceFixture fixture = new(commandType, (ChatBotAdmissionProfile)profile);

        ChatBotGatewayResult result = await fixture.Gateway.SubmitAsync(fixture.Submission, TestContext.Current.CancellationToken);

        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        fixture.Calls.ShouldBe(expectedCalls.Split(','));
        fixture.Evidence.Context!.AdmissionEnvelope.ShouldNotBeNull();
        fixture.AssertNoEffects();
    }

    /// <summary>Proves operation, qualification and profile failures prevent later calls and all mutation bookkeeping.</summary>
    [Theory]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), (int)ChatBotAdmissionProfile.AiRead, "operation", "operation")]
    [InlineData(nameof(ProposeAIAction), (int)ChatBotAdmissionProfile.AiEffect, "operation", "operation")]
    [InlineData(nameof(ExecuteApprovedOutboundDraft), (int)ChatBotAdmissionProfile.HumanEffect, "operation", "operation")]
    [InlineData(nameof(RecordGovernedNote), (int)ChatBotAdmissionProfile.CommandExecution, "operation", "operation")]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), (int)ChatBotAdmissionProfile.AiRead, "profile", "operation,classify,qualification,approval,profile")]
    [InlineData(nameof(ProposeAIAction), (int)ChatBotAdmissionProfile.AiEffect, "profile", "operation,classify,qualification,approval,profile")]
    [InlineData(nameof(ExecuteApprovedOutboundDraft), (int)ChatBotAdmissionProfile.HumanEffect, "profile", "operation,approval,profile")]
    [InlineData(nameof(RecordGovernedNote), (int)ChatBotAdmissionProfile.CommandExecution, "profile", "operation,profile")]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), (int)ChatBotAdmissionProfile.AiRead, "qualification", "operation,classify,qualification")]
    [InlineData(nameof(ProposeAIAction), (int)ChatBotAdmissionProfile.AiEffect, "qualification", "operation,classify,qualification")]
    public async Task CommonAdmissionFailureShouldStopBeforeLaterStagesEnvelopeOrReservation(
        string commandType, int profile, string failure, string expectedCalls)
    {
        ArgumentNullException.ThrowIfNull(expectedCalls);
        SyntheticAdmissionSequenceFixture fixture = new(commandType, (ChatBotAdmissionProfile)profile, failure);

        ChatBotGatewayResult result = await fixture.Gateway.SubmitAsync(fixture.Submission, TestContext.Current.CancellationToken);

        result.Problem!.Code.ShouldBe(failure == "qualification" ? ChatBotMessageCodes.ClassifierIndeterminate : ChatBotMessageCodes.CommandContractInvalid);
        fixture.Calls.ShouldBe(expectedCalls.Split(','));
        fixture.Evidence.Context!.AdmissionEnvelope.ShouldBeNull();
        fixture.AssertNoEffects();
    }
}
