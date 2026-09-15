---
title: Normative PRD Appendices - Hexalith.ChatBot
status: approved
created: "2026-05-28"
updated: "2026-09-14"
approvedAt: "2026-09-14"
approvalScope: "Binding PRD implementation context for confidence thresholds and evaluation, risk classifier, approval freshness, command allowlists, tenant policy schema, release-gate records, shared command-pipeline profiles, idempotency keys, replay isolation, ID evolution, inbound authenticity, sender authority, and operating baselines."
---

# Normative PRD Appendices — Hexalith.ChatBot

These approved appendices define the normative implementation context for the finalized PRD. They are the current approval authority for the executable contract detail linked from the main document. Audit and override history lives in `.memlog.md`; source lineage and time-bounded implementation evidence live in `source-manifest.md` and `qualification-evidence.md`.

## Release authority

### Increment Gate Record Contract

Referenced from §Current Release Status, A5, A6, A9a, A10, A11, A13, and every increment gate.

Release logic consumes immutable versioned gate records rather than prose or file presence. Each record contains `gate_id`, schema version, `decision` (`approved|rejected|revoked`), exact candidate revision, every consumed dependency revision, approved environment/profile, evidence locators and hashes, owner identity, independent approver identities, signer-independence result, approval timestamp, explicit expiry timestamp, reopen predicates, superseded record ID, revocation record ID, and computed status.

Computed status is exactly `open`, `approved-current`, `expired`, `invalidated`, or `superseded`. `approved-current` requires an approved decision, valid independent signatures, exact candidate/dependency/environment match, passing required thresholds, unexpired evidence, and no matched reopen/revocation predicate. A `rejected` decision or required-threshold failure sets the computed status to `open`. A `revoked` decision or material drift in any revision, provider, policy, owner contract, data class, metric definition, supported mix, environment, or gate evidence sets it to `invalidated`. When the expiry timestamp elapses, the status becomes `expired`. Issuing a valid successor sets the predecessor’s status to `superseded`. A new immutable successor is required to close the gate again. A5/A6/A13 block M0 and are revalidated later; A9a detector/classifier records block first use at M0 and require `approved-current` exact-artifact revalidation at M1 and M2; A11-M1 blocks M1; A10 and A11-M2 additionally block M2. A9a reopens on an artifact/version/hash, dataset/version/hash, candidate/dependency binding, threshold result, production-sample window, or evidence-expiry change; a status other than `approved-current` blocks the corresponding increment and disables the affected artifact. This schema defines validity only and does not claim any currently open gate is closed.

## Association and AI decisioning

### Confidence Thresholds (T_high / T_low)

Referenced from FR9 and §Increment M0.

- **Contract:** `AssociationScorer` is a versioned deterministic-scoring contract independent of `TaskIntentDetector` and `ActionRiskClassifier`.
- **Inputs:** authorized project identifiers, mailbox routing, thread/conversation identifiers, sender/recipient Party evidence, stale/conflicting evidence flags, and the scorer version. Unauthorized evidence is excluded before scoring.
- **Output:** finite confidence in `[0.0, 1.0]`, candidate identifiers visible to the actor, evidence references, scorer version, and one typed reason code.
- **Signals fed to the score (M0 set):** explicit project-identifier match (weight class A, deterministic), mailbox-routing-rule match (weight class A, deterministic), conversation/thread-identifier match (weight class A, deterministic). M0 does not score learned features; learned signals (sender history, attachment metadata patterns, prior correction history) enter the kernel in M1 with separate calibration.
- **Canonical disposition:** `score >= T_high` with required deterministic evidence and no conflict may auto-associate. Every other outcome—`score < T_high`, no candidate, deterministic conflict, scorer error, stale evidence, or unauthorized evidence—enters `NeedsReview`; reason codes and candidate visibility distinguish the cases. `Deferred` and `Rejected` are reserved for explicit authorized human decisions.
- **Safe initial defaults:** `T_high = 0.90` and `T_low = 0.60` for M0. `T_low` controls ranking and reviewer presentation only; it does not create a second automatic disposition.
- **Calibration protocol:** thresholds are calibrated against the A9a evaluation dataset before each pilot phase. Calibration targets use §Association Evaluation Protocol: automatic-association precision ≥ 95%, recall ≥ 90% across adjudicated unambiguous messages with exactly one authorized in-scope Project, and zero critical false-positives. Ambiguous and safe-abstention populations are reported separately and never inflate association correctness.
- **Guardrail on threshold changes:** any tenant-policy change to `T_high` or `T_low` is security-sensitive and follows the corresponding Tenant Policy Schema row: an authorized `policy-admin` initiates it, an independent second authorized admin approves it, and the canonical audit envelope carries the justification. Service clients and AI actors cannot perform it. M0 cannot lower `T_high` below `0.80` or `T_low` below `0.50` without a corresponding documented evaluation run.
- **Failure modes:** if `AssociationScorer` returns an error or non-finite value, the message fails closed to `NeedsReview` with the candidate list empty and the failure event audited.

### Association Evaluation Protocol

Referenced from SM1, SM7, SM-C1, and A9a.

- **Mutually exclusive ground truth:** each message is adjudicated as `one-authorized-project-unambiguous`, `one-authorized-project-ambiguous`, `no-match`, `unauthorized-project`, `cross-tenant-reference`, `duplicate`, `attachment-only`, or `authenticity-anomaly`. A message has one primary partition; secondary tags may describe adversarial characteristics but cannot duplicate it in a denominator.
- **Formulas:** SM1 is correct final Project assignments divided by messages in the two `one-authorized-project-*` partitions. SM7 precision is correct automatic assignments divided by all automatic assignments; SM7 recall is correct automatic assignments divided by `one-authorized-project-unambiguous`. Safe abstention is the correct review/no-association disposition divided by all messages whose expected outcome is no automatic association. Wrong-Project automatic and human-confirmed results are reported separately over all adjudicated messages. Corrections do not erase an earlier wrong result.
- **Sampling and minima:** the A9a corpus has at least 500 messages for M0 and 2,000 for M1. Every named association partition has at least 30 M0 and 100 M1 examples; no single partition exceeds 40% of the corpus. Each cycle adds at least 20 new adversarial examples distributed across the affected partitions. Tenant/domain/language prevalence is recorded, and synthetic examples are labeled.
- **Adjudication:** two authorized reviewers independently assign expected Project/disposition and criticality from source evidence. Disagreement is resolved by a third Test Architect delegate; the original votes, rationale, and final label remain versioned. Evaluation code, scorer/policy versions, dataset hash, exclusions, and every confusion-matrix cell are retained.
- **Confidence:** every proportion reports a two-sided 95% Wilson confidence interval and raw numerator/denominator. The point estimate must meet its target; a lower bound more than five percentage points below target blocks the gate pending more evidence. SM-C1 remains zero-tolerance regardless of interval.

