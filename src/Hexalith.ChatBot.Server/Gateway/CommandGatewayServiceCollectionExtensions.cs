using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Adapters.Parties;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Retry;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Registration;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class CommandGatewayServiceCollectionExtensions
{
    /// <summary>The DAPR app id of the EventStore command gateway the dispatcher invokes via service invocation.</summary>
    private const string EventStoreDaprAppId = "eventstore";

    // The DAPR sidecar/proxy endpoints bind the IPv4 loopback (127.0.0.1). On dual-stack hosts (e.g. WSL2)
    // "localhost" resolves to ::1 first and the connection is refused, so every sidecar endpoint the chatbot
    // dials must use the IPv4 literal.
    private static string Ipv4Loopback(string endpoint)
        => endpoint.Replace("localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase);

    private static Dapr.Client.DaprClient BuildDaprClient()
    {
        Dapr.Client.DaprClientBuilder builder = new();
        string? grpcEndpoint = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        string resolved = Ipv4Loopback(string.IsNullOrWhiteSpace(grpcEndpoint)
            ? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001"}"
            : grpcEndpoint);
        _ = builder.UseGrpcEndpoint(resolved);

        string? httpEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        _ = builder.UseHttpEndpoint(Ipv4Loopback(string.IsNullOrWhiteSpace(httpEndpoint)
            ? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}"
            : httpEndpoint));

        return builder.Build();
    }

    public static IServiceCollection AddChatBotCommandGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The real dispatcher routes admitted commands into EventStore through the public gateway client. The
        // submission must authenticate WITHOUT a forged user JWT, so it goes through the chatbot's OWN DAPR
        // sidecar via service invocation tagged `dapr-app-id: eventstore` (the DaprAppIdHandler) — the receiving
        // EventStore sidecar then injects the verified `dapr-caller-app-id: chatbot` header that EventStore's
        // DaprInternal scheme validates against its allow-list (the AppHost grants `chatbot`). BaseAddress is the
        // caller's own sidecar (DAPR_HTTP_ENDPOINT / DAPR_HTTP_PORT, default 3500); the literal localhost host is
        // deliberately opaque to AddServiceDiscovery so it stays a direct sidecar call. In-process tests replace
        // IEventStoreGatewayClient with an accepting fake, so this wiring is exercised only in the live topology.
        string daprHttpEndpoint = Ipv4Loopback(Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT")
            ?? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
        string? daprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        _ = services
            .AddEventStoreGatewayClient(options => options.BaseAddress = new Uri(daprHttpEndpoint))
            .AddHttpMessageHandler(() => new DaprAppIdHandler(EventStoreDaprAppId, daprApiToken));

        // Host the Pattern-A GovernedOperationAggregate as a real EventStore domain processor. AddEventStore
        // reflection-discovers EventStoreAggregate<TState> subclasses in the domain assembly and registers each
        // as an IDomainProcessor; the EventStore aggregate actor invokes the /process endpoint (mapped in
        // Program.cs) by convention (domain "chatbot" → app id "chatbot" → method "process").
        _ = services.AddEventStore(typeof(GovernedOperationAggregate).Assembly);
        services.TryAddScoped<ChatBotDomainServiceRequestHandler>();

        // M0 read model is projected into an in-memory, tenant-partitioned store (mirrors the Folders default;
        // the DAPR chatbot-statestore-backed store is the production swap). Projection writes stay idempotent
        // and order-tolerant through the handler.
        services.TryAddSingleton<IGovernedOperationProjectionStore, InMemoryGovernedOperationProjectionStore>();
        services.TryAddSingleton<GovernedOperationProjectionHandler>();
        services.TryAddSingleton<IParticipantResolutionProjectionStore, InMemoryParticipantResolutionProjectionStore>();
        services.TryAddSingleton<IParticipantDisplayDirectory, UnavailableParticipantDisplayDirectory>();
        services.TryAddSingleton<ParticipantResolutionProjectionHandler>();
        services.TryAddSingleton<IProjectConversationProjectionStore, InMemoryProjectConversationProjectionStore>();
        services.TryAddSingleton<IMailboxAttachmentContentSource, UnavailableMailboxAttachmentContentSource>();
        services.TryAddSingleton<IOutboundMailboxSender, UnavailableOutboundMailboxSender>();
        services.TryAddSingleton<IFolderStore, UnavailableFolderStore>();
        services.TryAddSingleton<IAttachmentScanner, PassThroughAttachmentScanner>();
        services.TryAddSingleton<IAttachmentUnsafeHandlingResolver, DefaultAttachmentUnsafeHandlingResolver>();
        services.TryAddSingleton<IAttachmentSafetyPolicy, DefaultAttachmentSafetyPolicy>();
        services.TryAddSingleton<IAttachmentAuthorizationService, ProjectionAttachmentAuthorizationService>();
        services.TryAddSingleton<IAttachmentCaptureCoordinator, AttachmentCaptureCoordinator>();
        services.TryAddSingleton<IProjectAiContextPackageAssembler, DefaultProjectAiContextPackageAssembler>();
        services.TryAddSingleton<ITenantAiPolicySnapshotProvider, UnavailableTenantAiPolicySnapshotProvider>();
        services.TryAddSingleton<IAiActionPolicyEvaluator, DefaultAiActionPolicyEvaluator>();
        services.TryAddSingleton<IAiAssistanceProvider, DisabledAiAssistanceProvider>();
        services.TryAddSingleton<IApprovedAiActionCommandAllowlist, ApprovedAiActionCommandAllowlist>();
        services.TryAddSingleton<IConversationWriter, MetadataOnlyConversationWriter>();
        services.TryAddSingleton<IAssociationProjectionStore, InMemoryAssociationProjectionStore>();
        services.TryAddSingleton<IAiActionProposalInvalidationCoordinator, AiActionProposalInvalidationCoordinator>();
        services.TryAddSingleton<AssociationProjectionHandler>();
        services.TryAddSingleton<AiOutcomeProjectionHandler>();
        services.TryAddSingleton<TaskIntentProjectionHandler>();
        services.TryAddSingleton<ApprovalProjectionHandler>();
        services.TryAddSingleton<IMailboxMessageContentSource, UnavailableMailboxMessageContentSource>();
        services.AddChatBotCorrectionPropagation();

        return services
            .AddScoped<IAuthenticationStage, ClaimsAuthenticationStage>()
            .AddScoped<ITenantBindingStage, ClaimsTenantBindingStage>()
            .AddScoped<IServiceClientGrantResolver, ClaimsServiceClientGrantResolver>()
            .AddScoped<IServiceClientControlStateProvider, AlwaysActiveServiceClientControlStateProvider>()
            .AddScoped<IAiActorControlStateProvider, AlwaysActiveAiActorControlStateProvider>()
            .AddScoped<ICommandCapabilityControlStateProvider, AlwaysActiveCommandCapabilityControlStateProvider>()
            .AddScoped<IOutboundChannelControlStateProvider, AlwaysActiveOutboundChannelControlStateProvider>()
            .AddScoped<IServiceClientRateLimitProvider, AlwaysUnlimitedServiceClientRateLimitProvider>()
            .AddScoped<IServiceClientCommandHistory, EmptyServiceClientCommandHistory>()
            .AddScoped<IAiActorRateLimitProvider, AlwaysUnlimitedAiActorRateLimitProvider>()
            .AddScoped<IAiActorProposalHistory, EmptyAiActorProposalHistory>()
            .AddScoped<ICommandCapabilityRateLimitProvider, AlwaysUnlimitedCommandCapabilityRateLimitProvider>()
            .AddScoped<ICommandCapabilityCommandHistory, EmptyCommandCapabilityCommandHistory>()
            .AddScoped<IOutboundChannelRateLimitProvider, AlwaysUnlimitedOutboundChannelRateLimitProvider>()
            .AddScoped<IOutboundChannelSendHistory, EmptyOutboundChannelSendHistory>()
            .AddScoped<IServiceClientGrantValidator, ServiceClientGrantValidator>()
            .AddSingleton<IAssociationCorrectionDependencyReadiness, DefaultAssociationCorrectionDependencyReadiness>()
            .AddScoped<IAuthorizationStage, ParticipantAuthorizationStage>()
            .AddScoped<IRiskClassifier, DeterministicAiActionRiskClassifier>()
            .AddScoped<IApprovalGate, AiActionApprovalGate>()
            .AddScoped<IParticipantDirectory, UnavailableParticipantDirectory>()
            .AddScoped<IParticipantResolutionOrchestrator, ParticipantResolutionOrchestrator>()
            .AddScoped<IProjectDirectory, UnavailableProjectDirectory>()
            .AddScoped<IAssociationScoringOrchestrator, AssociationScoringOrchestrator>()
            .AddSingleton(static _ => BuildDaprClient())
            .AddSingleton<IIdempotencyStore, DaprCoarseIdempotencyStore>()
            .AddSingleton<InMemoryAuditWriter>()
            // Story 9.1 (NFR49a): the WORM hash-chain store sits behind the post-commit audit seam via the
            // ChainedAuditWriter decorator (fail-open-then-reconcile). The in-process append-only store is the M0
            // test/dev default; the production swap is an immutable/WORM object store behind the same interface.
            .AddSingleton<InMemoryWormAuditStore>()
            .AddSingleton<IWormAuditStore>(static services => services.GetRequiredService<InMemoryWormAuditStore>())
            .AddSingleton<ChainedAuditWriter>(static services => new ChainedAuditWriter(
                services.GetRequiredService<InMemoryAuditWriter>(),
                services.GetRequiredService<IWormAuditStore>()))
            .AddSingleton<IAuditWriter>(static services => services.GetRequiredService<ChainedAuditWriter>())
            // The audit-history surface (Story 1.9) reads the inner writer's metadata-only envelopes directly.
            .AddSingleton<IAuditHistoryReader>(static services => services.GetRequiredService<InMemoryAuditWriter>())
            // Story 9.1 GDPR erasure (AC3): separate-KMS redaction-key store, encrypted-original store, projection
            // tombstone store, the nightly chain verifier's broken-chain alert coordinator, and the redaction service.
            .AddSingleton<IKmsRedactionKeyStore, InMemoryKmsRedactionKeyStore>()
            .AddSingleton<IEncryptedAuditOriginalStore, InMemoryEncryptedAuditOriginalStore>()
            .AddSingleton<IRedactionProjectionStore, InMemoryRedactionProjectionStore>()
            .AddSingleton<AuditChainVerificationCoordinator>()
            .AddSingleton<AuditRedactionService>()
            .AddSingleton<InMemoryAuditReplayIntentQueue>()
            .AddSingleton<IAuditReplayIntentQueue>(static services => services.GetRequiredService<InMemoryAuditReplayIntentQueue>())
            .AddSingleton<InMemoryOperatorAlertSink>()
            .AddSingleton<IOperatorAlertSink>(static services => services.GetRequiredService<InMemoryOperatorAlertSink>())
            .AddSingleton<InMemoryNotificationSink>()
            .AddSingleton<INotificationSink>(static services => services.GetRequiredService<InMemoryNotificationSink>())
            .AddSingleton<InMemoryNotificationDeliveryHistoryStore>()
            .AddSingleton<INotificationDeliveryHistoryStore>(static services => services.GetRequiredService<InMemoryNotificationDeliveryHistoryStore>())
            .AddSingleton<InMemoryNotificationDigestStore>()
            .AddSingleton<INotificationDigestStore>(static services => services.GetRequiredService<InMemoryNotificationDigestStore>())
            .AddSingleton<IOperationStatusStore, InMemoryOperationStatusStore>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<RetryFailureAlertEmitter>()
            .AddSingleton<EscalationEvaluationCoordinator>()
            .AddSingleton<NotificationThrottleCoordinator>()
            .AddSingleton<ReviewerBacklogAlertCoordinator>()
            .AddSingleton<ApprovalRubberStampRateCoordinator>()
            .AddSingleton<InMemoryUserFacingMessageTelemetry>()
            .AddSingleton<IUserFacingMessageTelemetry>(static services => services.GetRequiredService<InMemoryUserFacingMessageTelemetry>())
            // Story 8.2: the always-on operational metrics seam. The audit-projection-lag source defaults to the
            // fail-safe Unavailable feed (no fabricated lag) until a real per-tenant checkpoint source is swapped in.
            .AddSingleton<IAuditProjectionLagSource, UnavailableAuditProjectionLagSource>()
            // Story 8.4: tenant-safe operational alert wiring. The retry-exhaustion source and authorization-failure
            // counter are in-process singletons (mirroring the audit-projection-lag source); the wiring coordinator
            // mirrors the ReviewerBacklogAlertCoordinator registration.
            .AddSingleton<IRetryExhaustionAlertSource, InMemoryRetryExhaustionAlertSource>()
            .AddSingleton<IAuthorizationFailureCounter, InMemoryAuthorizationFailureCounter>()
            .AddSingleton<OperationalAlertWiringCoordinator>()
            .AddSingleton<IChatBotMetrics, ChatBotMetrics>()
            .AddScoped<IUserFacingRedactionStage, CoarseUserFacingRedactionStage>()
            .AddScoped<IChatBotProblemDetailsFactory, ChatBotProblemDetailsFactory>()
            .AddScoped<ILifecycleTransitionGuard, CommandSubmissionLifecycleTransitionGuard>()
            .AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>()
            .AddScoped<ICommandDispatcher, AcceptedCommandDispatcher>()
            .AddScoped<CommandGateway>();
    }

    public static IServiceCollection AddChatBotCorrectionPropagation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICorrectionPropagationCommandWriter, EventStoreCorrectionPropagationCommandWriter>();
        services.TryAddSingleton<ICorrectionPropagationCoordinator, DaprCorrectionPropagationCoordinator>();
        services.TryAddSingleton<ICorrectedContextReadinessPolicy, ProjectionCorrectedContextReadinessPolicy>();
        services.AddSingleton<ICorrectionPropagationStoreActivity>(static services =>
            new MetadataOnlyCorrectionPropagationStoreActivity(Association.CorrectionPropagationStoreKeys.AssociationRouting, services.GetRequiredService<ISystemClock>()));
        services.AddSingleton<ICorrectionPropagationStoreActivity>(static services =>
            new MetadataOnlyCorrectionPropagationStoreActivity(Association.CorrectionPropagationStoreKeys.EvidenceSnapshot, services.GetRequiredService<ISystemClock>()));
        services.AddSingleton<ICorrectionPropagationStoreActivity>(static services =>
            new MetadataOnlyCorrectionPropagationStoreActivity(Association.CorrectionPropagationStoreKeys.OperationStatus, services.GetRequiredService<ISystemClock>()));
        services.AddSingleton<ICorrectionPropagationStoreActivity>(static services =>
            new MetadataOnlyCorrectionPropagationStoreActivity(Association.CorrectionPropagationStoreKeys.AiContextReadiness, services.GetRequiredService<ISystemClock>()));

        return services;
    }

    /// <summary>
    /// Production swap (gated on a DAPR sidecar being present): project chatbot read models into the DAPR
    /// <c>chatbot-statestore</c> (Redis) instead of the in-memory M0 defaults, so operation and association
    /// propagation views survive across the topology and are inspectable end-to-end.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddChatBotDaprStateStores(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IGovernedOperationProjectionStore>();
        services.RemoveAll<IAssociationProjectionStore>();
        services.RemoveAll<IProjectConversationProjectionStore>();
        services.RemoveAll<IOperationStatusStore>();
        return services
            .AddSingleton<IGovernedOperationProjectionStore, DaprGovernedOperationViewStore>()
            .AddSingleton<IAssociationProjectionStore, DaprAssociationProjectionStore>()
            .AddSingleton<IProjectConversationProjectionStore, DaprProjectConversationProjectionStore>()
            .AddSingleton<IOperationStatusStore, DaprOperationStatusStore>();
    }
}
