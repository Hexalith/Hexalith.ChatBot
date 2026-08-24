---
title: Addendum - Hexalith.ChatBot PRD
status: approved
created: "2026-05-28"
updated: "2026-05-28"
approvedAt: "2026-06-09"
approvalScope: "Binding PRD implementation context for confidence thresholds, risk classifier, command allowlists, tenant policy schema, shared command pipeline, idempotency keys, replay isolation, ID evolution, inbound authenticity, and operating baselines."
---

# Addendum — Hexalith.ChatBot PRD

This addendum holds depth that belongs in a downstream document (architecture, solution design, story context) or that earned a place but does not fit the PRD's main narrative. Audit and override information lives in `.decision-log.md`, not here.

## Confidence Thresholds (T_high / T_low)

Referenced from FR9 and §Increment M0.

- **Score domain:** the association confidence score is a value in `[0.0, 1.0]` produced by the deterministic-signals scorer described in `§Risk Classifier`. The same scoring kernel produces both association confidence and risk classification; the calibration targets differ.
- **Signals fed to the score (M0 set):** explicit project-identifier match (weight class A, deterministic), mailbox-routing-rule match (weight class A, deterministic), conversation/thread-identifier match (weight class A, deterministic). M0 does not score learned features; learned signals (sender history, attachment metadata patterns, prior correction history) enter the kernel in M1 with separate calibration.
- **Safe initial defaults:** `T_high = 0.90` and `T_low = 0.60` for M0. A score `≥ T_high` triggers automatic association; a score `< T_low` fails closed to NeedsReview with the full candidate list; a score in `[T_low, T_high)` produces an ambiguous-association decision routed to UI review.
- **Calibration protocol:** thresholds are calibrated against the A9a evaluation dataset before each pilot phase. Calibration targets: precision ≥ 95% for auto-association, recall ≥ 90% across the ambiguous + auto-associated set, zero critical false-positives where "critical" is defined as auto-association of a message into a project the sender is not authorized to read.
- **Guardrail on threshold changes:** any tenant-policy change to `T_high` or `T_low` is treated as a security-sensitive operation. It requires tenant-admin authorization, produces an audit event, cannot be performed by service clients or AI actors, and cannot lower `T_high` below `0.80` or `T_low` below `0.50` in M0 without a corresponding documented evaluation run.
- **Failure modes:** if the scoring kernel returns an error or non-finite value, the message fails closed to NeedsReview with the candidate list empty and the failure event audited.

## Risk Classifier

Referenced from FR39 and §Increment M0.

- **M0 mechanism:** tag-and-heuristic classifier. The classifier reads (a) the command being proposed, (b) the tenant-policy classification of that command (low-risk / approval-required / disallowed), (c) the AI action's effect surface (read-only / writes project state / sends external communication / exposes file contents / creates or assigns tasks / invokes external tools / acts on behalf of a participant), and (d) the requester's authority class. The output is `low-risk` or `approval-required`; there is no `disallowed` output — disallowed commands are rejected before classification.
- **M1 evolution:** the classifier remains tag-and-heuristic; an optional LLM-assisted explanation layer produces reviewer-facing risk rationales when enabled but does not change the classification.
- **Misclassification fallback:** if the classifier returns an indeterminate result (missing tags, unknown effect surface, undeclared authority class), the action is treated as `approval-required` (fail-closed to review).
- **Reviewer-disagreement audit chain:** when a reviewer rejects or modifies an action the classifier marked `low-risk`, or approves an action the classifier marked `approval-required` outside the allowed override path, an audit event records the disagreement with the classifier version, the input tuple, the classification, the reviewer decision, and the resolution. These events feed the calibration cycle in A9a.
- **Error rate as first-class risk:** the classifier's misclassification rate is tracked as a production observable per NFR50a, with a target of ≤ 1% misclassification on the evaluation dataset and ≤ 2% on production-sampled disagreements.

## Command Allowlist v0 (M0)

Referenced from §Increment M0 and FR43.

- **M0 allowlist (exactly one command):** `Project.AppendConversationMessage` — appends the AI-action result as a conversation message in the associated project. This is a state-mutating write to Hexalith.Conversations (it creates a new conversation message record) but is bounded to: in-tenant scope, in-project scope, append-only, no outbound communication, no file mutation, no task creation, no external tool invocation, and no participant-impersonating behavior. Risk classification: `approval-required` by default — pilot reviewers approve each appended AI message before it lands in the conversation. This is the single command exercised through the M0 vertical loop.
- **Out of M0:** all other Hexalith service commands. They exist in the catalog but cannot be invoked by the AI actor in M0; they are invoked by human users through their direct UI/CLI/MCP paths once those paths exist (M1).
- **Change control:** the M0 allowlist is checked in alongside the PRD. Changes require PRD update, decision-log entry, and re-validation against the evaluation dataset.