### Task-Intent Detector

Referenced from FR26 and FR35.

- **Contract:** `TaskIntentDetector` is versioned independently from association and action-risk classification.
- **Inputs:** authorized source message content and evidence offsets, tenant/project scope, detector version, and declared language.
- **Output:** one label from `informational`, `request-information`, `request-action`, or `request-decision`; confidence in `[0.0, 1.0]`; evidence offsets; and detector version. `actionable` means any of the three request labels and is not a fifth model label.
- **Qualification:** offline A9a partitions measure informational/actionable precision and recall. The deployed versioned detector artifact is the runtime dependency; the source evaluation corpus is not.
- **Failure:** missing, invalid, or unqualified detector artifacts produce `detector-unavailable` and require authorized review; they do not invoke `ActionRiskClassifier`.

### Risk Classifier

Referenced from FR39 and §Increment M0.

- **Contract:** `ActionRiskClassifier` is a versioned categorical rules contract. It does not emit a numeric confidence score and does not share a kernel with association or task-intent detection.
- **M0 mechanism:** the classifier reads the pinned AI allowlist entry, effect surface, approved tenant policy snapshot, requester authority, and project/file/recipient/tool scopes. Its output is `low-risk` or `approval-required`; `denied` and `unsupported` are pre-classification dispositions.
- **Mandatory approval:** modifying state, exposing files, sending externally, creating or assigning tasks, invoking external tools, or acting on behalf of a participant is always `approval-required`. Only a currently authenticated, authorized human with recorded user presence and `actorType=human` can approve; policy may classify, route, or deny but cannot approve or co-sign the human gate. Tenant policy cannot downgrade these effects. Only product-declared, versioned read-only/no-external-effect subtypes may be eligible for `low-risk`.
- **M1 evolution:** the classifier remains tag-and-heuristic; an optional LLM-assisted explanation layer produces reviewer-facing risk rationales when enabled but does not change the classification.
- **Misclassification fallback:** missing/invalid classifier artifacts or an indeterminate result (missing tags, unknown effect surface, undeclared authority class) returns typed `classifier-indeterminate`. It creates no proposal or durable idempotency state and cannot be approved. A separately typed security-sensitive non-mutating attempt record carries the redacted reason and remediation; after remediation, the requester submits a new linked operation that must classify determinately.
- **Reviewer-disagreement audit chain:** an audit event is required in either of two cases: a reviewer rejects or modifies an action classified as `low-risk`; or a later product version makes a previously approval-required, read-only/no-external-effect subtype eligible for low risk. The event records the classifier version, input tuple, original classification, reviewer or product decision, and resolution. Normal approval authorizes execution but does not reclassify the action. The six mandatory boundary-crossing effect classes have no downgrade or override path. These events feed the calibration cycle in A9a.
- **Error rate as first-class risk:** classifier-quality metrics are independent of audit completeness: ≤ 1% misclassification on the evaluation dataset and ≤ 2% on production-sampled reviewer disagreements, published with the deployed classifier version.

### Detector and Classifier Qualification Records

Referenced from FR35, FR39, A9a, and the M0/M1/M2 increment gates.

Each deployed `TaskIntentDetector` and `ActionRiskClassifier` version has an immutable qualification record with these fields: artifact version and hash; exact candidate and dependency revisions; A9a dataset version and hash; per-partition counts; exclusions; adjudicator identities; formulas; raw confusion matrix; numerator and denominator; point estimate and 95% interval; threshold result; owner and independent approver; approval and expiry timestamps; and computed gate status under §Increment Gate Record Contract.

- **Task-intent qualification:** `actionable` is the positive class (`request-information|request-action|request-decision`) and `informational` is negative. Precision is actionable true positives divided by all actionable predictions; recall is actionable true positives divided by all adjudicated actionable examples. Each of the four labels has at least 30 examples for M0 and 100 for M1. M0 requires precision ≥ 80% and recall ≥ 75%; M1 requires precision ≥ 90% and recall ≥ 85%.
- **Risk-classifier offline qualification:** the denominator is every determinately adjudicated A9a risk example, split between `low-risk` and `approval-required` with every mandatory boundary-crossing effect represented. Misclassification is any output differing from the independent expected class; target ≤ 1%, with zero boundary-crossing effects classified `low-risk`. Indeterminate/invalid outputs fail qualification rather than disappearing from the denominator.
- **Production-sampled disagreement:** for M1, the Test Architect independently adjudicates all reviewed approval-required proposals plus a stratified sample of at least 100 executed low-risk decisions per rolling 30-day window, or all low-risk decisions when fewer than 100 exist. The numerator is sampled decisions whose deployed class differs from the adjudicated class; the denominator is all completed adjudications in both strata. Target ≤ 2%; both strata and rates are reported separately so absence of proposal review for low-risk work cannot hide errors.

M0 requires approved-current detector and offline-classifier qualification records before their first use. M1 requires new approved-current records for the exact deployed versions plus the production-sampled result. M2 requires those records to remain `approved-current` and revalidates the M1 thresholds and current production-sampled result against the exact deployed release candidate and dependency revisions. A missed threshold or missing stratum computes the affected gate `open`; artifact/dataset drift, candidate/dependency mismatch, or material version change computes it `invalidated`; elapsed evidence expiry computes it `expired`. Every non-`approved-current` result blocks or disables the corresponding artifact under the M0/M1/M2 gate; it never falls back to an unqualified model.

### Approval Freshness

Referenced from FR41, FR42, FR50, NFR16, NFR36, and NFR48.

- **Lifetime classes:** reversible internal writes default to 15 minutes with an immutable maximum of 30 minutes. External, disclosure, policy/admin, security-sensitive, or irreversible effects default to 5 minutes with an immutable maximum of 10 minutes. Tenants may shorten but never lengthen these bounds.
- **Record:** each approved proposal stores `approved_at`, `expires_at`, effect class, proposal/content/resource digests, policy and classifier versions, target revision, sender-authority evidence where applicable, requester, human approver, `actorType=human`, human-presence evidence, and principal-independence proof.
- **Expiry and drift:** reaching `expires_at` moves `Approved -> Expired` with `AIActionApprovalExpired`. Any material drift invalidates immediately: proposal/content/resource digest, recipient, file, tool target, effect class, policy/allowlist/classifier version, requester/approver authority, sender authority, target revision, or retention/hold state.
- **After expiry:** execution and retry are denied with a stable reason. Renewal in place is forbidden; the requester creates a new linked proposal with fresh evidence and a new human decision slot.

### Untrusted Content and Instruction Authority

Referenced from FR27, FR33, NFR8, NFR9, and A5.

