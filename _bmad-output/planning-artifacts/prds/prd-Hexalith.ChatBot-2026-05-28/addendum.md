---
title: Addendum - Hexalith.ChatBot PRD
status: draft
created: "2026-05-28"
updated: "2026-09-14"
approvedAt: "2026-06-09"
approvalScope: "Binding PRD implementation context for confidence thresholds, risk classifier, command allowlists, tenant policy schema, shared command pipeline, idempotency keys, replay isolation, ID evolution, inbound authenticity, and operating baselines."
---

# Addendum — Hexalith.ChatBot PRD

This addendum holds depth that belongs in a downstream document (architecture, solution design, story context) or that earned a place but does not fit the PRD's main narrative. Audit and override information lives in `.memlog.md`, not here. Source lineage and time-bounded implementation evidence live in `source-manifest.md` and `qualification-evidence.md`.

## Confidence Thresholds (T_high / T_low)

Referenced from FR9 and §Increment M0.

- **Contract:** `AssociationScorer` is a versioned deterministic-scoring contract independent of `TaskIntentDetector` and `ActionRiskClassifier`.
- **Inputs:** authorized project identifiers, mailbox routing, thread/conversation identifiers, sender/recipient Party evidence, stale/conflicting evidence flags, and the scorer version. Unauthorized evidence is excluded before scoring.
- **Output:** finite confidence in `[0.0, 1.0]`, candidate identifiers visible to the actor, evidence references, scorer version, and one typed reason code.
- **Signals fed to the score (M0 set):** explicit project-identifier match (weight class A, deterministic), mailbox-routing-rule match (weight class A, deterministic), conversation/thread-identifier match (weight class A, deterministic). M0 does not score learned features; learned signals (sender history, attachment metadata patterns, prior correction history) enter the kernel in M1 with separate calibration.
- **Canonical disposition:** `score >= T_high` with required deterministic evidence and no conflict may auto-associate. Every other outcome—`score < T_high`, no candidate, deterministic conflict, scorer error, stale evidence, or unauthorized evidence—enters `NeedsReview`; reason codes and candidate visibility distinguish the cases. `Deferred` and `Rejected` are reserved for explicit authorized human decisions.
- **Safe initial defaults:** `T_high = 0.90` and `T_low = 0.60` for M0. `T_low` controls ranking and reviewer presentation only; it does not create a second automatic disposition.
- **Calibration protocol:** thresholds are calibrated against the A9a evaluation dataset before each pilot phase. Calibration targets: precision ≥ 95% for auto-association, recall ≥ 90% across the ambiguous + auto-associated set, zero critical false-positives where "critical" is defined as auto-association of a message into a project the sender is not authorized to read.
- **Guardrail on threshold changes:** any tenant-policy change to `T_high` or `T_low` is treated as a security-sensitive operation. It requires tenant-admin authorization, produces an audit event, cannot be performed by service clients or AI actors, and cannot lower `T_high` below `0.80` or `T_low` below `0.50` in M0 without a corresponding documented evaluation run.
- **Failure modes:** if the scoring kernel returns an error or non-finite value, the message fails closed to NeedsReview with the candidate list empty and the failure event audited.

## Task-Intent Detector

Referenced from FR26 and FR35.

- **Contract:** `TaskIntentDetector` is versioned independently from association and action-risk classification.
- **Inputs:** authorized source message content and evidence offsets, tenant/project scope, detector version, and declared language.
- **Output:** one label from `informational`, `request-information`, `request-action`, or `request-decision`; confidence in `[0.0, 1.0]`; evidence offsets; and detector version. `actionable` means any of the three request labels and is not a fifth model label.
- **Qualification:** offline A9a partitions measure informational/actionable precision and recall. The deployed versioned detector artifact is the runtime dependency; the source evaluation corpus is not.
- **Failure:** missing, invalid, or unqualified detector artifacts produce `detector-unavailable` and require authorized review; they do not invoke `ActionRiskClassifier`.

## Risk Classifier

Referenced from FR39 and §Increment M0.

