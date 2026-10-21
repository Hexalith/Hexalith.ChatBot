using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Records synthetic evidence-stage calls without replacing the unavailable production atomic gate.</summary>
internal sealed class SyntheticObservedAdmissionEvidenceValidator(List<string> calls, ChatBotAdmissionProfile profile, string? failure) : IChatBotAdmissionEvidenceValidator
{
    /// <summary>The single operation context passed through the observed admission stages.</summary>
    public ChatBotGatewayContext? Context { get; private set; }

    /// <inheritdoc />
    public ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        Context = context;
        context.AdmissionProfile.ShouldBe(profile);
        context.RiskClassification.ShouldBeNull();
        context.ApprovalResult.ShouldBeNull();
        return Observe(context, "operation");
    }

    /// <inheritdoc />
    public ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        context.RiskClassification!.Record.ShouldNotBeNull();
        context.ApprovalResult.ShouldBeNull();
        return Observe(context, "qualification");
    }

    /// <inheritdoc />
    public ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        (context.RiskClassification is not null).ShouldBe(ChatBotCommandAdmissionCatalog.RequiresClassification(context.Submission.Request.CommandType));
        (context.ApprovalResult is not null).ShouldBe(profile is ChatBotAdmissionProfile.AiRead or ChatBotAdmissionProfile.AiEffect or ChatBotAdmissionProfile.HumanEffect);
        return Observe(context, "profile");
    }

    /// <inheritdoc />
    public ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        AuditEnvelope envelope = context.AdmissionEnvelope.ShouldNotBeNull();
        envelope.CommandName.ShouldBe(context.Submission.Request.CommandType);
        envelope.TenantId.ShouldBe(context.TenantBinding.TenantId);
        envelope.ActorId.ShouldBe(context.Actor.ActorId);
        envelope.CorrelationId.ShouldBe(context.Submission.CorrelationId);
        envelope.StateTransition.ShouldBe("Received->Admitted");
        envelope.SourceEvidenceRefs.ShouldContain("command:" + context.Submission.Request.CommandId);
        context.Idempotency.ShouldBeNull();
        calls.Add("envelope");
        calls.Add("atomic");
        return new ChatBotAdmissionEvidenceValidator().ValidateAtomicParticipationAsync(context, cancellationToken);
    }

    private ValueTask<string?> Observe(ChatBotGatewayContext context, string stage)
    {
        context.ShouldBeSameAs(Context);
        context.AdmissionEnvelope.ShouldBeNull();
        context.Idempotency.ShouldBeNull();
        calls.Add(stage);
        return ValueTask.FromResult<string?>(failure == stage
            ? stage == "qualification" ? ChatBotMessageCodes.ClassifierIndeterminate : ChatBotMessageCodes.CommandContractInvalid : null);
    }
}