## Command Allowlist v1 (M1)

Referenced from §Increment M1.

- **M1 allowlist:** the full catalog of Hexalith service commands declared in the §Command and Query Contracts section, minus any explicitly tagged `disallowed-for-AI` in tenant policy.
- **Per-command metadata required:** effect surface (read-only / write / external), authority class required, default risk classification (`low-risk` / `approval-required`), per-tenant override hook, idempotency key contract (see §Idempotency Keys).
- **Versioning:** the allowlist is versioned. A version increment is required when a command is added, removed, or has its default risk classification changed. Each version is checked in; deployed versions are recorded in `.decision-log.md`.
- **Change control:** changes require security-engineer sign-off in M1; in M2 they additionally require a passing run against the A9a evaluation dataset's command-coverage subset.

## Tenant Policy Schema

Referenced from many NFRs (NFR9, NFR23, etc.) and §Increment M1.

The Tenant Policy Schema is the master list of knobs an administrator can configure. It is a first-class versioned artifact under change control. Each entry declares:

- **Knob name** (stable identifier, kebab-case).
- **Type and allowed values.**
- **Safe default** (the value applied when the knob is unset or invalid).
- **Sensitivity class** (low / standard / security-sensitive). Security-sensitive changes require tenant-admin authorization and produce audit events; service clients and AI actors cannot change them.
- **Increment of introduction** (M0 / M1 / M2).
- **Validation rule** (machine-checkable predicate that rejects unsafe combinations).

**M0 knob set (minimum to ship):**
- `association.t-high` (float `[0.80, 1.00]`, default `0.90`, security-sensitive).
- `association.t-low` (float `[0.50, t-high)`, default `0.60`, security-sensitive).
- `attachments.unsafe-handling` (enum `quarantine | block | reject-message`, default `quarantine`, standard).
- `ai-action.low-risk-allowed` (map of `action-class → bool`, where action-class is one of the six risky-action categories named in FR41: `modifies-state`, `exposes-files`, `sends-external`, `creates-tasks`, `invokes-tools`, `acts-on-behalf`. Default for every class is `false`. Security-sensitive — opt-in per class, not opt-out. The per-class map shape is required by the `[NOTE FOR PM]` tuning rule near FR41: pilot operability data identifies which classes consistently approve without revision, and ratcheting is per-class, not global.).
- `mailbox.routing-rules` (list of routing-rule objects, default empty, standard).

**M1 knob set adds** policy knobs for approval routing (per role / project / action type / recipient / risk class), admin permission scopes, allowlist version pin, classifier explanation layer toggle, and the inbound-authenticity strictness level.

**M2 knob set adds** dashboard visibility scopes, replay/simulation toggles, retention overrides bounded by NFR49a, and the idempotency replay window per operation class.

Tenants cannot define new knobs; the schema is closed at the product level. Tenants can only set values within the declared types and ranges.

## Shared Command Pipeline (architectural invariant for FR81a)

Referenced from §Increment M1 and FR81a.

