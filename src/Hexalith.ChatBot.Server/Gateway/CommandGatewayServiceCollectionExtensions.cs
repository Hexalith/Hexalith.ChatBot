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
using Hexalith.ChatBot.Server.Projections.DerivedStores;
using Hexalith.EventStore.Client.Registration;

using Dapr.Workflow;

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
        // Story 9.4 (FR95/FR95a): replay/simulation outbound isolation. The production outbound sender stays the
        // existing UnavailableOutboundMailboxSender (the only production impl today). The tenant-aware
        // ReplayAwareOutboundMailboxSender is the single IOutboundMailboxSender the dispatcher resolves: it routes a
        // TEST tenant (ReplayTenantPolicy.IsTestTenant) to the TestModeOutboundMailboxSender (intercept + record to the
        // tenant-partitioned outbound-trace store, never send) and every PRODUCTION tenant to the production sender
        // unchanged. Production tenants are never reachable to the test-mode adapter — one decision point, by construction.
        services.TryAddSingleton<IOutboundTraceStore, InMemoryOutboundTraceStore>();
        services.TryAddSingleton<UnavailableOutboundMailboxSender>();
        services.TryAddSingleton<TestModeOutboundMailboxSender>();
        services.TryAddSingleton<IOutboundMailboxSender>(static provider => new ReplayAwareOutboundMailboxSender(
            provider.GetRequiredService<UnavailableOutboundMailboxSender>(),
            provider.GetRequiredService<TestModeOutboundMailboxSender>()));
        // Story 9.5 (FR55a/NFR9a): the tenant-partitioned-by-construction derived-store seam (vector index, embedding
        // store, prompt-context cache, candidate-ranking cache). The in-memory default keeps one partition per
        // {tenant}:{derived-class} so a cross-tenant read is a key miss at the store layer — the M2 live
        // Redis-Vector/FalkorDB binding is an additive IDerivedStore behind this same interface (DerivedStorePartition).
        services.TryAddSingleton<IDerivedStore, InMemoryDerivedStore>();
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
            // Story 9.2 (NFR50a): audit completeness as a production observable. The reconstructor + measurer + budget
            // evaluator are stateless/pure (no DI needed); the measurer reads the WORM chain and the governed-operation
            // projection read-only, and the audit-then-deliver alert coordinator mirrors the AuditChainVerificationCoordinator
            // registration. The periodic scheduler is deferred (inert-control-floor); a runtime calls
            // MeasureAllTenantsAndAlertAsync on its cadence and publishes the sweep into IAuditCompletenessSource.
            .AddSingleton<AuditCompletenessMeasurer>()
            .AddSingleton<AuditCompletenessAlertCoordinator>()
            // Story 9.4 (FR95a): the nightly replay-isolation probe coordinator, modeled directly on the 9.1 chain
            // verifier — pure verifier + fail-closed audit-then-deliver, no always-on BackgroundService. A periodic
            // scheduler AND the M2 release gate call SweepAllProductionTenantsAsync; zero breaches ⇒ release may proceed.
            .AddSingleton<ReplayIsolationProbeCoordinator>()
            // Story 9.5 (FR55a/NFR9a/NFR59): the synthetic cross-tenant derived-store isolation probe coordinator,
            // modeled directly on the 9.4 replay probe — pure verifier + fail-closed audit-then-deliver, no always-on
            // BackgroundService. A periodic scheduler AND the M2 release gate call SweepAllTenantPairsAsync; zero
            // breaches ⇒ release may proceed.
            .AddSingleton<DerivedStoreIsolationProbeCoordinator>()
            // Story 9.11 (NFR56/A10): the continuity-drill coordinator, modeled directly on the 9.5 derived-store probe
            // — a pure evaluator (ContinuityDrillEvaluator) + fail-closed audit-then-deliver, no always-on
            // BackgroundService. A periodic scheduler AND a release gate call RunAllScenariosAsync on its cadence
            // (Unmeasurable == 0 ⇒ the drills produced evidence). The live fault-injection runtime behind the
            // IContinuityDrillScenarioRunner seam is M2-deferred — the inert default throws so the seam is wired but not
            // yet live (mirroring the 9.4 deferred replay driver); the coordinator's fail-safe catch maps it to an
            // unmeasurable report, never a fabricated met.
            .AddSingleton<IContinuityDrillScenarioRunner, DeferredContinuityDrillScenarioRunner>()
            .AddSingleton<ContinuityDrillCoordinator>()
            // Story 9.12 (NFR57/NFR49a): the projection-rebuild validation coordinator, modeled directly on the 9.11
            // continuity drill — a pure evaluator (ProjectionRebuildEquivalenceEvaluator) + fail-closed audit-then-deliver,
            // no always-on BackgroundService. A periodic scheduler AND a release gate call RunAllAsync on its cadence
            // (Divergent == 0 && Unmeasurable == 0 ⇒ rebuilds are deterministic and produced evidence). The live rebuild
            // runtime behind the IProjectionRebuildDriver seam is M2-deferred — the inert default throws so the seam is
            // wired but not yet live (mirroring the 9.4 deferred replay driver); the coordinator's fail-safe catch maps it
            // to an unmeasurable report, never a fabricated equivalent.
            .AddSingleton<IProjectionRebuildDriver, DeferredProjectionRebuildDriver>()
            .AddSingleton<ProjectionRebuildValidationCoordinator>()
            // Story 9.13 (NFR58/NFR59/NFR41): the scoped-outage degradation validation coordinator, modeled directly on
            // the 9.12 projection-rebuild validation — a pure evaluator (ScopedOutageDegradationEvaluator) + fail-closed
            // audit-then-deliver, no always-on BackgroundService. A periodic scheduler AND a release gate call
            // RunAllScenariosAsync on its cadence (Breached == 0 && Unmeasurable == 0 ⇒ every dependency outage degraded
            // only its scope and produced evidence). The live fault-injection runtime behind the
            // IScopedOutageInjectionDriver seam is M2-deferred — the inert default throws so the seam is wired but not yet
            // live (mirroring the 9.4 deferred replay driver); the coordinator's fail-safe catch maps it to an
            // unmeasurable report, never a fabricated contained.
            .AddSingleton<IScopedOutageInjectionDriver, DeferredScopedOutageInjectionDriver>()
            .AddSingleton<ScopedOutageDegradationValidationCoordinator>()
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
            // Story 9.2: the completeness gauge's read-only source. Defaults to the fail-safe Unavailable feed (no
            // fabricated fraction) until a periodic sweep publishes MeasureAllTenantsAsync results into it.
            .AddSingleton<IAuditCompletenessSource, UnavailableAuditCompletenessSource>()
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
        services.TryAddSingleton<ICorrectionPropagationActivityCatalog, CorrectionPropagationActivityCatalog>();
        services.TryAddSingleton<ICorrectionPropagationWorkflowRuntime, UnavailableCorrectionPropagationWorkflowRuntime>();
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

        // Story 9.6 (AC1/AC2): wire the vector-reindex M2 derived-store activity in alongside the four metadata-only M0
        // activities, so correction propagation reaches the derived stores. IDerivedStore is already registered
        // (Story 9.5); the version-guard ledger and the in-memory ReindexVectors reindexer plug in behind their seams.
        // The live Hexalith.Memories Redis-Vector/FalkorDB reindex binding is the additive deferred-M2 swap.
        services.TryAddSingleton<IVectorReindexLedger, InMemoryVectorReindexLedger>();
        services.TryAddSingleton<IVectorReindexer, InMemoryVectorReindexer>();
        services.AddSingleton<ICorrectionPropagationStoreActivity>(static services =>
            new VectorReindexCorrectionPropagationStoreActivity(services.GetRequiredService<IVectorReindexer>()));

        return services;
    }

    public static IServiceCollection AddChatBotCorrectionPropagationWorkflow(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ICorrectionPropagationWorkflowRuntime>();
        services.AddSingleton<ICorrectionPropagationWorkflowRuntime, DaprCorrectionPropagationWorkflowRuntime>();
        services.AddDaprWorkflow(static options =>
        {
            options.RegisterWorkflow<CorrectionPropagationWorkflow>();
            options.RegisterActivity<CorrectionPropagationScopeActivity>();
            options.RegisterActivity<CorrectionPropagationStartActivity>();
            options.RegisterActivity<CorrectionPropagationRunStoreActivity>();
            options.RegisterActivity<CorrectionPropagationCompleteActivity>();
            options.RegisterActivity<CorrectionPropagationDelayActivity>();
        });

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
