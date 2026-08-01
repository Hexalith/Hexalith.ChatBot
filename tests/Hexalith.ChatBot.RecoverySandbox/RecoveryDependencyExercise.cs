using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Exercises four concrete ChatBot dependency seams and derives safety outcomes from recorded effects.</summary>
internal sealed class RecoveryDependencyExercise(
    RecoveryScopedOutageState state,
    RecoveryAiAssistanceProvider aiProvider,
    RecoveryEventStoreGatewayClient eventStore,
    RecoveryAuditWriter auditWriter,
    RecoveryAttachmentContentSource attachmentSource,
    RecoveryScopeObservationMonitor scopeMonitor)
{
    /// <summary>Runs the selected dependency contract for one idempotent correlation.</summary>
    public async ValueTask<(RecoveryDependencyExerciseResult Result, RecoveryScopeObservation? Scope)> ProcessAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        int effectsBefore = state.EffectCount(dependency, tenantRef);
        bool faultObserved = dependency switch
        {
            "ai-provider" => await ExerciseAiProviderAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "command-execution" => await ExerciseCommandDispatcherAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "audit-store" => await ExerciseAuditWriterAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "attachment-processing" => await ExerciseAttachmentSourceAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Unknown recovery dependency exercise."),
        };
        DateTimeOffset observedAtUtc = DateTimeOffset.UtcNow;
        bool expectedFault = state.IsFaulted(dependency);
        if (faultObserved != expectedFault)
        {
            throw new InvalidOperationException("The exercised ChatBot dependency seam did not reflect its configured fault state.");
        }

        RecoveryScopeObservation? scope = faultObserved
            ? await scopeMonitor.RecordAsync(
                new RecoveryDependencyFailure(dependency, correlationId, observedAtUtc),
                cancellationToken).ConfigureAwait(false)
            : null;
        int effectsAfter = state.EffectCount(dependency, tenantRef);
        int emittedEffects = effectsAfter - effectsBefore;
        return (
            new RecoveryDependencyExerciseResult(
                faultObserved,
                observedAtUtc,
                effectsAfter,
                UnauthorizedMutationDetected: faultObserved && emittedEffects != 0,
                SilentDataLossDetected: !faultObserved && effectsAfter == 0,
                DuplicateSideEffectDetected: effectsAfter > 1,
                CrossTenantLeakageDetected: state.HasCrossTenantEffect(dependency, tenantRef)),
            scope);
    }

    private async ValueTask<bool> ExerciseAiProviderAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceExecutionRecord record = await aiProvider.ExecuteAsync(
            new AiAssistanceProviderRequest(
                tenantRef,
                "project-recovery",
                "recovery-validator",
                correlationId,
                correlationId,
                "summarize",
                "context-recovery",
                "v1",
                "metadata_only",
                "operational",
                "disabled",
                ["recovery:source"],
                ["recovery:context"],
                [],
                "policy-recovery",
                "recovery_validation",
                correlationId,
                $"audit:{correlationId}"),
            cancellationToken).ConfigureAwait(false);
        return string.Equals(record.FailureCode, "ai_provider_unavailable", StringComparison.Ordinal);
    }

    private async ValueTask<bool> ExerciseCommandDispatcherAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "recovery-validator")], "recovery"));
        CommandSubmissionRequest request = new()
        {
            CommandId = correlationId,
            CommandType = nameof(RecordGovernedNote),
            Command = new RecordGovernedNote(correlationId),
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
        };
        ChatBotCommandSubmission submission = new(principal, request, correlationId, correlationId);
        ChatBotGatewayContext context = new(
            submission,
            new ChatBotAuthenticatedActor("recovery-validator", principal),
            new ChatBotTenantBinding(tenantRef));
        AcceptedCommandDispatcher dispatcher = new(
            eventStore,
            new RecoveryParticipantResolutionOrchestrator(),
            new RecoveryAssociationScoringOrchestrator(),
            new SystemClock());
        try
        {
            _ = await dispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (HttpRequestException) when (state.IsFaulted("command-execution"))
        {
            return true;
        }
    }

    private async ValueTask<bool> ExerciseAuditWriterAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        AuditWriteResult result = await auditWriter.RecordPreCommitAsync(
            new AuditEnvelope(
                tenantRef,
                "recovery-validator",
                "service-client",
                nameof(RecordGovernedNote),
                correlationId,
                "allow",
                "recovery_validation",
                correlationId,
                DateTimeOffset.UtcNow,
                "policy-recovery",
                ["recovery:source"],
                correlationId,
                "accepted",
                "metadata_only",
                "pending",
                AuditCommitPhase.PreCommit,
                "chatbot.audit-envelope.v1",
                null,
                "api"),
            cancellationToken).ConfigureAwait(false);
        return !result.Succeeded;
    }

    private async ValueTask<bool> ExerciseAttachmentSourceAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        MailboxAttachmentContentResult result = await attachmentSource.FetchAttachmentContentAsync(
            new MailboxAttachmentContentRequest(
                tenantRef,
                "project-recovery",
                "association-recovery",
                "intake-recovery",
                "mailbox-recovery",
                "message-recovery",
                "attachment-recovery",
                0,
                1,
                correlationId),
            cancellationToken).ConfigureAwait(false);
        return result.Kind is MailboxAttachmentContentResultKind.Retryable;
    }
}