- **Contract:** `ActionRiskClassifier` is a versioned categorical rules contract. It does not emit a numeric confidence score and does not share a kernel with association or task-intent detection.
- **M0 mechanism:** the classifier reads the pinned AI allowlist entry, effect surface, approved tenant policy snapshot, requester authority, and project/file/recipient/tool scopes. Its output is `low-risk` or `approval-required`; `denied` and `unsupported` are pre-classification dispositions.
- **Mandatory approval:** modifying state, exposing files, sending externally, creating or assigning tasks, invoking external tools, or acting on behalf of a participant is always `approval-required`. Tenant policy cannot downgrade these effects. Only product-declared, versioned read-only/no-external-effect subtypes may be eligible for `low-risk`.
- **M1 evolution:** the classifier remains tag-and-heuristic; an optional LLM-assisted explanation layer produces reviewer-facing risk rationales when enabled but does not change the classification.
- **Misclassification fallback:** if the classifier returns an indeterminate result (missing tags, unknown effect surface, undeclared authority class), the action is treated as `approval-required` (fail-closed to review).
- **Reviewer-disagreement audit chain:** when a reviewer rejects or modifies an action the classifier marked `low-risk`, or approves an action the classifier marked `approval-required` outside the allowed override path, an audit event records the disagreement with the classifier version, the input tuple, the classification, the reviewer decision, and the resolution. These events feed the calibration cycle in A9a.
- **Error rate as first-class risk:** classifier-quality metrics are independent of audit completeness: ≤ 1% misclassification on the evaluation dataset and ≤ 2% on production-sampled reviewer disagreements, published with the deployed classifier version.

## Command Allowlist v0 (M0)

Referenced from §Increment M0 and FR43.

- **M0 allowlist (exactly one command):** `Project.AppendConversationMessage` — appends the AI-action result as a conversation message in the associated project. This is a state-mutating write to Hexalith.Conversations (it creates a new conversation message record) but is bounded to: in-tenant scope, in-project scope, append-only, no outbound communication, no file mutation, no task creation, no external tool invocation, and no participant-impersonating behavior. Risk classification: `approval-required` by default — pilot reviewers approve each appended AI message before it lands in the conversation. This is the single command exercised through the M0 vertical loop.
- **Out of M0:** all other Hexalith service commands. They exist in the catalog but cannot be invoked by the AI actor in M0; they are invoked by human users through their direct UI/CLI/MCP paths once those paths exist (M1).
- **Change control:** the M0 allowlist is checked in alongside the PRD. Changes require a PRD update, `.memlog.md` decision, and re-validation against the evaluation dataset.

## Command Allowlist v1 (M1)

Referenced from §Increment M1.

- **Deny by default:** the complete product operation catalog, per-surface exposure policy, and AI-invocable allowlist are separate artifacts. Catalog membership never grants AI access.
- **M1 AI-invocable set (exactly two commands):**

  | Command | Effect | Risk | Actor and scope | Idempotency |
  | --- | --- | --- | --- | --- |
  | `Project.AppendConversationMessage` | Append-only conversation write; no outbound/file/task/tool effect | `approval-required` | AI actor acting for an authorized requester in one tenant and Project | Stable `operation_id` and expected conversation revision |
  | `ChatBot.ExecuteLowRiskAssistance` | Read-only, no external effect; produces an attributed assistant response or proposal only | `low-risk` when its subtype is product-eligible and tenant-enabled; otherwise `approval-required` | Authorized requester and AI actor, scoped to one tenant/Project and an explicit context package | Stable `operation_id`; canonical request hash is evidence |

- **Never AI-invocable in MVP:** outbound sends, identity or role changes, tenant-policy mutation, allowlist mutation, permission/service-client grants, administrative operations, destructive file operations, and unrestricted downstream commands.
- **Per-command metadata required:** effect surface, authority class, immutable mandatory-approval flag, eligible low-risk subtype set, actor/resource scope, expected revision, stable operation identity, and audit-envelope schema.
- **Versioning:** any membership or metadata change creates a new immutable version. Tenant policy may pin an approved version or disable a member, but cannot add a command or weaken mandatory approval. Deployed versions are recorded in `.memlog.md`.
- **Change control:** changes require security-engineer sign-off in M1; in M2 they additionally require a passing run against the A9a evaluation dataset's command-coverage subset.

## Tenant Policy Schema

Referenced from many NFRs (NFR9, NFR23, etc.) and §Increment M1.

The Tenant Policy Schema is a closed, versioned product contract. Tenants can set declared values but cannot create knobs, add AI commands, redefine authority classes, or weaken mandatory approval, isolation, audit, and retention invariants. An unset or invalid policy uses its safe default; a snapshot that violates a cross-knob invariant is rejected atomically and the prior valid snapshot remains active.