System/developer policy, the closed command and tool catalogs, current tenant policy, and authenticated actor/owner authority are the only instruction and authority sources. Email bodies, quoted threads, attachments, filenames, extracted text, retrieved Project context, model output, and tool results are untrusted data even when they contain instruction-like language. Every fragment retains immutable origin, source-evidence ID, trust class, redaction state, and extraction boundary through context packaging and output attribution. Untrusted content cannot alter policy, request tools outside the catalog, supply credentials/authority, approve a proposal, or suppress review. When the system detects a suspicious instruction pattern, it returns `untrusted-instruction-detected`, prevents model or tool invocation, and records a redacted, security-sensitive attempt. An authorized Project actor may then inspect the source, exclude or quarantine the fragment through its owner workflow, and submit a new request. MVP has no command that promotes external/retrieved content into a trusted instruction source. A5 qualification includes adversarial fixtures for every named origin and proves identical enforcement before and after extraction, retrieval, model invocation, and tool return.

## Command governance and execution

### Command Allowlist Contracts

Allowlist membership and metadata are immutable and versioned. Tenant policy may pin an approved version or disable a member, but cannot add a command or weaken mandatory approval. Deployed versions are recorded in `.memlog.md`.

- **M0 change control:** changes require a PRD update, a `.memlog.md` decision, and revalidation against the evaluation dataset.
- **M1/M2 change control:** changes require security-engineer sign-off in M1; in M2 they additionally require a passing run against the A9a evaluation dataset's command-coverage subset.

#### Command Allowlist v0 (M0)

Referenced from §Increment M0 and FR43.

- **M0 allowlist (exactly one command):** `Project.AppendConversationMessage` appends the AI-action result to the associated Project conversation. This command writes a conversation message to Hexalith.Conversations. It is append-only and limited to the current tenant and Project. It cannot send outbound communication, mutate files, create tasks, invoke external tools, or impersonate participants. Risk classification is `approval-required`: pilot reviewers approve each appended AI message before it lands in the conversation. This is the single command exercised through the M0 vertical loop.
- **Out of M0:** all other Hexalith service commands. They exist in the catalog but cannot be invoked by the AI actor in M0; they are invoked by human users through their direct UI/CLI/MCP paths once those paths exist (M1).
- **Checked-in authority:** the M0 allowlist is checked in alongside the PRD.

#### Command Allowlist v1 (M1)

Referenced from §Increment M1.

- **Deny by default:** the complete product operation catalog, per-surface exposure policy, and AI-invocable allowlist are separate artifacts. Catalog membership never grants AI access.
- **M1 AI-invocable set (exactly two commands):**

  | Command | Effect | Risk | Actor and scope | Idempotency |
  | --- | --- | --- | --- | --- |
  | `Project.AppendConversationMessage` | Append-only conversation write; no outbound/file/task/tool effect | `approval-required` | AI actor acting for an authorized requester in one tenant and Project | Stable `operation_id` mapped to owner idempotency plus stable message identity; current callable mapping is gated by A13 |
  | `ChatBot.ExecuteLowRiskAssistance` | Read-only/no-external-effect response; a boundary-crossing request returns the typed `approval-required` disposition and must be submitted separately through `ProposeAIAction` | `low-risk` when its subtype is product-eligible and tenant-enabled; otherwise no execution and `approval-required` | Authorized requester and AI actor, scoped to one tenant/Project and an explicit context package | Stable `operation_id`; canonical request hash is evidence |

- **Never AI-invocable in MVP:** outbound sends, identity or role changes, tenant-policy mutation, allowlist mutation, permission/service-client grants, administrative operations, destructive file operations, and unrestricted downstream commands.
- **Per-command metadata required:** effect surface, authority class, immutable mandatory-approval flag, eligible low-risk subtype set, actor/resource scope, expected revision, stable operation identity, and audit-envelope schema.

#### AI allowlist executable contract mapping

The stable product IDs above are not permission to invent downstream commands. The mapping is versioned and deny-by-default:

| Product/allowlist ID | Target contract and schema | Required field/authority mapping | Concurrency, outcome, and failure translation | Compatibility status |
| --- | --- | --- | --- | --- |
| `Project.AppendConversationMessage` | Target DTO `Hexalith.Conversations.Contracts.Commands.AppendMessageCommand`, `SchemaVersion.Current = 1`; target success event `MessageAppended`; current revision and implementation evidence in `source-manifest.md`. | `Metadata.TenantId` = trusted tenant; `Metadata.ActorPartyId` = authorized requesting Party; `Metadata.CorrelationId`/`CausationId` = ChatBot correlation/proposal; `Metadata.IdempotencyKey` = stable `operation_id`; `ConversationId` = owner-issued ID assigned to the authorized Project; `MessageId` = stable result ID; `AuthorPartyId` = registered governed-AI Party; `Text` = approved frozen result; `CallerMetadata` = bounded ChatBot/composer provenance only | The accepted producer must return `ConversationCommandAcceptedResult` or closed typed `ConversationError`; ChatBot maps duplicate to the stored logical outcome, `idempotency_conflict` to `operation-conflict`, tenant/authorization failure to safe denial, and uncertainty to retryable pending. Current concurrency and idempotency compatibility evidence is recorded in `source-manifest.md`. | **Blocked by A13** until Conversations provides and accepts an executable producer mapping with compatible audit, authority, concurrency, and retention proof. |
| `ChatBot.ExecuteLowRiskAssistance` | ChatBot `ExecuteLowRiskAssistance` v1 through the shared command pipeline | Trusted tenant/Project/requester, explicit context-package ID, product-declared subtype, policy snapshot, classifier version, stable `operation_id`; no downstream mutation target | Returns attributed response or a typed denied/unsupported/approval-required disposition. It may persist only the request/result and canonical audit bookkeeping; it cannot persist an action proposal or materialize any Project/file/task/tool/outbound effect. | **Specified for M1**; qualification remains required by A8/A9a. |

The legacy `Project.` prefix is a stable product identifier only; it does not transfer message ownership to Hexalith.Projects. Hexalith.Conversations owns the append and conversation-to-Project assignment. Any field, schema, outcome, idempotency, authority, or ownership drift fails closed and triggers the source-manifest re-check.

### Tenant Policy Schema

Referenced from many NFRs (NFR9, NFR23, etc.) and §Increment M1.

The Tenant Policy Schema is a closed, versioned product contract. Tenants can set declared values but cannot create knobs, add AI commands, redefine authority classes, or weaken mandatory approval, isolation, audit, and retention invariants. An unset or invalid policy uses its safe default; a snapshot that violates a cross-knob invariant is rejected atomically and the prior valid snapshot remains active.

