# Architecture Reconciliation — UX Impact (2026-09-14)

## Scope, authority, and status

This refresh compares the current `DESIGN.md` and `EXPERIENCE.md` with the finalized feature architecture. It records user-visible implementation constraints without allowing downstream architecture to replace product intent. The architecture itself says that the PRD and addendum own normative product requirements and that `[ADOPTED]`, compatibility, or interface presence does not prove executable support, qualification, or release readiness (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:62-76`).

Both UX spines remain `status: in-review` (`DESIGN.md:1-6`; `EXPERIENCE.md:1-5`). The current PRD/addendum remediation is still draft, the latest product validation still records STOP for the earlier snapshot, finalized architecture still reflects several pre-remediation contracts, and qualification gates remain open (`EXPERIENCE.md:51-60`; `../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:528-540`).

## Verdict

The current UX pair has incorporated the architecture's user-observable safety, authority, lifecycle, recovery, accessibility, and qualification consequences. No visual-token or shell change is required. Remaining work is reconciliation of explicit product-draft versus finalized-architecture differences—not restoration of the pre-update UX behavior. Disputed paths stay fail-closed and implementation-blocked until product is freshly validated/approved and architecture is revised and revalidated.

## Incorporated architecture-aligned behavior

| Architecture contract | Incorporated UX behavior | Precise sources |
|---|---|---|
| FrontComposer/Fluent UI is the single shell and component system; partial output and status distinctions must remain accessible. | `DESIGN.md` inherits FrontComposer and Fluent UI v5, prohibits a parallel control/theme system, and assigns exact component bases. `EXPERIENCE.md` supplies WCAG 2.2 AA behavior and per-surface acceptance. | `DESIGN.md:66-76`, `:98-104`, `:116-164`; `EXPERIENCE.md:306-340`; architecture AD-16, `ARCHITECTURE-SPINE.md:354-378`. |
| Every mutation uses one governed command/audit path; current owner authority and restricted-read auditing fail closed. | Cross-surface operations preserve authorization, redaction, idempotency, state, reason, audit, and origin. Omission applies before the rendered DOM and accessibility tree. Admin reads, denials, and restricted reads use the separate auditable-attempt path. | `EXPERIENCE.md:258-286`, `:300-304`; architecture AD-2/AD-3, `ARCHITECTURE-SPINE.md:86-126`. |
| Family states, lifetime identities, first-commit-wins decisions, retry ceilings, stored replay outcomes, and immutable successors remain distinct. | The UX uses exact family-state rows, separates availability reasons from durable state, returns prior outcomes on replay, rejects changed input, and creates linked attempts/successors only where allowed. | `EXPERIENCE.md:184-210`, `:288-290`; architecture AD-4, `ARCHITECTURE-SPINE.md:128-147`. |
| ChatBot coordinates; sovereign contexts own committed effects; delivery and projections cannot prove or retry an owner effect. | Shared status separates coordinator, effect owner, delivery/provider, and projection/investigation lanes. Correction, retry, and outbound recovery do not reinterpret a committed or unknown effect. | `DESIGN.md:149-153`, `:174`; `EXPERIENCE.md:216-235`, `:288-294`; architecture AD-5/AD-15, `ARCHITECTURE-SPINE.md:149-163`, `:338-352`. |
| SignalR progress is advisory; authoritative status is queried; streaming attempts are immutable and partial text is not committed content. | Governed chat keeps one immutable attempt, re-queries typed status, suppresses live announcement of chunks, reconciles by stable identity after disconnect, and creates a linked retry instead of resuming partial output. | `EXPERIENCE.md:233-244`; architecture AD-16, `ARCHITECTURE-SPINE.md:354-372`. |
| Bootstrap, service-client grants, admin authority, and runtime controls use exact scope, distinct approvers, current evidence, and fail-closed admission. | M0 bootstrap is automation-only; M1 exposes the editor. Service-client and role grants show scope, expiry, initiator, approver, version, and immutable status. Runtime-control and SLO views show freshness, qualification, owner, and safe recovery; no absent signal becomes “healthy.” | `DESIGN.md:144-152`; `EXPERIENCE.md:45-47`, `:246-254`, `:296-304`; architecture AD-6/AD-19, `ARCHITECTURE-SPINE.md:165-180`, `:403-416`. |
| Canonical audit, laggable investigation views, diagnostics, story evidence, and gate evidence are different authority channels. | Canonical effects and derived views are labeled separately; projection lag never undoes a commit. Qualification language names evidence and remains blocked rather than claiming GDPR, tamper evidence, recovery, or SLO readiness. | `EXPERIENCE.md:55-58`, `:216-225`, `:246-250`, `:300-304`; architecture AD-10 through AD-12, `ARCHITECTURE-SPINE.md:244-310`. |
| Unaccepted identity evolution preserves history and blocks mutation. | `unresolved-current-identity` retains the original identifier, rejects mutation, and routes to authorized reconciliation; only an accepted successor may be linked. | `EXPERIENCE.md:58`, `:280-286`; architecture AD-14, `ARCHITECTURE-SPINE.md:326-336`. |
| The six AI-mediated boundary-crossing effect classes never downgrade. | Both spines require permanent human approval for Project-state mutation, file exposure, external communication, task creation/assignment, external-tool invocation, and acting on behalf. Direct authorized human commands remain governed without being recast as AI proposals. | `DESIGN.md:170-174`; `EXPERIENCE.md:49`, `:168-180`; architecture AD-7, `ARCHITECTURE-SPINE.md:182-210`. |

## Remaining product-draft versus finalized-architecture divergences

### 1. Risk-classifier failure branch and canonical reason — direct conflict

The current product draft makes missing, invalid, unqualified, indeterminate, unknown-effect, and undeclared-authority classifier outcomes `classifier-indeterminate`; none creates a proposal or durable idempotency state, and none can be approved (`../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:49-59`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:1296-1313`). The finalized architecture instead returns `classifier-unavailable` for missing/invalid/unqualified/failed artifacts and treats valid-artifact missing tags, unknown effects, or undeclared authority as `approval-required` (`ARCHITECTURE-SPINE.md:186-197`).