| Knob | Type/range and safe default | Sensitivity / authorized mutator | Increment | Validation and failure behavior |
| --- | --- | --- | --- | --- |
| `association.t-high` | float `[0.80,1.00]`; `0.90` | security-sensitive / `policy-admin` with two-person approval | M0 | Must exceed `t-low`; invalid snapshot rejected |
| `association.t-low` | float `[0.50,t-high)`; `0.60` | security-sensitive / `policy-admin` with two-person approval | M0 | Ranking only; cannot enable auto-association |
| `attachments.unsafe-handling` | enum `quarantine|block|reject-message`; `quarantine` | standard / `policy-admin` | M0 | Unknown value fails to `quarantine` |
| `mailbox.routing-rules` | versioned list of scoped rules; empty | standard / `mailbox-admin` | M0 | Unresolved or conflicting rule enters `NeedsReview` |
| `mailbox.authenticity-strictness` | enum `strict|paranoid`; `strict` | security-sensitive / `mailbox-admin` plus `policy-admin` approval | M0 | Cannot be `permissive` in MVP; anomaly enters `NeedsReview` or is blocked |
| `ai-action.low-risk-subtypes` | subset of product-published read-only/no-external-effect subtype IDs; empty | security-sensitive / `policy-admin` with two-person approval | M0 | Cannot name a boundary-crossing effect or undeclared subtype |
| `approval.routing` | ordered role/project/action/recipient/risk rules; safest eligible human-review queue | security-sensitive / `policy-admin` | M1 | Must resolve every mandatory-approval action to an authorized reviewer or block it |
| `ai.allowlist-version` | one product-approved immutable version; current v1 | security-sensitive / `policy-admin` with security approval | M1 | Tenant may pin or disable members, never add or alter metadata |
| `classifier.explanations-enabled` | boolean; `false` | standard / `policy-admin` | M1 | Explanation cannot alter classification or expose restricted evidence |
| `outbound.authority-enabled` | subset of fixed FR48 authority classes; empty | security-sensitive / `policy-admin` with mailbox evidence | M1 | Membership/delegation revalidated at execution; mismatch blocks |
| `notification.routing` | versioned destinations by queue/severity; in-app operations queue | standard / `operations-admin` | M1 | Missing route retains in-app item and raises configuration degradation |
| `approval.priority-weights` | bounded positive weights for risk, affected authority, and age; product defaults | standard / `operations-admin` | M1 | Cannot demote mandatory-approval items below configured maximum age |
| `data.residency-region` | product-supported region ID; tenant home region | security-sensitive / `compliance-admin` with two-person approval | M2 | Unsupported migration blocks writes requiring the new region |
| `data.retention-class` | duration within NFR49a data-class bounds; product minimum | security-sensitive / `compliance-admin` | M2 | Cannot shorten legal hold or exceed approved maximum |
| `ai-context.retention` | duration within the approved AI-derived-data class; shortest product period | security-sensitive / `compliance-admin` | M2 | Cannot outlive its authorized source or legal-hold decision |
| `dashboard.visibility-scopes` | closed set of aggregate/per-item scopes; aggregate-only | security-sensitive / `operations-admin` | M2 | Per-item evidence still requires Project authority |
| `replay.enabled` | boolean; `false` | security-sensitive / `operations-admin` with two-person approval | M2 | Requires replay-safe composition and egress denial or start is rejected |
| `proposal.replay-window` | duration `[0,24h]`; `5m` | standard / `policy-admin` | M2 | Applies only to non-mutating proposals; never to durable mutation identity |
| `operational.limits` | typed mailbox/AI/command/outbound quota and circuit-breaker map; product defaults | security-sensitive / `operations-admin` | M1 | Unknown class or negative/unbounded value rejected |

Every row carries a schema version and migration rule. Schema drift, unknown knobs, invalid dependencies, and unsafe combinations are tested at every increment gate. Service clients and AI actors cannot mutate policy. All successful and rejected policy changes produce canonical audit envelopes.

## Shared Command Pipeline (architectural invariant for FR81a)

Referenced from §Increment M1 and FR81a.

