using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

/// <summary>Pins refusal-only fallback behavior independently of the mandatory pre-parse admission gate.</summary>
public sealed class PolicyRefusalFallbackTests
{
    /// <summary>Both new and replayed aggregates refuse every unsupported policy DTO without changing state.</summary>
    [Theory]
    [InlineData("escalation", false)]
    [InlineData("escalation", true)]
    [InlineData("mailbox", false)]
    [InlineData("mailbox", true)]
    [InlineData("retention", false)]
    [InlineData("retention", true)]
    [InlineData("inventory", false)]
    [InlineData("inventory", true)]
    public void UnsupportedPolicyFallbackMustRejectWithoutMutation(string kind, bool replayed)
    {
        GovernedOperationState? state = replayed ? new() : null;
        state?.Apply(new GovernedNoteRecorded("existing-note"));
        string before = JsonSerializer.Serialize(state);
        CommandEnvelope envelope = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", "tenant-alpha", "chatbot", "policy-change-001",
            "policy-fallback", [], "correlation-001", null, "actor-alpha", null);
        DomainResult result = kind switch
        {
            "escalation" => GovernedOperationAggregate.Handle(new SubmitEscalationPolicyChange("policy-change-001", "source", "proposed", 7,
                new([]), "test", "requester", "v1", "correlation-001", "old", "new"), state, envelope),
            "mailbox" => GovernedOperationAggregate.Handle(new SubmitMailboxConfigurationChange("policy-change-001", "source", "proposed", 7,
                new([], [], [], []), "test", "requester", "v1", "correlation-001", "old", "new"), state, envelope),
            "retention" => GovernedOperationAggregate.Handle(new SubmitRetentionConfigurationChange("policy-change-001", "source", "proposed", 7,
                new([]), "test", "requester", "v1", "correlation-001", "policy", "old", "new", DateTimeOffset.UnixEpoch), state, envelope),
            "inventory" => GovernedOperationAggregate.Handle(new SubmitDataClassInventoryChange("policy-change-001", "source", "proposed", 7,
                new([]), "test", "requester", "v1", "correlation-001", "policy", "old", "new", DateTimeOffset.UnixEpoch), state, envelope),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        result.IsRejection.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.PostCommandStateJson.ShouldBeNull();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicyChangeRejected>()
            .ShouldBe(new TenantPolicyChangeRejected("policy-change-001", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable, 7, "correlation-001"));
        JsonSerializer.Serialize(state).ShouldBe(before);
    }
}