The UX follows the candidate product name while taking the safe intersection: no proposal, no effect, no durable idempotency state, a separate auditable attempt, and a new linked request after remediation (`EXPERIENCE.md:164-176`). `classifier-unavailable` is only a non-canonical availability reason. Do not implement either disputed canonical branch until product and architecture agree; architecture's reviewable `approval-required` branch must not bypass the product draft's prohibition.

### 2. Inbound authenticity family, authority, and S2a — direct conflict

The product draft defines distinct `AuthenticityAccepted`, `AuthenticityReviewRequired`, `AuthenticityBlocked`, and `AuthenticityRejected` states; a current `mailbox-admin` initiates and an independent current `policy-admin` approves the frozen decision. Only accepted intake enters association, and terminal reprocessing creates a successor (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:486-488`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:239-248`). The finalized architecture still routes strict authenticity anomalies to association `NeedsReview`, does not define the two-person S2a decision, and its binding UI inventory omits S2a (`ARCHITECTURE-SPINE.md:212-223`, `:373-378`).

The UX keeps S2a separate, exposes no Project candidates before acceptance, requires the two-person decision, and permits successor-only reprocessing (`EXPERIENCE.md:62-100`, `:184-191`). This stricter product-draft path remains implementation-blocked pending architecture alignment.

### 3. Correction state spelling and completion manifest — direct conflict

