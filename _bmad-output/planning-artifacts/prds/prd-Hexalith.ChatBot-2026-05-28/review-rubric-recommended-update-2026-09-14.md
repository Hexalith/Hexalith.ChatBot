# PRD Quality Review — Hexalith.ChatBot Recommended Update (2026-09-14)

## Overall verdict

**Needs revision before re-finalization — no Critical findings, but four High contract gaps remain.** The recommended update successfully closes the headline contradictions around AI/service self-approval, the sole exit from `Deferred`, correction completion semantics, the A11-M1/A11-M2 split, approval freshness, outbound uncertainty, sender authority, and authenticity-state separation. The product thesis, scope, and fail-closed release posture remain strong; the residual risk is downstream implementability, because several new normative mechanisms still lack a complete profile, query/actor contract, governed-record declaration, or release qualification edge.

## Decision-readiness — adequate

The PRD is unusually candid about what is decided and what is not. §Current Release Status names the open A5, A6, A10, A11, and A13 gates and limits claims while they remain open; §Minimum Release Slice ties each increment to evidence, approvers, rollback conditions, and permitted claims. The A11 split now gives M1 and M2 separate, executable decision boundaries rather than treating all metric work as an M2 concern.

One material qualification gap remains: two normative model-quality bars used by the M0/M1 product loop are not included in an increment gate. The artifact also uses two names for the positive gate-record state, which is fixable but should be resolved before software consumes the schema.

### Findings

- **high** Detector and classifier quality thresholds are not release-gated (§Measurable Outcomes; §Minimum Release Slice — Three Increments; FR35; A9a; `addendum.md` §Risk Classifier) — FR35 defines TaskIntentDetector precision/recall targets of 80%/75% for M0 and 90%/85% for M1, while the Risk Classifier appendix defines ≤1% evaluation misclassification and ≤2% production-sampled disagreement. A9a provides partitions and corpus sizes, but the M0/M1 gate rows do not require a passing detector/classifier qualification record or name these thresholds among their pass/fail evidence. As written, a candidate could satisfy the enumerated gate evidence while failing a product-critical detector threshold. The classifier's “production-sampled reviewer disagreements” denominator is also not defined for low-risk actions that execute without a proposal review. *Fix:* add versioned TaskIntentDetector and ActionRiskClassifier qualification records, exact sample/denominator rules, and their thresholds to the first increment that uses each artifact; identify how low-risk production samples are adjudicated, and make failure invalidate the relevant gate record.
- **medium** Gate status vocabulary drifts at the release boundary (§Current Release Status, paragraph after the increment bullets; `addendum.md` §Increment Gate Record Contract) — the PRD says “`Current`, `expired`, and `invalidated` are computed,” while the normative schema says the exact positive state is `approved-current` and also defines `open` and `superseded`. This is especially risky because the text says release logic is machine-evaluable. *Fix:* use the exact five-value enum everywhere and state explicitly how a `decision=rejected` record computes (presumably `open`, unless a separate rejected status is intended).

## Substance over theater — strong

The document's detail is earned by the product's trust boundaries. Personas are embedded in concrete journeys and directly drive authorization, evidence, queue, approval, correction, and audit decisions. The NFRs carry measurable bounds rather than generic claims, and the PRD explicitly refuses unsupported market, GDPR, tamper-evidence, and production-readiness claims.

No substantive theater finding was identified. The length is high, but most of it is executable contract material appropriate to a chain-top enterprise PRD.

## Strategic coherence — strong

The thesis is clear and stable: turn ordinary external project email into authorized, auditable Project work, with AI acting as a governed actor rather than a side-channel assistant. M0 proves the vertical UI loop, M1 proves governed cross-surface parity, and M2 proves production operability and recovery. The feature cuts, non-goals, success measures, and counter-metrics consistently support that arc.

The recommended edits strengthen rather than disturb this strategy. In particular, human-delegated MCP parity remains available while AI/tool principals are denied human decisions, and the A11-M1 split now makes pilot success evidence consistent with the M1 product claim.

## Done-ness clarity — adequate

Most risky workflows now have named states, commands, actors, guards, events, idempotency behavior, and typed failure outcomes. The approval, correction, outbound-send, retry, and data-rights contracts are substantially more implementable after the update.

Two of the new mechanisms are not yet closed enough for architecture and story creation: the “closed” admission-profile matrix does not cover every state-mutating command family, and the authenticity workflow lacks a per-item query and a named reprocessing authority.

### Findings