| Knob | Type/range and safe default | Sensitivity / authorized mutator | Increment | Validation and failure behavior |
| --- | --- | --- | --- | --- |
| `association.t-high` | float `[0.80,1.00]`; `0.90` | security-sensitive / `policy-admin` with two-person approval | M0 | Must exceed `t-low`; invalid snapshot rejected |
| `association.t-low` | float `[0.50,t-high)`; `0.60` | security-sensitive / `policy-admin` with two-person approval | M0 | Ranking only; cannot enable auto-association |
| `attachments.unsafe-handling` | enum `quarantine|block|reject-message`; `quarantine` | standard / `policy-admin` | M0 | Unknown value fails to `quarantine` |
| `mailbox.routing-rules` | versioned list of scoped rules; empty | standard / `mailbox-admin` | M0 | Unresolved or conflicting rule enters `NeedsReview` |
| `mailbox.authenticity-strictness` | enum `strict|paranoid`; `strict` | security-sensitive / `mailbox-admin` plus `policy-admin` approval | M0 | Cannot be `permissive` in MVP; `strict` anomaly enters `AuthenticityReviewRequired`; `paranoid` anomaly enters terminal `AuthenticityBlocked` |
| `ai-action.low-risk-subtypes` | subset of product-published read-only/no-external-effect subtype IDs; empty | security-sensitive / `policy-admin` with two-person approval | M0 | Cannot name a boundary-crossing effect or undeclared subtype |
| `approval.routing` | ordered role/project/action/recipient/risk rules; safest eligible human-review queue | security-sensitive / `policy-admin` with independent two-person approval | M1 | Must resolve every mandatory-approval action to an authorized reviewer or block it |
| `ai.allowlist-version` | one product-approved immutable version; current v1 | security-sensitive / `policy-admin` with security approval | M1 | Tenant may pin or disable members, never add or alter metadata |
| `classifier.explanations-enabled` | boolean; `false` | standard / `policy-admin` | M1 | Explanation cannot alter classification or expose restricted evidence |
| `outbound.authority-enabled` | subset of fixed FR48 authority classes; empty | security-sensitive / `policy-admin` plus independent `mailbox-admin` approval with mailbox evidence | M1 | Membership/delegation revalidated at execution; mismatch blocks |
| `notification.routing` | versioned destinations by queue/severity; in-app operations queue | standard / `operations-admin` | M1 | Missing route retains in-app item and raises configuration degradation |
| `approval.priority-weights` | bounded positive weights for risk, affected authority, and age; product defaults | standard / `operations-admin` | M1 | Cannot demote mandatory-approval items below configured maximum age |
| `data.residency-region` | A6-approved product-supported region ID; no pre-approval default | security-sensitive / `compliance-admin` with independent two-person approval | M0 | Missing A6 region decision blocks persistence when residency applies; unsupported migration blocks affected writes |
| `data.retention-class` | duration within A6/NFR49a data-class bounds; no pre-approval default | security-sensitive / `compliance-admin` with independent two-person approval | M0 | Missing class decision blocks persistence; cannot shorten legal hold or exceed approved maximum |
| `ai-context.retention` | duration within the A6-approved AI-derived-data class; no pre-approval default | security-sensitive / `compliance-admin` with independent two-person approval | M0 | Missing class decision blocks AI invocation/persistence; cannot outlive its authorized source or legal hold |
| `dashboard.visibility-scopes` | closed set of aggregate/per-item scopes; aggregate-only | security-sensitive / `operations-admin` with independent two-person approval | M2 | Per-item evidence still requires Project authority |
| `replay.enabled` | boolean; `false` | security-sensitive / `operations-admin` with two-person approval | M2 | Requires replay-safe composition and egress denial or start is rejected |
| `proposal.replay-window` | duration `[0,24h]`; `5m` | standard / `policy-admin` | M2 | Applies only to non-mutating proposals; never to durable mutation identity |
| `operational.limits` | typed mailbox/AI/command/outbound quota and circuit-breaker map; product defaults | security-sensitive / `operations-admin` with independent two-person approval | M1 | Unknown class or negative/unbounded value rejected |
| `safety.controls` | command-managed versioned map whose keys are exactly `mailbox-source|service-client|ai-actor|command-capability`; value is `active|disabled|quarantined|rate-limited`, with a positive bounded rate/time window required only for `rate-limited`; empty/`active` | security-sensitive / `ApplySafetyControl` and `ReleaseSafetyControl` only, with initiator and independent approver fixed by the FR74 matrix | M1 | `UpdateTenantPolicy` cannot mutate this row; unknown subject/mode, missing bound, stale approval, or attempted authority broadening is rejected and the prior safer control remains active |

Every row carries a schema version and migration rule. The sensitivity/mutator cell is authoritative: every security-sensitive row requires the named initiating role, an independent second authorized admin, separation of duty, and a documented justification; when a row names two roles they must be different principals. Schema drift, unknown knobs, invalid dependencies, and unsafe combinations are tested at every increment gate. Service clients and AI actors cannot mutate policy. All successful and rejected policy changes produce canonical audit envelopes.

### Shared Command Pipeline (architectural invariant for FR81a)

Referenced from §Increment M1 and FR81a.

#### Universal invariant and admission profiles

- **Invariant:** every state-mutating operation, regardless of the originating surface (UI, CLI, MCP, service client, AI actor, background worker, or mailbox event), enters one command spine. Authentication, tenant binding, authorization, stable operation identity, expected-revision validation or an A13-approved owner concurrency guard, and canonical audit-envelope construction are universal. The central pipeline selects exactly one closed admission profile below; adapters and handlers cannot select, omit, or replicate stages.

| Admission profile | Operation/effect classes | Additional mandatory stages |
| --- | --- | --- |
| `mailbox-intake-v1` | mailbox capture and intake retry | provider identity/deduplication, authenticity state contract, A6 retention, attachment quarantine |
| `workflow-v1` | association, participant, attachment, task-intent, correction | family state guard, evidence freshness, actor/Project authority; correction additionally freezes and completes the owner-bound impact manifest |
| `governed-chat-v1` | governed-chat submit/stop/cancel/retry attempt mutations | actor/Project/conversation authority, origin/instruction boundary, attempt identity/revision, stream-effect and race guard; it commits only the chat attempt state |
| `ai-read-v1` | eligible low-risk assistance | origin/instruction-boundary validation, AI risk classification, current policy/context-package scope; no proposal approval stage |
| `ai-effect-v1` | AI-mediated Project/file/task/tool/outbound/on-behalf effect | origin/instruction-boundary validation, determinate risk classification, human-only non-self approval, approval lifetime/drift validation, allowlist and effect-target guard |
| `human-effect-v1` | human-origin outbound draft/send and send-outcome reconciliation | frozen content/recipient digest, full sender-authority tuple, human approval lifetime where effectful, provider evidence, no-blind-retry guard |
| `command-execution-v1` | general non-AI command retry/execution-state mutation | exposed-command/effect contract, caller authority, prior-attempt state, target idempotency/concurrency, uncertain-effect denial |
| `governance-v1` | mailbox configuration, policy, allowlist, notification routing, service-client, admin-role, safety-control, queue control | closed role/scope grammar, required separation of duty where declared, independent approval, safer-state-on-failure |
| `data-rights-v1` | export, erasure, legal hold, retention | validated request authority, independent TenantOwner/compliance approval, A6 policy, hold precedence, owner-context command/acknowledgement |
| `projection-delivery-v1` | audit projection and notification | source watermark or recipient/redaction/routing guard; never AI risk classification or AI proposal approval |

