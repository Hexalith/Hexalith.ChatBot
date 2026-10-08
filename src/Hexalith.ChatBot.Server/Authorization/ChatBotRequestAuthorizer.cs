using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Authorizes exact operations against current owner evidence before any protected access.</summary>
internal sealed class ChatBotRequestAuthorizer(ChatBotAuthorityCatalog catalog, IChatBotOwnerAuthorityProvider owners, ISystemClock clock, ServiceClientGrantProjectionCache grants, ILogger<ChatBotRequestAuthorizer>? logger = null)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    /// <summary>Recognizes only operations covered by the closed catalog.</summary>
    public bool IsKnownOperation(string operation, bool isQuery) => catalog.Find(operation, isQuery) is not null;

    /// <summary>Checks retained authority immediately before protected effects or disclosure.</summary>
    public bool IsCurrent(ChatBotAuthorityPrincipal principal) => principal.IsCurrent(clock.UtcNow);

    /// <summary>Collects current project evidence and jointly validates its complete set before disclosure.</summary>
    public async ValueTask<bool> HasProjectAuthoritiesAsync(ChatBotRequestContext context, IEnumerable<string> projects, string operation, CancellationToken cancellationToken, ChatBotAuthorityPrincipal? principal = null)
    {
        string[] exactProjects = projects.Distinct(StringComparer.Ordinal).ToArray();
        if (context.TenantId is null || exactProjects.Length == 0 || exactProjects.Any(static project => !AuditMetadata.IsSafeStableIdentifier(project) || project == "*") || catalog.Find(operation, true) is null)
        {
            return false;
        }

        DateTimeOffset started = clock.UtcNow;
        List<ChatBotOwnerAuthorityEvidence> evidence = [];
        foreach (string project in exactProjects)
        {
            if (principal is not null && !IsCurrent(principal)) { return false; }
            ChatBotOwnerAuthorityEvidence? current = await GetEvidenceAsync(Request(context, "Projects", project, operation, "project", true), started, cancellationToken).ConfigureAwait(false);
            if (current is null) { return false; }
            evidence.Add(current);
        }

        DateTimeOffset decidedAt = clock.UtcNow;
        bool allowed = (principal is null || IsCurrent(principal)) && evidence.All(item => ValidateEvidence(item, item.Request, started, decidedAt));
        if (allowed) { principal?.RetainValidatedEvidence(evidence.Select(item => (item, started))); }
        return allowed;
    }

    /// <summary>Checks the closed operation row and every bound owner scope.</summary>
    public async ValueTask<ChatBotAuthorityDecision> AuthorizeAsync(ChatBotRequestContext context, string operation, bool isQuery, object? payload, CancellationToken cancellationToken, string? fallbackAggregateId = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ChatBotAuthorityRequirement? row = catalog.Find(operation, isQuery);
        if (row is null || context.TenantId is null)
        {
            return Denied(operation);
        }

        if (context.IsMachine && row.AdminScope is not null)
        {
            return Denied(operation);
        }

        JsonElement json;
        try
        {
            json = payload is JsonElement element ? element : JsonSerializer.SerializeToElement(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            return Denied(operation);
        }

        if (json.ValueKind != JsonValueKind.Object || !ValidTenantTargets(json, context.TenantId))
        {
            return Denied(operation);
        }

        string? resource = row.ResourceProperty is null ? context.TenantId : ReadString(json, row.ResourceProperty);
        if (!AuditMetadata.IsSafeStableIdentifier(resource) || resource == "*")
        {
            return Denied(operation);
        }

        DateTimeOffset started = clock.UtcNow;
        List<string> references = [];
        List<(ChatBotOwnerAuthorityEvidence Evidence, DateTimeOffset Started)> collectedEvidence = [];
        List<string> projects = [];
        List<ChatBotOwnerAuthorityRequest> requests =
        [
            Request(context, "ChatBot", resource!, operation, row.AdminScope is null ? "operation" : $"admin:{row.AdminScope}", !isQuery),
        ];
        if (!isQuery && ChatBotCanonicalDispatchTarget.IsSupported(operation))
        {
            if (!ChatBotCanonicalDispatchTarget.TryResolve(operation, json, out string? target))
            {
                return Denied(operation);
            }

            if (!string.Equals(target, resource, StringComparison.Ordinal))
            {
                requests.Add(Request(context, "ChatBot", target!, operation, "operation", true));
            }
        }
        if (!isQuery && !ChatBotCanonicalDispatchTarget.IsSupported(operation) && fallbackAggregateId is not null)
        {
            if (!AuditMetadata.IsSafeStableIdentifier(fallbackAggregateId) || fallbackAggregateId == "*") { return Denied(operation); }
            if (!string.Equals(fallbackAggregateId, resource, StringComparison.Ordinal) || row.AdminScope is not null)
            {
                requests.Add(Request(context, "ChatBot", fallbackAggregateId, operation, "operation", true));
            }
        }
        if (operation == nameof(Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry))
        {
            string? failedEvent = ReadString(json, "FailedEventId");
            if (!AuditMetadata.IsSafeStableIdentifier(failedEvent) || failedEvent == "*")
            {
                return Denied(operation);
            }

            requests.Add(Request(context, "ChatBot", failedEvent!, operation, "operation", true));
        }

        if (row.AdminScope is not null)
        {
            requests.Add(Request(context, "Tenants", context.TenantId, operation, "TenantOwner", true));
        }

        // Human identity itself is trust bearing and requires current Parties membership evidence.
        if (!context.IsMachine || row.RequiresParties)
        {
            requests.Add(Request(context, "Parties", context.SubjectId, operation, "identity", !isQuery));
        }

        CollectReferences(json, ["ProjectId", "ProjectRef", "TargetProjectId", "PriorProjectId", "ProjectScopeRef", "ProjectIds", "ProjectRefs", "ProjectScopeRefs"], projects);
        List<string> affectedResources = [];
        CollectReferences(json, ["AffectedResourceReferences"], affectedResources);
        projects.AddRange(affectedResources.Where(static reference => reference.StartsWith("project:", StringComparison.Ordinal))
            .Select(static reference => reference["project:".Length..]));
        if (row.RequiresProject && projects.Count == 0)
        {
            return Denied(operation);
        }

        if (projects.Any(static project => !AuditMetadata.IsSafeStableIdentifier(project) || project == "*"))
        {
            return Denied(operation);
        }

        foreach (string project in projects.Distinct(StringComparer.Ordinal))
        {
            requests.Add(Request(context, "Projects", project, operation, "project", !isQuery));
        }

        {
            List<string> parties = [];
            CollectReferences(json, ["PartyId", "RequesterPartyId", "TargetActorId", "RequesterId", "SourceActorId", "RecipientRefs", "RecipientPartyIds", "RecipientPartyRefs", "RequesterRef", "ApproverRef", "ResolvedPartyRef", "RecipientReferences", "AssigneeRef", "ReviewerRef", "PreviousAssigneeRef"], parties);
            foreach (string party in parties.Distinct(StringComparer.Ordinal))
            {
                if (!AuditMetadata.IsSafeStableIdentifier(party))
                {
                    return Denied(operation);
                }

                requests.Add(Request(context, "Parties", party, operation, "identity", !isQuery));
            }
        }

        foreach (ChatBotOwnerAuthorityRequest request in requests)
        {
            ChatBotOwnerAuthorityEvidence? evidence = await GetEvidenceAsync(request, started, cancellationToken).ConfigureAwait(false);
            if (evidence is null)
            {
                return new ChatBotAuthorityDecision(null, operation switch
                {
                    nameof(Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation) when request.Owner == "Projects" => ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized,
                    nameof(Hexalith.ChatBot.Contracts.Commands.CreateOutboundDraft) when request.Owner == "Projects" => Hexalith.ChatBot.Contracts.Messages.ChatBotDisabledActionReasons.InsufficientAuthority,
                    _ => Denied(operation).ReasonCode,
                }, []);
            }

            collectedEvidence.Add((evidence, started));
            references.Add($"{evidence.Request.Owner}:{evidence.EvidenceId}:{evidence.Version}");
        }

        if (context.IsMachine)
        {
            ServiceClientGrantResolution resolution = await ResolveServiceGrantResolutionCoreAsync(context, operation, isQuery, !isQuery, cancellationToken, collectedEvidence).ConfigureAwait(false);
            if (!resolution.IsResolved)
            {
                return new ChatBotAuthorityDecision(null, resolution.ReasonCode, []);
            }
            ChatBotOwnerAuthorityEvidence machineEvidence = collectedEvidence.Last().Evidence;
            references.Add($"{machineEvidence.Request.Owner}:{machineEvidence.EvidenceId}:{machineEvidence.Version}");
        }

        DateTimeOffset decidedAt = clock.UtcNow;
        if (collectedEvidence.Any(item => !ValidateEvidence(item.Evidence, item.Evidence.Request, item.Started, decidedAt) ||
            (item.Evidence.ServiceGrant is { } grant && (grant.ExpiresAt <= decidedAt || grants.IsRevoked(item.Evidence.Request, grant.GrantId) || grants.PredatesClientRevocation(item.Evidence)))))
        {
            return Denied(operation);
        }

        return new ChatBotAuthorityDecision(new ChatBotAuthorityPrincipal(context, row.AdminScope, projects, validatedEvidence: collectedEvidence, grants: grants), string.Empty, references.ToArray());
    }

    /// <summary>Resolves machine scope from current owner evidence; token grant claims cannot refresh it.</summary>
    public async ValueTask<ServiceClientGrant?> ResolveServiceGrantAsync(ChatBotRequestContext context, string operation, bool isQuery, bool requireCurrent, CancellationToken cancellationToken)
        => (await ResolveServiceGrantResolutionAsync(context, operation, isQuery, requireCurrent, cancellationToken).ConfigureAwait(false)).Grant;

    /// <summary>Retains the established safe grant denial codes while checking owner evidence.</summary>
    public ValueTask<ServiceClientGrantResolution> ResolveServiceGrantResolutionAsync(ChatBotRequestContext context, string operation, bool isQuery, bool requireCurrent, CancellationToken cancellationToken)
        => ResolveServiceGrantResolutionCoreAsync(context, operation, isQuery, requireCurrent, cancellationToken, null);

    private async ValueTask<ServiceClientGrantResolution> ResolveServiceGrantResolutionCoreAsync(ChatBotRequestContext context, string operation, bool isQuery, bool requireCurrent, CancellationToken cancellationToken,
        List<(ChatBotOwnerAuthorityEvidence Evidence, DateTimeOffset Started)>? collectedEvidence)
    {
        if (!context.IsMachine || context.TenantId is null || context.ServiceClientId is null)
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing);
        }

        ChatBotOwnerAuthorityRequest request = Request(context, "ChatBot", context.ServiceClientId, operation, "service-grant", requireCurrent);
        ChatBotOwnerAuthorityEvidence? evidence = requireCurrent ? null : grants.TryGetEvidence(request);
        DateTimeOffset started = clock.UtcNow;
        evidence ??= await GetEvidenceAsync(request, started, cancellationToken).ConfigureAwait(false);
        ServiceClientGrant? grant = evidence?.ServiceGrant;
        if (evidence is null || grant is null || !ValidateEvidence(evidence, request, started, clock.UtcNow) ||
            grant.ServiceClientId != context.ServiceClientId || !HasValidGrantMetadata(grant))
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing);
        }

        if (grant.TenantId != context.TenantId)
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch);
        }

        if (grant.SurfaceOrigin != context.Origin)
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface);
        }

        if (grant.ExpiresAt <= clock.UtcNow)
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired);
        }

        if (HasValidGrantScopes(grant) && (isQuery ? grant.AllowedQueryNames : grant.AllowedCommandNames).Contains(operation, StringComparer.Ordinal))
        {
            grants.ObserveAllowedEvidence(evidence);
        }
        if (grant.IsRevoked || grants.IsRevoked(request, grant.GrantId) || grants.PredatesClientRevocation(evidence))
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        }

        if (!HasValidGrantScopes(grant))
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
        }

        if (!(isQuery ? grant.AllowedQueryNames : grant.AllowedCommandNames).Contains(operation, StringComparer.Ordinal))
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        }

        // Freeze the owner-proven lists so subsequent provider mutation cannot widen cached authority.
        grant = grant with { Scopes = Array.AsReadOnly(grant.Scopes.ToArray()), AllowedCommandNames = Array.AsReadOnly(grant.AllowedCommandNames.ToArray()), AllowedQueryNames = Array.AsReadOnly(grant.AllowedQueryNames.ToArray()) };
        ChatBotOwnerAuthorityEvidence frozenEvidence = evidence with { ServiceGrant = grant };
        grants.Upsert(frozenEvidence);
        if (grants.IsRevoked(request, grant.GrantId) || grants.PredatesClientRevocation(frozenEvidence))
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        }

        collectedEvidence?.Add((frozenEvidence, started));
        return ServiceClientGrantResolution.Resolved(grant);
    }

    private async ValueTask<ChatBotOwnerAuthorityEvidence?> GetEvidenceAsync(ChatBotOwnerAuthorityRequest request, DateTimeOffset started, CancellationToken cancellationToken)
    {
        ChatBotOwnerAuthorityEvidence? evidence;
        try
        {
            evidence = await owners.GetAuthorityAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            ChatBotAuthorityLog.OwnerUnavailable(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatBotRequestAuthorizer>.Instance, request.Owner, request.Operation, exception.GetType().Name);
            return null;
        }

        if ((evidence?.IsRevoked == true || evidence?.ServiceGrant?.IsRevoked == true) && request.Authority == "service-grant" && evidence!.Request == request &&
            IsValidEvidence(evidence! with { Request = evidence.Request with { RequireCurrent = false }, IsAllowed = true, IsRevoked = false }, request with { RequireCurrent = false }, started, clock.UtcNow) &&
            (evidence!.ServiceGrant is null || (evidence.ServiceGrant.TenantId == request.TenantId &&
                evidence.ServiceGrant.ServiceClientId == request.ResourceId && evidence.ServiceGrant.SurfaceOrigin == request.Origin &&
                AuditMetadata.IsSafeStableIdentifier(evidence.ServiceGrant.GrantId) && !evidence.ServiceGrant.GrantId.Contains('@', StringComparison.Ordinal))))
        {
            grants.InvalidateRevocation(request.TenantId, request.ResourceId, Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigins.ToWireValue(request.Origin), evidence!.ServiceGrant?.GrantId ?? string.Empty);
        }

        if (evidence is null)
        {
            ChatBotAuthorityLog.EvidenceRejected(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatBotRequestAuthorizer>.Instance, request.Owner, request.Operation, "absent");
            return null;
        }

        return ValidateEvidence(evidence, request, started, clock.UtcNow) ? evidence : null;
    }

    private bool ValidateEvidence(ChatBotOwnerAuthorityEvidence evidence, ChatBotOwnerAuthorityRequest request, DateTimeOffset started, DateTimeOffset now)
    {
        string? reason = EvidenceRejectionReason(evidence, request, started, now);
        if (reason is null) { return true; }
        ChatBotAuthorityLog.EvidenceRejected(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatBotRequestAuthorizer>.Instance, request.Owner, request.Operation, reason);
        return false;
    }

    private static bool HasValidGrantMetadata(ServiceClientGrant grant)
        => AuditMetadata.IsSafeStableIdentifier(grant.GrantId) && !grant.GrantId.Contains('@', StringComparison.Ordinal) &&
            AuditMetadata.IsSafeStableIdentifier(grant.CommandSetVersion) && !grant.CommandSetVersion.Contains('@', StringComparison.Ordinal) &&
            Enum.IsDefined(grant.ClientClass) && grant.ExpiresAt.Offset == TimeSpan.Zero &&
            grant.Scopes is not null && grant.Scopes.Count > 0 && grant.AllowedCommandNames is not null && grant.AllowedQueryNames is not null &&
            (grant.DelegatedUserId is null || (AuditMetadata.IsSafeStableIdentifier(grant.DelegatedUserId) && !grant.DelegatedUserId.Contains('@', StringComparison.Ordinal))) &&
            (grant.OAuthGrantEvidenceFingerprint is null || (AuditMetadata.IsSafeStableIdentifier(grant.OAuthGrantEvidenceFingerprint) && !grant.OAuthGrantEvidenceFingerprint.Contains('@', StringComparison.Ordinal)));

    private bool HasValidGrantScopes(ServiceClientGrant grant)
        => grant.Scopes.All(static scope => AuditMetadata.IsSafeStableIdentifier(scope) && scope != "*" && !scope.Contains('@', StringComparison.Ordinal)) &&
            grant.AllowedCommandNames.All(name => AuditMetadata.IsSafeStableIdentifier(name) && name != "*" && catalog.Find(name, false) is not null) &&
            grant.AllowedQueryNames.All(name => AuditMetadata.IsSafeStableIdentifier(name) && name != "*" && catalog.Find(name, true) is not null);

    /// <summary>Checks the original request binding, observation, revocation freshness, and expiry bounds.</summary>
    internal static bool IsValidEvidence(ChatBotOwnerAuthorityEvidence evidence, ChatBotOwnerAuthorityRequest request, DateTimeOffset started, DateTimeOffset now)
        => EvidenceRejectionReason(evidence, request, started, now) is null;

    private static string? EvidenceRejectionReason(ChatBotOwnerAuthorityEvidence evidence, ChatBotOwnerAuthorityRequest request, DateTimeOffset started, DateTimeOffset now)
    {
        if (evidence.Request != request) { return "request-mismatch"; }
        if (!evidence.IsAllowed) { return "not-allowed"; }
        if (evidence.IsRevoked) { return "revoked"; }
        if (!AuditMetadata.IsSafeStableIdentifier(evidence.EvidenceId) || evidence.EvidenceId.Contains('@', StringComparison.Ordinal)) { return "invalid-evidence-reference"; }
        if (!AuditMetadata.IsSafeStableIdentifier(evidence.Version) || evidence.Version.Contains('@', StringComparison.Ordinal)) { return "invalid-version-reference"; }
        if (evidence.ObservedAt.Offset != TimeSpan.Zero || evidence.RevocationCheckedAt.Offset != TimeSpan.Zero || evidence.ExpiresAt.Offset != TimeSpan.Zero) { return "non-utc-timestamp"; }
        if (evidence.ObservedAt > now || evidence.RevocationCheckedAt > now) { return "future-observation"; }
        if (evidence.RevocationCheckedAt < evidence.ObservedAt) { return "invalid-revocation-order"; }
        if (now - evidence.ObservedAt >= TimeSpan.FromMinutes(5)) { return "observation-expired"; }
        if (now - evidence.RevocationCheckedAt >= TimeSpan.FromSeconds(60)) { return "revocation-expired"; }
        if (evidence.ExpiresAt <= now) { return "evidence-expired"; }
        if (request.RequireCurrent && (evidence.ObservedAt < started || evidence.RevocationCheckedAt < started)) { return "not-current"; }
        return null;
    }

    private static ChatBotOwnerAuthorityRequest Request(ChatBotRequestContext context, string owner, string resource, string operation, string authority, bool current)
        => new(owner, context.SubjectId, context.TenantId!, resource, operation, authority, context.ActorClass, context.Origin, current);

    private static ChatBotAuthorityDecision Denied(string operation) => new(null, operation switch
    {
        nameof(Hexalith.ChatBot.Contracts.Commands.AssignTenantAdminRole) or nameof(Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds)
            or nameof(Hexalith.ChatBot.Contracts.Commands.SubmitTenantPolicyChange) => ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized,
        nameof(Hexalith.ChatBot.Contracts.Commands.SubmitEscalationPolicyChange) => ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized,
        nameof(Hexalith.ChatBot.Contracts.Commands.SubmitNotificationRoutingChange) => ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized,
        _ => ChatBotAuthorizationReasonCodes.AuthorizationDenied,
    }, []);

    private static string? ReadString(JsonElement element, string name)
    {
        JsonProperty[] matches = element.EnumerateObject().Where(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 && matches[0].Value.ValueKind == JsonValueKind.String ? matches[0].Value.GetString() : null;
    }

    private static void CollectReferences(JsonElement element, string[] names, List<string> values)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                CollectReferences(item, names, values);
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null && new[] { "ResolvedPartyRef", "AssigneeRef", "ReviewerRef", "PreviousAssigneeRef" }.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement reference in property.Value.EnumerateArray())
                        {
                            values.Add(reference.ValueKind == JsonValueKind.String ? reference.GetString()! : string.Empty);
                        }
                    }
                    else
                    {
                        values.Add(property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : string.Empty);
                    }
                }

                CollectReferences(property.Value, names, values);
            }
        }
    }

    private static bool ValidTenantTargets(JsonElement element, string tenant)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().All(item => ValidTenantTargets(item, tenant));
        }

        return element.ValueKind != JsonValueKind.Object || element.EnumerateObject().All(property =>
            (!(property.Name.EndsWith("TenantId", StringComparison.OrdinalIgnoreCase) || property.Name.EndsWith("TenantRef", StringComparison.OrdinalIgnoreCase)) ||
                (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() == tenant)) && ValidTenantTargets(property.Value, tenant));
    }
}