- **high** The closed command-pipeline profile matrix does not cover all catalog mutations (`addendum.md` §Shared Command Pipeline, especially the seven profile rows; PRD §Shared Workflow Contract; §Command and Query Contracts; FR81a) — the appendix says the central pipeline selects exactly one closed profile and unknown classes are denied, but no row clearly covers governed-chat attempt mutations (`SubmitGovernedChatMessage`, stop/cancel/retry), human-origin outbound draft/send, mailbox configuration/retry, general non-AI command execution, or workflow notification routing configuration. `ai-effect-v1` is explicitly limited to AI-mediated effects, while `workflow-v1` enumerates only association, participant, attachment, task-intent, and correction. Implementers must therefore either deny catalogued commands or invent profile membership/stages, defeating the closed-profile decision. *Fix:* publish a command-to-profile map covering every stable mutator, add any missing profiles (for example governed-chat and human effect/outbound), and define how multi-stage entry commands select or transition profiles without letting adapters choose.
- **high** The new authenticity workflow is not retrievable or reprocessable through a complete authority contract (PRD §Shared Workflow Contract authenticity paragraph; §UI Surface Inventory S2a; §Command and Query Contracts; `addendum.md` §Inbound Message Authenticity and §Retry Profiles) — `ResolveInboundAuthenticityReview` names `mailbox-admin`, but `ReprocessInboundAuthenticity` has no explicit initiating actor/authority rule. The query catalog has no per-item `GetInboundAuthenticityStatus`; `GetMailboxIngestionHealth` is an aggregate health query and cannot supply the state/evidence/owner/next-action contract promised by S2a, especially for blocked/rejected intake that never creates an association record. *Fix:* add a stable per-item authenticity status/evidence query, define its redaction and response fields, and assign the reprocess command's actor, co-approval (if any), current-evidence guard, expected revision, idempotency, and successor result across UI/API and later parity surfaces.
- **medium** Attachment state vocabulary conflicts with its authoritative workflow (FR34; §Shared Workflow Contract attachment-handling row) — FR34 calls `captured`, `pending`, `unavailable`, `rejected`, `unsafe`, `failed`, and `retryable` attachment “states,” while the workflow defines only `PendingScan`, `Stored`, `Unsafe`, and `Failed`; capture and retry are commands/events, and unavailable/rejected have no transition. *Fix:* make FR34 use the canonical state enum, or extend the authoritative transition table with the omitted states and exact terminal/retry semantics.

## Scope honesty — strong

The artifact is explicit that it is a reopened draft and that draft prose or file presence is not approval evidence. MVP exclusions, post-MVP triggers and channels, increment claims, open dependencies, and provisional recovery/SLO evidence are all named rather than silently deferred. Open-item density is appropriate for a chain-top PRD because each unresolved gate has an owner and revisit condition.

No scope-honesty finding was identified.

## Downstream usability — adequate

Stable FR/NFR identifiers, named protagonists, the authority map, lifecycle tables, operation catalog, traceability overview, and acceptance guidance make the document highly extractable. The recommended update preserves identifier continuity and resolves the most dangerous cross-surface and lifecycle ambiguity.

One newly load-bearing durable artifact is nevertheless absent from the supposedly complete Data Governance Surface, leaving architecture and compliance to invent its storage obligations.

### Findings

- **high** The correction impact manifest is absent from the first-class durable-record inventory (§User Journey 4; §Data Governance Surface; FR7; FR91a) — correction cannot start or complete without a frozen, versioned manifest carrying source/destination Projects, affected owner records, effect history, repair/compensation status, evidence, and acknowledgements. Yet §Data Governance Surface, introduced as the list of ChatBot-owned first-class durable records, has no manifest row. Generic `Lifecycle state` and `Workflow instance map` rows do not specify the manifest's high-sensitivity contents, A6 retention class, redaction behavior, partitioning, isolation proof, legal-hold/export/erasure treatment, or M0 ownership. *Fix:* add an M0 `Correction impact manifest` record row and bind it to A6, native-store isolation, immutable versioning, owner acknowledgements, retention/hold/export/erasure, and correction completion.

## Shape fit — strong

This is the right shape for a multi-stakeholder B2B orchestration product feeding UX, architecture, security, compliance, QA, and story creation. Named journeys make the human and machine workflows concrete; capability requirements remain in the PRD while executable mechanisms and schemas live in the normative addendum; mutable evidence remains outside both.

The PRD is dense, but not wrongly shaped. Its density follows from the product's multi-context authority and audit obligations rather than template inflation.

## Mechanical notes

- FR1–FR96 and NFR1–NFR70 are contiguous and uniquely defined; letter-suffixed requirements are also unique. The phrase `FR81a-compliant` in M0 scope is prose, not a duplicate definition.
- All eight human journeys have named protagonists. The separate System Journey intentionally describes a governed actor flow and is not a floating human journey.
- Every inline `[ASSUMPTION ...]` tag resolves to A9a or A11 in §Open Assumptions and Decisions. No live `[NOTE FOR PM]` callout or Open Questions section remains; unresolved release questions are represented as owned gate assumptions instead.
- **low** The Glossary does not define the central capitalized term `Project`, even though it distinguishes related terms such as `Candidate project`, `Context package`, and `Project`-scoped authority throughout. Add the owner/context meaning once to reduce capitalization and bounded-context ambiguity in extracted stories.
- Both normative artifacts are consistently marked `status: draft`; their historical final/approval metadata is explicitly labeled as historical.

## Finding totals

- Critical: 0
- High: 4
- Medium: 2
- Low: 1