#### Command-to-profile map

The following map is exhaustive for the stable mutator catalog in PRD §Command and Query Contracts. Every command appears in exactly one row:

| Profile | Stable commands |
| --- | --- |
| `mailbox-intake-v1` | `CaptureMailboxEvent`, `ResolveInboundAuthenticityReview`, `ReprocessInboundAuthenticity`, `RetryMailboxIntake` |
| `workflow-v1` | `ProposeEmailProjectAssociation`, `AssociateEmailToProject`, `ConfirmEmailProjectAssociation`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `MarkEmailAssociationNeedsReview`, `SkipEmailAssociation`, `CorrectEmailProjectAssociation`, `ReprocessEmailAssociation`, `ResumeEmailAssociationReview`, `MarkEmailAssociationFailed`, `AcknowledgeAssociationCorrectionStore`, `MarkAssociationCorrectionDelayed`, `LinkOrResolveEmailParticipant`, `MarkParticipantResolutionDisposition`, `ResumeParticipantResolutionReview`, `CaptureEmailAttachment`, `StoreEmailAttachmentInProjectFolder`, `MarkEmailAttachmentOutcome`, `RetryAttachmentCapture`, `CaptureTaskIntent`, `MarkTaskIntentDisposition`, `AddWorkflowResolutionAnnotation`, `SupersedeWorkflowDecision` |
| `governed-chat-v1` | `SubmitGovernedChatMessage`, `CancelGovernedChatRequest`, `StopGovernedChatResponse`, `RetryGovernedChatMessage` |
| `ai-read-v1` | `ExecuteLowRiskAssistance` |
| `ai-effect-v1` | `ProposeAIAction`, `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, `CancelAIAction`, `ExecuteApprovedProjectCommand`, `RetryApprovedAIActionExecution` |
| `human-effect-v1` | `CreateOutboundProjectEmailDraft`, `SendApprovedProjectEmail`, `ReconcileOutboundSendOutcome` |
| `command-execution-v1` | `RetryCommandExecution` |
| `governance-v1` | `GrantServiceClientPermission`, `RevokeServiceClientPermission`, `ConfigureMailboxSource`, `PauseMailboxSource`, `ResumeMailboxSource`, `DisableMailboxSource`, `UpdateTenantPolicy`, `UpdateCommandAllowlist`, `ConfigureNotificationRouting`, `UpdateOperationalLimits`, `ApplySafetyControl`, `ReleaseSafetyControl`, `GrantChatBotAdminRole`, `ChangeChatBotAdminRole`, `RevokeChatBotAdminRole`, `PauseQueuePartition`, `ResumeQueuePartition`, `ClaimQueueItem`, `AssignQueueItem` |
| `data-rights-v1` | `InitiateDataExport`, `RetryDataExport`, `InitiateDataErasure`, `RetryDataErasure`, `PlaceLegalHold`, `ReleaseLegalHold`, `ExecuteRetentionDisposition`, `RetryRetentionDisposition` |
| `projection-delivery-v1` | `RebuildAuditProjection`, `DispatchWorkflowNotification`, `RetryWorkflowNotification` |

#### Transition and atomicity rules

Multi-stage entry commands never let an adapter choose or switch profiles. `SubmitGovernedChatMessage` commits only a governed-chat attempt; a determinate low-risk successor is a distinct `ExecuteLowRiskAssistance` command, and an approval-required successor is a distinct `ProposeAIAction` command. Outbound commands always use `human-effect-v1`; AI-produced content reaches them only through a previously committed `ai-effect-v1` proposal/approval chain. The central pipeline derives the next command and its profile from committed state and the catalog mapping. Before mutation, it denies unknown operation or effect classes, unmapped commands, duplicate mappings, and transitions that skip the distinct successor command. Adding or changing a profile or catalog command is a versioned PRD/addendum decision with a coverage test; universal stages cannot be disabled.
- **Atomic durability boundary:** the domain event, durable idempotency record, applied policy and approval references, and canonical audit envelope must become durable together—either in the EventStore stream or through a transactional outbox committed in the same transaction. If any element cannot be made durable, no element commits and the caller receives a typed failure.

#### Audit topology and projections

- **Audit ledger topology and concurrency:** canonical mutation envelopes are hash-linked within the same aggregate command stream. The supported actor-dispatched write path serializes turns and assigns the predecessor and sequence values. The envelope and its predecessor hash commit in the same aggregate transaction as the domain event. A separate signed per-tenant checkpoint periodically anchors the set of aggregate-stream heads; checkpoint lag is observable but never repairs a missing mutation envelope. Raw/direct actor-state writes are prohibited. Before M0, A13 requires deployment evidence that storage ACLs admit only the supported write path and that the selected provider's ETag/first-write behavior detects competing writers. Concurrent-write, duplicate-predecessor, fork, reordering, checkpoint-rebuild, and recovery tests must fail closed or raise P1.
- **After commit:** event publication, investigation/audit projection, UI projections, and notifications consume the committed record. These views are rebuildable and governed by projection-lag SLOs; a delayed projection never turns a committed command into an unaudited mutation.

#### Surface construction and parity

- **Construction:** surface adapters (UI controller, CLI command, MCP tool, service-client SDK, AI-actor mediator) translate surface-specific input into a typed Command record and hand it to the pipeline. Adapters MUST NOT replicate any pipeline stage; in particular, adapters cannot authorize, classify risk, or write audit records.
- **Parity follows by construction:** because every surface hits the same pipeline, parity is a property of the architecture, not a property of the test suite. The contract tests in FR82–FR86 verify the invariant (each surface's adapter, when handed an equivalent input, produces the same Command record); they do not enforce it.
- **Parity violation = invariant violation.** Any adapter that bypasses a pipeline stage is a defect, not a feature gap. The architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale.

### Idempotency Keys (per operation class)

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
| Governed chat attempt | Stable `chat_request_id` plus immutable `attempt_operation_id` and predecessor attempt | Same request, attempt payload, source state, and expected conversation revision | Reusing an attempt returns its prior outcome; retry from `stopped`/`failed` creates one new linked attempt; stop/completion uses first expected-revision commit |

Durable identities are retained for the lifetime of the governed record; time windows never permit a durable mutation or human decision to execute twice. Canonical request hashes remain audit evidence and may suppress duplicate non-mutating proposals. Expected aggregate/workflow revision controls concurrency independently from idempotency.

### Replay Isolation

Referenced from FR95a and §Increment M2.

- **Architectural enforcement:** replay/simulation uses a separate test tenant and a replay-only composition root. Production credentials and resource locators are unavailable at construction time. Mail, AI/model/tool, command, file/state, queue, and outbound adapters are replaced with replay-safe implementations; default-deny egress prevents undeclared external contact.
- **Audit distinguishability:** replay events carry a `replay_run_id` field in their audit envelope; production audit queries default to excluding replay events. Audit-completeness measurement (NFR50a) excludes replay events from numerator and denominator.
- **Verification:** each replay records before/after fingerprints for every relevant production store and external-resource ledger, then proves invariance. Tests cover email, model/tool calls, domain commands, files/state, queues, audit targets, and outbound traces. Credential-composition, egress, or invariance failure is a stop-ship M2 defect.

### ID Evolution Contract

Referenced from §Data Governance Surface and §Increment M1 / M2 (handles the case where a sibling bounded context renames, splits, merges, or deprecates an identifier ChatBot has audit records against).

`IdentityEvolved` is a proposed external dependency, not a binding contract. Each producing context must accept a versioned schema covering old and successor IDs, evolution kind, authority, reason, effective time, ordering, replay/idempotency, compatibility rollout, and authorization. It must also expose a reconciliation query for missed events and document consumer fallback.

Until every required producer accepts that contract, ChatBot preserves original identifiers in audit, rejects operations whose current identity cannot be resolved, and routes reconciliation to authorized review. If accepted, ChatBot may project immutable migration links without rewriting historical audit records. Producer acceptance, versions, and rollout evidence are recorded in `source-manifest.md` and `.memlog.md` before binding status is restored.

## Messaging authority

### Inbound Message Authenticity

Referenced from FR48a–FR48d and §Increment M0. Provider-specific tuning and broader compatibility may extend in M1.

- **DMARC / DKIM / SPF:** the M365 / Exchange adapter passes through the provider's verdicts for every inbound message. ChatBot records them in the intake audit and applies `mailbox.authenticity-strictness`: `strict` routes unresolved/anomalous identity to `AuthenticityReviewRequired`; `paranoid` routes it to terminal `AuthenticityBlocked`. MVP has no permissive mode.
- **Header inspection:** the adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` and records discrepancies. They feed intake reason codes and authorized association review; they are not inputs to the separate AI action-risk classifier.
- **On-behalf-of disambiguation:** when the M365 permission model expresses delegated send (send-on-behalf), the sender authority recorded in the audit is the on-behalf-of identity (the delegate), and the principal identity (the mailbox owner) is recorded as `principal_for`. Sender-authority mapping for outbound actions follows the same rule.
- **External-sender posture:** messages from external senders (no tenant party match) carry an `external_sender = true` flag through the pipeline. The risk classifier and the approval policy both reference this flag.
- **Canonical intake outcomes:** `CaptureMailboxEvent` produces exactly `AuthenticityAccepted`, `AuthenticityReviewRequired`, or `AuthenticityBlocked`. A current `mailbox-admin` initiates `ResolveInboundAuthenticityReview`; an independent current `policy-admin` approves the exact evidence digest and decision before the authenticity review transitions to `AuthenticityAccepted` or terminal `AuthenticityRejected`. Only accepted intake starts association `Received`; no authenticity state is an alias for association `NeedsReview`, `Failed`, `Skipped`, or participant `Quarantined`.
- **Retention, query, and reprocessing:** the M0 inbound-authenticity record is a ChatBot-owned durable record under the PRD Data Governance Surface. Evidence is tenant/mailbox scoped, redacted outside `GetInboundAuthenticityStatus`, and retained under the A6 source-message/intake class. `AuthenticityBlocked` and `AuthenticityRejected` never reopen. A current `mailbox-admin` may initiate `ReprocessInboundAuthenticity` only with independent current `policy-admin` approval, changed policy/provider evidence, the terminal expected revision, and a stable operation ID. A successful reprocessing command creates one audit-linked successor, and replay returns that same successor. Neither command nor query reveals candidate Project data before acceptance.