- **Invariant:** every state-mutating operation, regardless of the originating surface (UI, CLI, MCP, service client, AI actor, background worker), enters the system through one command spine. The ChatBot admission layer applies, in order: authentication, tenant-scope binding, authorization, risk classification, approval gate, coarse idempotency check, pre-commit audit gate, EventStore command execution (including fine idempotency), event publication, projection update, and post-commit audit emission.
- **Construction:** surface adapters (UI controller, CLI command, MCP tool, service-client SDK, AI-actor mediator) translate surface-specific input into a typed Command record and hand it to the pipeline. Adapters MUST NOT replicate any pipeline stage; in particular, adapters cannot authorize, classify risk, or write audit records.
- **Parity follows by construction:** because every surface hits the same pipeline, parity is a property of the architecture, not a property of the test suite. The contract tests in FR82–FR86 verify the invariant (each surface's adapter, when handed an equivalent input, produces the same Command record); they do not enforce it.
- **Parity violation = invariant violation.** Any adapter that bypasses a pipeline stage is a defect, not a feature gap. The architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale.

## Idempotency Keys (per operation class)

Referenced from NFR13a / FR90 and §Increment M2.

| Operation class | Key composition | Replay window | Equivalence rule | Conflict response |
|---|---|---|---|---|
| Message intake | `tenant_id + mailbox_id + provider_message_id` | indefinite (provider-message-id is unique per mailbox) | byte-identical message body and headers | suppress duplicate; audit suppression event |
| Association decision | `tenant_id + message_id + decision_actor + decision_kind` | 24h | same decision_kind on same message by same actor | reject second decision; surface "already decided" |
| Approval decision | `tenant_id + ai_action_id + decision_actor + decision_kind` | 24h | same decision_kind on same ai_action_id | reject second decision; audit |
| Command execution | `tenant_id + command_name + command_input_hash + requester_id` | 60s | byte-identical command input | return prior outcome; do not re-execute |
| Outbound send | `tenant_id + outbound_draft_id + send_actor` | indefinite (drafts are single-shot) | same send_actor on same draft | reject; surface "already sent" |
| AI action proposal | `tenant_id + project_id + ai_actor_id + intent_hash + input_files_hash` | 5min | byte-identical proposal inputs | return prior proposal; do not re-propose |
| Correction | `tenant_id + message_id + correction_actor + correction_kind` | indefinite | same correction_kind | reject; surface "already corrected" |
| Retry | `tenant_id + failed_event_id + retry_actor` | indefinite | same actor retrying same failed event | reject; audit |

Equivalence rule "byte-identical" includes canonical-form normalization (key ordering, whitespace) defined per Command record schema. The 60s/5min/24h windows are starter values — pilot operability data calibrates them in M2.

## Replay Isolation

Referenced from FR95a and §Increment M2.

- **Architectural enforcement:** replay and simulation runs execute against a separate test tenant. The outbound adapter for the test tenant is the test-mode adapter, which intercepts every outbound send, records the would-have-sent envelope to the test-tenant's outbound-trace store, and returns success without contacting any external system. Production tenants do not have access to the test-mode adapter.
- **Audit distinguishability:** replay events carry a `replay_run_id` field in their audit envelope; production audit queries default to excluding replay events. Audit-completeness measurement (NFR50a) excludes replay events from numerator and denominator.
- **Verification:** an automated test confirms that no replay run has ever produced a record in any production tenant's outbound-trace store. The test runs nightly and gates M2 release.

## ID Evolution Contract

Referenced from §Data Governance Surface and §Increment M1 / M2 (handles the case where a sibling bounded context renames, splits, merges, or deprecates an identifier ChatBot has audit records against).

When a sibling bounded context (Hexalith.Projects, Hexalith.Folders, Hexalith.Parties, Hexalith.Conversations) renames, splits, merges, or deprecates an identifier referenced by ChatBot audit records:

- **Notification:** the sibling context emits an `IdentityEvolved` event (rename / split / merge / deprecate) carrying old ID, new ID(s), reason, effective timestamp.
- **ChatBot response:** ChatBot subscribes to `IdentityEvolved` events. On receipt, it records a `ProjectionIdentityMigration` record in its own audit store that maps old-ID → new-ID(s) and records the reason. The original audit records are NOT mutated; they continue to reference the old ID.
- **Reconstruction at query time:** when an audit query encounters an old ID, the projection layer joins through `ProjectionIdentityMigration` to surface the current ID alongside the original. Both are returned to the caller; the caller decides which to display.
- **Split case:** when a sibling splits one ID into multiple, ChatBot's audit query returns all successors; the caller resolves ambiguity (typically by also reading the original context's evidence).
- **Deprecation case:** a deprecated ID continues to resolve in audit queries; new operations against the deprecated ID are rejected at the command boundary with a typed error pointing to the migration record.

## Inbound Message Authenticity

Referenced from FR48a–FR48d and §Increment M1.