The current product uses canonical `CorrectionDelayed` and requires the frozen complete impact manifest—including Conversations/Folders-owned records and irreversible effects—to receive an immutable acknowledgment or explicit disposition before `Corrected` (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:496-532`). The finalized architecture still emits `Correction-delayed` and enumerates a narrower acknowledgment set (`ARCHITECTURE-SPINE.md:225-242`).

The UX preserves `CorrectionDelayed`, treats `Correction-delayed` as non-canonical, blocks affected AI context, and withholds `Corrected` until every frozen manifest item closes (`EXPERIENCE.md:191`, `:227-231`). The broader completion rule is the conservative posture.

### 4. A11 milestone split — direct conflict

The product draft requires A11-M1 before the M1 governed-pilot exit and A11-M2, with A10, before M2 production/release-candidate status (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:100-108`, `:288-291`). The finalized architecture models A11 only as an M2 gate (`ARCHITECTURE-SPINE.md:283-310`, `:528-540`).

The UX surfaces A11-M1 and A11-M2 separately and never treats architecture finality or a visible dashboard as qualification (`EXPERIENCE.md:41-47`, `:246-250`). Enforce both product gates until architecture is reconciled. The Foundation availability table is a compact UX summary, not a replacement for the PRD's complete increment-gate table; A9a and every other product-owned gate still apply.

### 5. S2a and O1 inventory — architecture coverage gap

The product draft requires S2a and restricted O1 before persisted pilot data (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:612-625`), while architecture AD-16's “exact” M0 UI inventory lists only association review, AI approval, and project conversation (`ARCHITECTURE-SPINE.md:373-378`). AD-10 acknowledges A6/data-protection constraints but does not define O1 as a governed operator interface (`ARCHITECTURE-SPINE.md:244-260`).

The UX includes both surfaces. O1 is visibly qualification-blocked while A6 is open, is not a general application or UI/CLI/MCP-parity surface, and grants no direct owner-context mutation (`EXPERIENCE.md:55-58`, `:62-100`). Do not remove either surface to match the older inventory.

### 6. Remediated product details not yet explicit in finalized architecture — coverage gaps

The product draft now specifies the sole Resume exit from `Deferred`, bounded approval lifetimes and new-proposal-only recovery, four-hour outbound reconciliation with no blind resend, and candidate human-delegated MCP decisions with current user presence and `actorType=human` (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:215`, `:523-527`, `:543-551`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:73-80`, `:268-270`). The architecture delegates state transitions to normative family matrices but does not yet carry these remediated details consistently.

The UX preserves the stricter behavior: Resume before confirm/reject, no in-place approval renewal, no resend from unknown/reconciling/unresolved, and no machine approval. Human-delegated MCP approval remains candidate M1 behavior and explicitly implementation/qualification-blocked until product approval and architecture alignment (`EXPERIENCE.md:191`, `:278`, `:288-298`, `:438-459`).

## Conservative UX posture and handoff rule

- Keep `DESIGN.md` and `EXPERIENCE.md` `in-review`; do not claim clean downstream handoff, release readiness, GDPR satisfaction, tamper-evident completeness, recovery qualification, or SLO qualification.
- Preserve the product-draft branch where it is stricter, and otherwise use the intersection of product and architecture constraints. Never infer permission from an architecture omission.
- Block implementation of the six disputed/under-specified paths above until the current PRD/addendum are revalidated and approved and the finalized architecture is revised against that exact source revision.
- Keep A5, A6, A13, A10, A11-M1, A11-M2, A9a, and the complete PRD increment-gate evidence authoritative. File presence, compatible interfaces, or `[ADOPTED]` labels close no gate.
- Preserve existing fail-closed outcomes: no classifier-bypass approval, no authenticity-to-association leak, no direct decision from `Deferred`, no false `Corrected`, no blind outbound resend, no machine approval, and no unresolved-identity mutation.

## Architecture detail intentionally excluded from the UX spines

Repository topology, DomainService hosting, OpenAPI/NSwag generation, exact package/runtime versions, scheduling algorithms, key encodings, actor fencing, hash/checkpoint formats, and retry-backoff formulas remain architecture concerns. The UX reflects only their observable state, authority, evidence, failure, and recovery consequences (`ARCHITECTURE-SPINE.md:380-416`, `:418-526`).