### Authority class mapping (FR48 five-class taxonomy)

FR48 declares five outbound sender-authority classes. Each maps to a specific M365 / Exchange posture and ChatBot authorization. Every send evaluates the full provider-neutral evidence tuple: token subject/client, delegated-versus-application OAuth mode and permission, target mailbox, mailbox ACL/delegation grant, current shared-mailbox membership where applicable, asserted sender identity, send-as versus send-on-behalf semantics, tenant, ChatBot Project/outbound scope, evidence timestamp, and execution-time revalidation result. The mapping rule is fixed; `outbound.authority-enabled` can enable an eligible class but cannot redefine it. Every required tuple element must be present and current; missing, stale, or mismatched evidence is a typed denial.

| FR48 authority class | M365 / Exchange permission posture | ChatBot-side authorization required | Audit fields |
|---|---|---|---|
| `draft-only` | none (action does not leave ChatBot) | requester has project authority + outbound-draft scope | requester, project, draft_id |
| `authenticated-user send` | delegated token for the requesting user with `Mail.Send`, target is the user's own mailbox, and no delegation is asserted | requester equals token subject/mailbox owner and holds current Project/outbound scope | full tuple, requester, mailbox, recipients |
| `shared-mailbox send` | delegated token permission **and** current membership **and** the mailbox's required send-as grant are all present; membership alone is insufficient | requester holds current Project/outbound scope; shared-mailbox and send-as semantics are explicit | full tuple, requester, shared_mailbox, membership/grant evidence, recipients |
| `send-on-behalf` | delegated token permission **and** current mailbox send-on-behalf delegation are both present; API scope alone is insufficient | requester and `principal_for` are recorded; ChatBot enables the class and requester holds current Project/outbound scope | full tuple, requester, principal_for, recipients, delegation evidence |
| `approved service-send` | application token/client permission plus target-mailbox application-access restriction and current service mailbox grant | exact service client has explicit outbound scope; originating requester and fresh human approval are in the audit chain | full tuple, service_client, originating_requester, approval_id, recipients |

**Conflict resolution rules:**
- If M365 grants `send-on-behalf` but `outbound.authority-enabled` excludes it, the action fails closed with `policy-blocked` (per FR77 catalog).
- If M365 grants `send-on-behalf` to delegate A, but the proposed action's requester is delegate B, the action fails closed with `delegation-mismatch`.
- If a `shared-mailbox send` is attempted by a member whose membership lapsed between policy snapshot and command execution, the action fails closed with `membership-revoked`; the membership-at-send is recorded for audit.
- If `approved service-send` is attempted without a paired approval record in the audit chain, the action fails closed with `approval-missing`. There is no permitted code path where a service client sends outbound without a prior approval record.

### Outbound send reconciliation