- **DMARC / DKIM / SPF:** the M365 / Exchange adapter passes through the provider's DMARC/DKIM/SPF verdict for every inbound message. ChatBot does not re-verify (the provider is the source of truth), but it records the verdict in the message intake audit event and applies the tenant policy's `mailbox.authenticity-strictness` knob to decide whether to associate (`permissive`), route to NeedsReview (`strict`), or fail closed (`paranoid`).
- **Header inspection:** the adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` headers and records discrepancies (e.g., `Sender` and `From` disagree, `Reply-To` differs from `From`) as message intake metadata. Discrepancies do not block ingestion at the adapter; the risk classifier weights them.
- **On-behalf-of disambiguation:** when the M365 permission model expresses delegated send (send-on-behalf), the sender authority recorded in the audit is the on-behalf-of identity (the delegate), and the principal identity (the mailbox owner) is recorded as `principal_for`. Sender-authority mapping for outbound actions follows the same rule.
- **External-sender posture:** messages from external senders (no tenant party match) carry an `external_sender = true` flag through the pipeline. The risk classifier and the approval policy both reference this flag.

### Authority class mapping (FR48 five-class taxonomy)

FR48 declares five outbound sender-authority classes. Each maps to a specific M365 / Exchange permission posture and to a ChatBot-side authorization requirement. The mapping rule is fixed; per-tenant policy can disable a class (`tenant-policy.outbound.<class>-allowed = false`) but cannot redefine its meaning.

| FR48 authority class | M365 / Exchange permission posture | ChatBot-side authorization required | Audit fields |
|---|---|---|---|
| `draft-only` | none (action does not leave ChatBot) | requester has project authority + outbound-draft scope | requester, project, draft_id |
| `authenticated-user send` | the requesting user has `Mail.Send` for their own mailbox | requester is the mailbox owner and holds outbound-send scope; no delegation | requester, mailbox, recipients |
| `shared-mailbox send` | the requesting user is a member of a shared mailbox with `Mail.Send` | requester is on the shared mailbox membership list and holds outbound-send scope; shared-mailbox role recorded | requester, shared_mailbox, members_at_send, recipients |
| `send-on-behalf` | M365 grants the requester `Mail.Send.Shared` or send-on-behalf on a delegating mailbox owner | both requester and `principal_for` identity are recorded; tenant policy `outbound.send-on-behalf-allowed = true`; the principal has not revoked delegation since policy snapshot | requester, principal_for, recipients, delegation_evidence |
| `approved service-send` | a service account with `Mail.Send` and an explicit ChatBot service-client grant | service client is on the allowlist for outbound; requester is the originating human or AI actor that proposed the send; the approval record is in the audit chain | service_client, originating_requester, approval_id, recipients |

**Conflict resolution rules:**
- If M365 grants `send-on-behalf` but tenant policy `outbound.send-on-behalf-allowed = false`, the action fails closed with `policy-blocked` (per FR77 catalog).
- If M365 grants `send-on-behalf` to delegate A, but the proposed action's requester is delegate B, the action fails closed with `delegation-mismatch`.
- If a `shared-mailbox send` is attempted by a member whose membership lapsed between policy snapshot and command execution, the action fails closed with `membership-revoked`; the membership-at-send is recorded for audit.
- If `approved service-send` is attempted without a paired approval record in the audit chain, the action fails closed with `approval-missing`. There is no permitted code path where a service client sends outbound without a prior approval record.

## Operating Baselines

Referenced from NFR42a.

This section is **published at M2 release** (Story 8.3) with the starter SLO catalog below. The M0/M1 SLO defaults live in NFR24–NFR27 and NFR43; pilot calibration runs against A11 baseline measurements and fills the `calibration-pending` targets with per-tenant overrides recorded here.

> **Single source of truth:** the code catalog (`Hexalith.ChatBot.Contracts.Queries.OperatingBaselineCatalog`) is authoritative; this table mirrors it and a drift test asserts the metric-name set matches. Targets/error budgets shown as `calibration-pending` are filled after the A11 baseline run (calibration source `a11-pending`). Tokens are ASCII-safe (`le` = ≤, `gt` = &gt;) so they pass the published-SLO contract validator.

Per SLO entry, the recorded fields are:

- **Metric name** (stable identifier).
- **Target** (numeric, with units).
- **Measurement window** (e.g., rolling 7 days, p95 over 24h).
- **Error budget** (the fraction of the window the SLO may be missed before incident).
- **Alert threshold** (the budget consumption that fires an alert).
- **Calibration source** (A11 baseline run that derived the target, with timestamp).
- **Tenant scope** (per-tenant override or platform-wide default).

The SLO catalog covers, at minimum: ingestion latency, candidate generation latency, ambiguous-resolution time, command latency per command class, audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval queue p95 age per risk class, AI mediation latency, correction propagation latency (per NFR17a), and the FR81a shared-pipeline overhead per surface.

**Published SLO catalog (M2 starter values):**

| Metric name | Target | Measurement window | Error budget | Alert threshold | Calibration source | Tenant scope |
| --- | --- | --- | --- | --- | --- | --- |
| `chatbot.command.execution.latency` | `p95-le-2000ms` | `rolling-24h` | `calibration-pending` | `budget-burn` | `nfr24` | `platform-default` |
| `chatbot.association.latency` | `p95-le-10000ms` | `rolling-24h` | `calibration-pending` | `budget-burn` | `nfr25` | `platform-default` |
| `chatbot.operation.identity.latency` | `p95-le-5000ms` | `rolling-24h` | `calibration-pending` | `budget-burn` | `nfr26` | `platform-default` |
| `chatbot.correction.propagation.latency` | `p95-le-10m` | `rolling-24h` | `calibration-pending` | `budget-burn` | `nfr17a` | `platform-default` |
| `chatbot.audit.projection.lag` | `p95-le-5m` | `rolling-24h` | `degraded-100ev-failed-1000ev` | `lag-gt-5m` | `nfr43` | `platform-default` |
| `chatbot.retry.exhausted` | `on-exhaustion` | `rolling-24h` | `calibration-pending` | `any-exhaustion` | `nfr43` | `platform-default` |
| `chatbot.approval.queue.age` | `p95-le-2-business-days` | `rolling-7d` | `calibration-pending` | `age-gt-2-business-days` | `nfr43` | `platform-default` |
| `chatbot.mailbox.subscription.expiry` | `expiry-le-7d` | `rolling-7d` | `calibration-pending` | `expiry-le-7d` | `nfr43` | `platform-default` |
| `chatbot.ingestion.latency` | `calibration-pending` | `rolling-24h` | `calibration-pending` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.ambiguous.resolution.time` | `calibration-pending` | `rolling-7d` | `calibration-pending` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.duplicate.suppressed` | `calibration-pending` | `rolling-24h` | `calibration-pending` | `spike-baseline` | `a11-pending` | `platform-default` |
| `chatbot.mailbox.failure.rate` | `calibration-pending` | `rolling-24h` | `calibration-pending` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.ai.mediation.latency` | `calibration-pending` | `rolling-24h` | `calibration-pending` | `budget-burn` | `a11-pending` | `platform-default` |

