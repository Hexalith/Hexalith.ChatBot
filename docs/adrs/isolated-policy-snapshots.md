# ADR: Isolated policy snapshots — synthetic foundation, live writes denied

## Status

Partially implemented; Story 1.6 remains in progress. A6 and native
isolation acceptance are incomplete. This decision does not authorize live policy
persistence, onboarding, pilot data, compliance claims, or production readiness.

## Context

A6 is the data-protection release gate: the `openBlockers` entry with `gate: "A6"`
in [`release-metadata.json`](../../release-metadata.json) requires accepted
data-protection and owner-runtime evidence for the exact candidate.

## Decision

The gate covers commands that propose a replacement tenant policy or configuration
snapshot, plus policy approval and direct threshold activation. Its eight members are:

- `SubmitTenantPolicyChange`
- `ApproveTenantPolicyChange`
- `SetAssociationConfidenceThresholds`
- `SubmitEscalationPolicyChange`
- `SubmitNotificationRoutingChange`
- `SubmitMailboxConfigurationChange`
- `SubmitRetentionConfigurationChange`
- `SubmitDataClassInventoryChange`

The persistence gate covers every command the audit map classifies as
`tenant-policy-mutation`, unioned with every Contracts `IChatBotCommand` that has a
public `Proposed*SnapshotId`, `Pending*SnapshotId`, or `Activated*SnapshotId` property.
The guard test
`TenantPolicyPersistenceGateTests.GateMustDenyEveryAuditClassifiedOrStructuralPolicySnapshotCommand`
discovers that structural set independently of the gate and asserts it is nonempty
before checking denial coverage.
The retention and inventory contracts and their suspended admission are also
documented in [`data-class-inventory-and-retention-policy.md`](data-class-inventory-and-retention-policy.md).

