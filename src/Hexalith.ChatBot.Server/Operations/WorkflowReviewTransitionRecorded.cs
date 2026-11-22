using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>The workflow has recorded an attributable observation against the owning subject.</summary>
public sealed record WorkflowReviewTransitionRecorded(RecordWorkflowReviewTransition Transition) : IEventPayload;