### Recovery-validation commitments

This subsection is **not** part of the SLO catalog above: its rows are governance decisions on A10/NFR56, NFR57 and NFR41, they are not mirrored by `OperatingBaselineCatalog`, and no drift test covers them. Recovery targets have a single runtime source of truth in `RecoveryTargets` (`MaxRpo`, `MaxRto`, `MaxScopeRecordingLatency`); this table records their decision status, not their values.

Story 12.15 retained the A10/NFR56 numeric targets and did not ratify them. **No measured figure is published.** The story's local diagnostic bundle predates the current evidence-manifest contract and cannot be replayed through the shipped evidence gate, so its run identifier and values were withdrawn rather than restated with caveats. The scheduled CI and release lanes are configured to retain complete reports, manifests and raw output for 30 days, but the gate's `MaximumEvidenceAge` is 8 days — an artifact aged 8–30 days is downloadable but not citable. A successful hosted locator is necessary evidence, not ratification by itself: the 4-hour RTO needs a full-window lane or separate pre-production drill, and the 15-minute RPO needs a citable non-constant loss-path measurement. A10 remains provisional until those bounds and the locator are recorded in `.decision-log.md`.

| Requirement | Commitment / target | Story 12.15 status | Decision scope |
| --- | --- | --- | --- |
| A10 / NFR56 | RPO ≤ 15 minutes; RTO ≤ 4 hours | Live fault injection stood up for the EventStore outage and the composed M365 subscription path. RPO measurement re-opened: it is a constant on the no-loss path, so the 15-minute target is only evaluated on an already-breached run. RTO is bounded by the lane's 180-second measurable ceiling and cannot demonstrate a miss of 4 hours. | Provisional; external M365, durable WORM, production control, production scale and the measurable ceiling remain residual. |
| NFR57 | Projection rebuild ≤ 4 hours | The driver writes and reads back separate persisted projection partitions through the production read-model abstractions and ETag-cleans the fresh one. Source-email records are reconstructed as `MailboxMessageIntakeCaptured` events and replayed through the real `AssociationProjectionHandler`, so source-email divergence is reachable. Governed/WORM projections remain identity-written, so full immutable-source-plus-WORM equivalence is still residual `RV-REBUILD-WORM`. No duration published. | Provisional; not an A10 value. Bounded by the same measurable ceiling. |
| NFR41 | Dependency/scope recording ≤ 5 minutes | Re-opened for all six dependencies. Observed scope now comes from an independently keyed fault signal and the sandbox monitor polls on a 200 ms timer, so the former expectation-copy and same-tick channel tautologies are removed. The stamps are still sandbox-originated rather than product-monitoring evidence; no latency figure is published, and missing monitoring evidence is `unmeasurable`. | Not established; not an A10 value. |

The **current error-budget burn** per SLO is surfaced (coarse `within-budget` / `approaching` / `exhausted` / `unknown`) on the per-tenant operational dashboard to authorized operators only (NFR38); it reports `unknown` whenever its live signal is not yet wired (today only `chatbot.audit.projection.lag` has a live signal). SLO **alerting** on threshold breach is Story 8.4.