- **Invariant:** every state-mutating operation, regardless of the originating surface (UI, CLI, MCP, service client, AI actor, background worker, or mailbox event), enters one command spine. Admission applies authentication, tenant binding, authorization, action-risk classification, approval validation, stable operation identity, expected-revision validation, and canonical audit-envelope construction before commit.
- **Atomic durability boundary:** the domain event, durable idempotency record, applied policy and approval references, and canonical audit envelope commit atomically in the EventStore stream or a transactional outbox committed with that stream. If any element cannot be made durable, no element commits and the caller receives a typed failure.
- **After commit:** event publication, investigation/audit projection, UI projections, and notifications consume the committed record. These views are rebuildable and governed by projection-lag SLOs; a delayed projection never turns a committed command into an unaudited mutation.
- **Construction:** surface adapters (UI controller, CLI command, MCP tool, service-client SDK, AI-actor mediator) translate surface-specific input into a typed Command record and hand it to the pipeline. Adapters MUST NOT replicate any pipeline stage; in particular, adapters cannot authorize, classify risk, or write audit records.
- **Parity follows by construction:** because every surface hits the same pipeline, parity is a property of the architecture, not a property of the test suite. The contract tests in FR82–FR86 verify the invariant (each surface's adapter, when handed an equivalent input, produces the same Command record); they do not enforce it.
- **Parity violation = invariant violation.** Any adapter that bypasses a pipeline stage is a defect, not a feature gap. The architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale.

## Idempotency Keys (per operation class)

Referenced from NFR13a / FR90 and the increment that first introduces each operation.

| Operation class | Durable identity | Equivalence rule | Conflict response |
| --- | --- | --- | --- |
| Message intake | `tenant_id + mailbox_id + provider_message_id` | Canonical content and provider identity match | Return prior outcome; differing content is `identity-conflict` |
| Durable command/mutation | Caller-issued stable `operation_id` scoped to tenant and command contract | Canonical command input and expected aggregate revision match | Return prior outcome; differing semantics is `operation-conflict` |
| Association decision | One stable `decision_slot_id` per association workflow instance | Same chosen disposition and target | Return prior decision; competing disposition is `decision-conflict` |
| Approval decision | One stable `decision_slot_id` per AI action proposal | Same decision and approved revision | Return prior decision; competing decision is `decision-conflict` |
| Outbound send | Stable `operation_id` plus single-shot draft identity | Same frozen content, recipients, authority, and approval | Return prior send outcome; drift is `operation-conflict` |
| AI action proposal | Stable `proposal_id`; optional bounded request-hash suppression | Canonical source intent/context match | Return prior proposal inside configured window; differing input creates a new proposal |
| Correction | Stable `operation_id` and predecessor workflow revision | Same replacement association and evidence | Return prior correction; competing successor is `revision-conflict` |
| Retry | Stable retry-attempt ID under the original `operation_id` | Same failed step and expected workflow revision | Return prior attempt; stale revision is rejected |

Durable identities are retained for the lifetime of the governed record; time windows never permit a durable mutation or human decision to execute twice. Canonical request hashes remain audit evidence and may suppress duplicate non-mutating proposals. Expected aggregate/workflow revision controls concurrency independently from idempotency.

## Replay Isolation

Referenced from FR95a and §Increment M2.

- **Architectural enforcement:** replay/simulation uses a separate test tenant and a replay-only composition root. Production credentials and resource locators are unavailable at construction time. Mail, AI/model/tool, command, file/state, queue, and outbound adapters are replaced with replay-safe implementations; default-deny egress prevents undeclared external contact.
- **Audit distinguishability:** replay events carry a `replay_run_id` field in their audit envelope; production audit queries default to excluding replay events. Audit-completeness measurement (NFR50a) excludes replay events from numerator and denominator.
- **Verification:** each replay records before/after fingerprints for every relevant production store and external-resource ledger, then proves invariance. Tests cover email, model/tool calls, domain commands, files/state, queues, audit targets, and outbound traces. Credential-composition, egress, or invariance failure is a stop-ship M2 defect.

## ID Evolution Contract

Referenced from §Data Governance Surface and §Increment M1 / M2 (handles the case where a sibling bounded context renames, splits, merges, or deprecates an identifier ChatBot has audit records against).

`IdentityEvolved` is a proposed external dependency, not a binding contract. Each producing context must accept a versioned schema covering old and successor IDs, evolution kind, authority, reason, effective time, ordering, replay/idempotency, compatibility rollout, and authorization. It must also expose a reconciliation query for missed events and document consumer fallback.

Until every required producer accepts that contract, ChatBot preserves original identifiers in audit, rejects operations whose current identity cannot be resolved, and routes reconciliation to authorized review. If accepted, ChatBot may project immutable migration links without rewriting historical audit records. Producer acceptance, versions, and rollout evidence are recorded in `source-manifest.md` and `.memlog.md` before binding status is restored.

## Inbound Message Authenticity

Referenced from FR48a–FR48d and §Increment M0. Provider-specific tuning and broader compatibility may extend in M1.

- **DMARC / DKIM / SPF:** the M365 / Exchange adapter passes through the provider's verdicts for every inbound message. ChatBot records them in the intake audit and applies `mailbox.authenticity-strictness`: `strict` routes unresolved/anomalous identity to `NeedsReview`; `paranoid` blocks it. MVP has no permissive mode.
- **Header inspection:** the adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` and records discrepancies. They feed intake reason codes and authorized association review; they are not inputs to the separate AI action-risk classifier.
- **On-behalf-of disambiguation:** when the M365 permission model expresses delegated send (send-on-behalf), the sender authority recorded in the audit is the on-behalf-of identity (the delegate), and the principal identity (the mailbox owner) is recorded as `principal_for`. Sender-authority mapping for outbound actions follows the same rule.
- **External-sender posture:** messages from external senders (no tenant party match) carry an `external_sender = true` flag through the pipeline. The risk classifier and the approval policy both reference this flag.

### Authority class mapping (FR48 five-class taxonomy)

FR48 declares five outbound sender-authority classes. Each maps to a specific M365 / Exchange permission posture and to a ChatBot-side authorization requirement. The mapping rule is fixed; `outbound.authority-enabled` can enable an eligible class but cannot redefine its meaning.

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

> **Authority:** approved planning values in this addendum are authoritative; the code catalog implements them and a drift test must prove conformance. `calibration-pending` means the row is unsupported for an M2 production-readiness claim until A11 supplies a numeric target, error budget, live signal, alert route, and passing burn test.

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

### Recovery completion evidence boundary

Current-run story-completion evidence and retained scheduled/release operational evidence serve separate trust purposes. Only current-run evidence bound to the exact candidate revision and approved evidence policy may satisfy the story-completion recovery gate. Only an independently governed, fresh operational bundle may inform A10. Neither substitutes for the other.

The completion gate accepts exactly one current-run recovery producer and fails on planning, production, timeout/no-test, cleanup, projection, attestation, or independent-validation failure. Its published artifact contains policy-allowed metadata only; raw test output and tenant diagnostics remain outside completion uploads. Operational recovery bundles follow the separate A10 retention and freshness policy. Exact locators, revisions, results, and expiry calculations live in `qualification-evidence.md`.

### Recovery-validation commitments

This subsection is **not** part of the SLO catalog above. It records product targets, owners, evidence kinds, freshness rules, and gate state. Implementations must conform to these approved targets; revision-specific evidence lives in `qualification-evidence.md`.

There is no fresh hosted bundle in the reconciled evidence that passes the currently required four-job recovery gate. The 2026-08-27 bundle is historical, expired on 2026-09-04, predates the controlled-loss requirement, and cannot test the four-hour RTO boundary. A10 remains provisional; details are retained in `qualification-evidence.md`.

Fresh qualification requires a hosted controlled-loss path that derives RPO from persisted EventStore bounds and an RTO-capable full-window lane or separately retained production-shaped drill. Local implementation evidence cannot ratify either target.

| Requirement | Commitment / target | Story 12.15 status | Decision scope |
| --- | --- | --- | --- |
| A10 / NFR56 | RPO ≤ 15 minutes; RTO ≤ 4 hours | No current qualifying hosted bundle | Provisional; fresh controlled-loss and RTO-capable evidence are stop-ship M2 gates. |
| NFR57 | Projection rebuild ≤ 4 hours | No current production-shaped full-window proof | Provisional; requires fresh exact-candidate evidence. |
| NFR41 | Dependency/scope recording ≤ 5 minutes | No current product-monitoring proof for every dependency | Not established; missing evidence is `unmeasurable`, never inferred from sandbox expectations. |

The per-tenant dashboard exposes `within-budget`, `approaching`, `exhausted`, or `unsupported`. `unsupported` is mandatory when a numeric target, live signal, route, or burn test is absent and blocks the associated M2 production-readiness claim.