An outcome is `SendOutcomeUnknown` if the provider accepts a request without returning authoritative send evidence or if the connection fails after the send might have occurred. Blind retry is forbidden. `ReconcileOutboundSendOutcome` freezes and evaluates provider request/message IDs, mailbox and tenant, frozen content/recipient digest, sender-authority tuple, submission and observation timestamps, provider delivery trace/status, and queried mailbox evidence. An authorized mailbox worker or `mailbox-admin` moves `SendOutcomeUnknown -> Reconciling -> Sent | NotSent | Unresolved`; every observation is append-only. The default deadline is four hours, after which absent or contradictory evidence yields terminal `Unresolved` and P2 escalation. Only audited `NotSent` permits a new draft and fresh human approval; neither `Unresolved` nor reconciliation changes the original send operation.

## Operations and qualification

### Retry Profiles

Referenced from FR65 and NFR18. These `v1` safe defaults are required before the named workflow first ships. “Automatic retries” excludes the initial attempt. Backoff is exponential with full jitter uniformly selected from zero to the stated cap. A tenant/deployment profile may reduce automatic retries or make a reason terminal, but it cannot make a terminal reason retryable or raise a maximum without a new version approved by the System Architect and Test Architect. The applicable profile version is recorded in every retry audit envelope.

| Workflow family | Retryable reasons | Always terminal / no blind retry | Max automatic retries and backoff | Exhaustion / dead letter | Manual recovery command |
| --- | --- | --- | --- | --- | --- |
| Mailbox intake | provider timeout/throttle, transient subscription or dependency unavailability before an authenticity decision | unauthorized tenant/mailbox, `AuthenticityBlocked`, `AuthenticityRejected`, malformed provider identity, duplicate terminal result | `8`; `5s` doubling to `15m` cap | `Failed`, dead-letter by tenant/mailbox/source identity, owner `operations-admin` | `RetryMailboxIntake` for retryable pre-decision failure; `ReprocessInboundAuthenticity` requires current `mailbox-admin` initiation, independent current `policy-admin` approval, changed evidence, expected revision, and stable operation ID and creates one successor |
| Attachment capture/scan/store | scanner timeout, transient Folders or storage failure | unsafe verdict, unauthorized Project/file target, invalid type/size, content-identity conflict | `5`; `10s` doubling to `10m` cap | `Failed`, quarantined bytes remain inaccessible, owner `operations-admin` | `RetryAttachmentCapture` after scanner/authority revalidation |
| Association evaluation | scorer/dependency timeout before a decision commits | unauthorized target, deterministic conflict requiring review, malformed evidence, any committed decision | `3`; `2s` doubling to `2m` cap | `Failed` or `NeedsReview` by typed reason, owner `ProjectAdmin` | `ReprocessEmailAssociation` only from a named terminal state; `ResumeEmailAssociationReview` for deferred review |
| Participant resolution | transient Parties/Tenants lookup failure | rejected/quarantined identity, unauthorized or malformed Party evidence | `3`; `2s` doubling to `2m` cap | `Unresolved` or `Quarantined`, owner `ProjectAdmin` | `ResumeParticipantResolutionReview` after fresh identity evidence |
| Task-intent and risk classification | transient qualified-artifact/provider load failure before any classifier result or state persists | invalid/unqualified artifact, `classifier-indeterminate`, unauthorized context, committed disposition | `3`; `2s` doubling to `2m` cap | Task intent may enter its named review state; risk classification returns `classifier-indeterminate` with no proposal/idempotency state, owner `ProjectAdmin` | Reinvoke `CaptureTaskIntent` where eligible; after classifier remediation create a new linked AI-action request that must classify determinately |
| Approval, policy, admin-role, service-client, safety-control, and queue decisions | transport uncertainty only, by replaying the same `operation_id` | authorization/policy failure, stale revision, expired or drift-invalidated approval, decision conflict, invalid scope, any committed decision | `0` automatic | Source state unchanged or approval becomes `Expired`; typed terminal response, owner is the initiating authorized role | Re-submit the same command/operation ID to retrieve an uncertain outcome; expired/drifted approval requires a new linked proposal, while other successor/change commands are those named in the workflow tables |
| Governed chat and low-risk assistance | provider timeout/throttle before any streamed/output effect | denied/unsupported/approval-required result, policy/authority failure, any completed or partially emitted effect | `2`; `2s` then `5s` | `Failed`, owner/requester sees typed reason | `RetryGovernedChatMessage` creates one linked attempt; low-risk assistance requires a new linked request |
| Approved AI action and command execution | transient dependency failure proven to occur before any target effect | committed/rejected effect, stale approval/revision, authorization failure, non-idempotent or uncertain external effect | `3`; `2s` doubling to `2m` cap | `Failed`; uncertain effect enters investigation and is not retried, owner `operations-admin` | `RetryApprovedAIActionExecution` or `RetryCommandExecution` only for typed pre-effect failure |
| Outbound send | pre-admission provider timeout with authoritative no-send evidence | sent result, authority/policy/approval failure, changed content/recipient, `SendOutcomeUnknown`, `Reconciling`, or `Unresolved` | `2`; `5s` then `30s` | `Failed` or `SendOutcomeUnknown`; unknown enters reconciliation, never retry, owner `mailbox-admin` | `ReconcileOutboundSendOutcome`; create a new approved draft only after audited `NotSent` |
| Audit projection | transient read/store/subscription failure after canonical commit | canonical source/hash verification failure (P1), unauthorized rebuild scope | `20`; `5s` doubling to `5m` cap | `Lagging` then `Failed`, dead-letter by projection/partition/watermark, owner `operations-admin` | `RebuildAuditProjection` after source-watermark validation |
| Notification delivery | transient destination timeout/throttle | unauthorized recipient, redaction failure, invalid destination, already sent | `6`; `30s` doubling to `1h` cap | `Failed`, dead-letter by route/recipient/workflow, owner `operations-admin` | `RetryWorkflowNotification` after route/recipient revalidation |
| Data export, erasure, and retention disposition | transient protected-store/backup/export dependency failure; declared partial completion | rejected request, unauthorized scope, completed/disposed result; active legal hold is non-retryable until released | `5`; `1m` doubling to `6h` cap | `Failed`, `PartiallyCompleted`, or `BlockedByHold`; owner `compliance-admin` | `RetryDataExport`, `RetryDataErasure`, or `RetryRetentionDisposition` with fresh A6/hold evidence |

Every exhausted item exposes the typed terminal reason, attempts, next safe action, owner, and predecessor/successor IDs. Retry state and the canonical audit envelope commit atomically; dead-letter routing is a projection of that committed state and cannot authorize another attempt.

### Operating Baselines

Referenced from NFR42a.

This section becomes publishable at M2 only after A11-M2 supplies every required target, error budget, live signal, route, and burn test. Until then it is the M2 qualification backlog, not a published SLO catalog.

> **Authority:** planning values in this approved addendum are authoritative product targets. The code catalog must implement them, and a drift test must prove conformance. `unsupported-pending-a11` blocks the corresponding M2 production-readiness claim.

Per SLO entry, the recorded fields are:

