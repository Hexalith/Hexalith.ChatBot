using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Returns a synthetic approval disposition to observe pipeline sequencing only.</summary>
internal sealed class SyntheticObservedAdmissionApprovalGate(List<string> calls) : IApprovalGate
{
    /// <inheritdoc />
    public ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        context.ApprovalResult.ShouldBeNull();
        context.AdmissionEnvelope.ShouldBeNull();
        calls.Add("approval");
        return ValueTask.FromResult(ChatBotApprovalResult.Approved);
    }
}