The [2026-10-17 Story 1.6 code-review scope decision](../../_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md#review-findings-round-14)
keeps the per-target `Submit*RateLimit` and disable/quarantine controls for AI
actors, service clients, mailbox sources, command capabilities and outbound
channels outside this gate: these are operational controls with
no proposed snapshot or old/new snapshot fingerprints. `SubmitConsentLawfulBasisRecord`
also stays outside the gate because it records per-subject consent, rather than
proposing a replacement tenant policy or configuration snapshot.
`SubmitTenantExportRequest` and `SubmitDeletionErasureRequest` also stay outside
this gate because they reference snapshot identifiers rather than propose replacements.
This exclusion does not establish A6 acceptance for either workflow.

All eight commands are denied before admission reservation or dispatch. This applies to legacy HTTP requests, canonical requests, SDK `/process`
requests, and correctly signed admission markers. A marker proves transport
integrity; it is not protection or isolation approval. HTTP admission runs the gate
after authentication, tenant binding (including payload tenant targets) and grant
authorization, so those denials keep their reason and spike counter. SDK `/process`
runs it after authentication and the envelope tenant check but before payload
validation, the canonical-target check, payload tenant-target binding and grant
authorization. A caller whose envelope subject and tenant match therefore receives
`policy-snapshot-unavailable`, recorded without a spike-counter increment, where
the ungated path would return another reason. A malformed payload would get
`invalid_command_payload`, which would record no authorization-failure fact and no
increment. `SubmitEscalationPolicyChange`, `SubmitMailboxConfigurationChange`,
`SubmitRetentionConfigurationChange` and `SubmitDataClassInventoryChange` have no
canonical dispatch target, so every other unmarked request for them would get
`authorization_denied` at the canonical-target check. For the other four, a
misrouted aggregate would get `authorization_denied`, a payload naming another tenant
would get `tenant_mismatch`, and a caller without a grant, such as a relayed
`service-account-*` subject, would get the command's authorization reason:
`notification_routing_unauthorized` for `SubmitNotificationRoutingChange`,
otherwise `threshold_policy_unauthorized`. Each of those authorization reasons
would record the fact and increment the spike counter. This order is decided; the
request is denied either way. The SDK check is the only gate on the validly marked
`/process` path for these commands: without it, an unmarked request would still
be denied before admission (at the canonical-target check, by authorization, or
by the shared pipeline gate), but a validly marked command would return accepted
without reaching the pipeline gate. Denials record the existing
metadata-only authorization-failure fact with `policy-snapshot-unavailable`, mapped
by the safe-message catalog to `dependency_degraded`. No protected policy values
are included in that outcome. Denials with `policy-snapshot-unavailable` do not
increment the authorization-failure spike counter. These facts are currently kept
in process; durable auditable attempts are deferred to Story 1.9. On SDK `/process`,
the denial also returns the metadata-only `ChatBotDomainServiceAdmissionRejected`
event. EventStore treats it as an ordinary domain rejection: it persists and
publishes the event on the target stream and returns that cached terminal rejection
to a retry with the same message ID, so an SDK producer gets no retryable outcome,
unlike the gateway 503 below. The event carries no policy values.

The gateway reports this denial as a retryable HTTP 503 `dependency_degraded` with
`retryable: true` and `clientAction: retry-later` by design. Policy persistence keeps
being denied until accepted A6 protection and native isolation evidence exist.
An unresolved-policy AI decision retains `policy-snapshot-unavailable` internally;
the caller sees non-retryable HTTP 403 `refusal_blocked_action`. These blocked AI
outcomes currently record no auditable attempt; that work is deferred to Story 1.9.
The gateway 503 and the AI-path 403 differ intentionally.

The internal snapshot, history fold, and resolver are explicitly synthetic. They
have no DI registration, store, event subscription, or runtime persistence path.
Snapshot construction copies nested lists and dictionaries, validates the closed
policy schema, and retains tenant, snapshot ID, policy/schema versions, effective
UTC, actor, provenance, redaction, retention, legal hold, export/deletion, backup,
encryption, supersession, and rollback references. All protection references are
unaccepted synthetic metadata; the model cannot confer A6 acceptance. Diagnostics
must label this material synthetic and must not export policy values or secrets.

History is append-only: exact equivalent ID/version reuse replays the original;
conflicting reuse retains it without change. Replacement and rollback append new
versions with later effective times and explicit predecessor references. A
rollback also identifies its historical source and must reproduce that source's
values and schema. Historical decision references remain exact tenant/ID/version
tuples. The synthetic resolver requires trusted tenant context, authorization,
fresh evidence, and a single exact match; it returns no default on ambiguity,
malformed references, missing versions, or foreign tenants. The AI evaluator also
matches the trusted request tenant and the requested snapshot ID, requires a
positive resolved policy version, and retains that exact tuple in resolved decisions.
Carrying it into approval results and events is deferred to Story 1.6's open
live-persistence scope, tracked in [`deferred-work.md`](../../_bmad-output/implementation-artifacts/deferred-work.md).
The request carries no expected version. The evaluator returns a redacted unavailable outcome for unresolved,
stale, invalid, or foreign snapshots. The production snapshot
provider remains unavailable. A future live provider must resolve the tenant's
current effective snapshot for new decisions and deny a caller-selected superseded
snapshot as stale. Exact historical resolution remains available only for audit
and replay.

## Consequences

Live persistence can be designed only after accepted data-class protection
evidence and independently witnessed native isolation proof are available. Future
evidence must bind the exact provider/candidate, trusted tenant-derived physical
partition or namespace, supported write/read paths and ACLs, protection and key
custody, redaction, retention/hold precedence, export/deletion, backup propagation,
restoration and crypto-erasure obligations. Native adversarial tests must prove
persisted end-state for two tenants, including cross-tenant writes/reads, replay,
projections, caches, exports, backups and recovery. Missing, stale, failing, or
unavailable proof or storage must deny before mutation. Prefixes, filters,
admission markers, flags, and these synthetic tests cannot satisfy that gate.

The evaluator now fails closed: a missing, invalid or stale AI policy snapshot is
`Blocked` (`policy-snapshot-unavailable`) instead of routed to approval, and early
routes no longer echo the requested snapshot id. This outcome change is intentional
and signed off by the story owner; no code or contract consumed the old reason codes.
This fail-closed outcome supersedes Story 7.2 AC4's route-to-approval requirement
for unavailable, invalid or stale policy; the Story 7.2 artifact remains a historical record.

Any future domain write must use Hexalith.EventStore; queries, projections, read
models, and cursors must use the domain-service SDK seams. There is no history
migration or sibling change in this work. Atomic command admission and mutation /
audit commitment remain Stories 1.7 and 1.8. Completion also requires the native
persisted-end-state evidence described above and a passing TE-2 completion contract
under [`story-evidence-integrity.md`](../story-evidence-integrity.md).