- **Metric name** (stable identifier).
- **Target** (numeric, with units).
- **Measurement window** (e.g., rolling 7 days, p95 over 24h).
- **Error budget** (the fraction of the window the SLO may be missed before incident).
- **Alert threshold** (the budget consumption that fires an alert).
- **Calibration source** (A11-M2 baseline run that derived the target, with timestamp).
- **Tenant scope** (per-tenant override or platform-wide default).
- **Live signal and provenance** (metric/query name, observable-system locator, collection version, and access-controlled evidence source).
- **Alert route and accountable receiver** (destination plus the on-call or owning role that acknowledges it).
- **Burn-test evidence** (result, date, exact candidate revision, and immutable run/report locator).
- **Gate state** (`supported` only when every required field is present and its evidence matches the exact M2 candidate; otherwise `unsupported`).

Each compact normative target row below pairs one-to-one, by `Metric name`, with a candidate-bound evidence row in `qualification-evidence.md`. Each pair forms a complete A11-M2 qualification record; the evidence row remains mutable evidence, not a competing product requirement. Filling this target table alone cannot close A11-M2.

The SLO catalog covers, at minimum: ingestion latency, candidate generation latency, ambiguous-resolution time, command latency per command class, audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval queue p95 age per risk class, AI mediation latency, correction propagation latency (per NFR17a), and the FR81a shared-pipeline overhead per surface.

**M2 SLO qualification backlog (starter values and required dimensions):**

| Metric name | Target | Measurement window | Error budget | Alert threshold | Calibration source | Tenant scope |
| --- | --- | --- | --- | --- | --- | --- |
| `chatbot.command.execution.latency{command_class}` | `p95-le-2000ms` for each catalog command class | `rolling-24h` per class | `unsupported-pending-a11` | `budget-burn` | `nfr24` | `platform-default` |
| `chatbot.shared_pipeline.overhead.latency{surface}` | `unsupported-pending-a11` in milliseconds for each of `ui|cli|mcp|service|ai|worker|mailbox` | `rolling-24h` per surface | `unsupported-pending-a11` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.candidate.generation.latency` | `p95-le-10000ms` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `nfr25` | `platform-default` |
| `chatbot.operation.identity.latency` | `p95-le-5000ms` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `nfr26` | `platform-default` |
| `chatbot.correction.propagation.latency{increment=m0|m1}` | `p95-le-10m` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `nfr17a` | `platform-default` |
| `chatbot.correction.propagation.latency{increment=m2}` | `p95-le-60m` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `nfr17a` | `platform-default` |
| `chatbot.audit.projection.lag` | `p95-le-5m` | `rolling-24h` | `degraded-100ev-failed-1000ev` | `lag-gt-5m` | `nfr43` | `platform-default` |
| `chatbot.retry.exhausted.rate{operation_class}` | `unsupported-pending-a11` exhausted attempts per `1,000` attempts | `rolling-24h` per class | `unsupported-pending-a11` violating attempts per window | `budget-burn` and any single safety-critical exhaustion | `nfr43` | `platform-default` |
| `chatbot.approval.queue.age{risk_class}` | `p95-le-2-business-days` per risk class | `rolling-7d` | `unsupported-pending-a11` overdue items per window | `age-gt-2-business-days` | `nfr43` | `platform-default` |
| `chatbot.mailbox.subscription.renewal` | `100%` renewed before `expiry-le-7d` | `rolling-30d` | `0` subscriptions entering the 7-day window without alert | any violating subscription | `nfr43` | `platform-default` |
| `chatbot.ingestion.latency` | `unsupported-pending-a11` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.ambiguous.resolution.time` | `unsupported-pending-a11` | `rolling-7d` | `unsupported-pending-a11` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.duplicate.suppression.rate{operation_class}` | `unsupported-pending-a11` duplicates per `1,000` attempts | `rolling-24h` per class | `unsupported-pending-a11` variance from baseline | `spike-baseline` | `a11-pending` | `platform-default` |
| `chatbot.mailbox.failure.rate` | `unsupported-pending-a11` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `a11-pending` | `platform-default` |
| `chatbot.ai.mediation.latency` | `unsupported-pending-a11` | `rolling-24h` | `unsupported-pending-a11` | `budget-burn` | `a11-pending` | `platform-default` |

### Recovery Qualification

#### Completion evidence boundary

Current-run diagnostics, current-run story-completion authority, and retained scheduled/release operational evidence serve three separate trust purposes. The metadata-only `recovery-primary-diagnostics` channel has no completion or A10 authority. Only independently validated current-run evidence bound to the exact candidate revision and approved evidence policy may satisfy the story-completion recovery gate. Only an independently governed, fresh operational bundle may inform A10. None substitutes for another.

The completion gate accepts exactly one current-run recovery producer and fails on planning, production, timeout/no-test, restoration, cleanup-receipt finalization, projection, attestation, independent validation, or publication failure. Its sole completion-authority channel contains only policy-allowed validated reports, the aggregate cleanup receipt, canonical sanitized result, and provenance sidecars; raw producer output remains outside every upload. The accepted Epic 12 contract remains `activation: pending`. Independent verification of its unique pull-request check identity, cleanup receipt, fixed closeout deadlines, immutable tool references, and fresh-runner destructive-test isolation is required before its authority can be claimed. Operational recovery bundles follow the separate A10 retention and freshness policy. Exact locators, revisions, results, activation state, and expiry calculations live in `qualification-evidence.md` and the source manifest.

#### Recovery-validation commitments

This subsection is **not** part of the SLO catalog above. It records current product targets, owners, evidence kinds, freshness rules, and gate state. Implementations must conform; revision-specific evidence lives in `qualification-evidence.md`.

A10 remains provisional, and the pending Epic 12 completion architecture does not close this operational gate. Current bundle validity, expiry, controlled-loss, and RTO evidence are retained in `qualification-evidence.md`.

Fresh qualification requires a hosted controlled-loss path that derives RPO from persisted EventStore bounds and an RTO-capable full-window lane or separately retained production-shaped drill. Local implementation evidence cannot ratify either target.

| Requirement | Commitment / target | Story 12.15 status | Decision scope |
| --- | --- | --- | --- |
| A10 / NFR56 | RPO ≤ 15 minutes; RTO ≤ 4 hours | No current qualifying hosted bundle | Provisional; fresh controlled-loss and RTO-capable evidence are stop-ship M2 gates. |
| NFR57 | Projection rebuild ≤ 4 hours | No current production-shaped full-window proof | Provisional; requires fresh exact-candidate evidence. |
| NFR41 | Dependency/scope recording ≤ 5 minutes | No current product-monitoring proof for every dependency | Not established; missing evidence is `unmeasurable`, never inferred from sandbox expectations. |

The per-tenant dashboard exposes `within-budget`, `approaching`, `exhausted`, or `unsupported`. The dashboard must report `unsupported` when a numeric target, live signal, route, or burn test is absent. This status blocks the associated M2 production-readiness claim.
