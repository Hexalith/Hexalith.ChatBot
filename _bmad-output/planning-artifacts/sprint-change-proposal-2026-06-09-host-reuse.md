---
title: Sprint Change Proposal - Host-Layer Reuse (DomainService SDK Adoption)
project: Chatbot
date: 2026-06-09
status: approved
mode: Batch
trigger: "Implementation readiness pass-2 (2026-06-09) found NEEDS WORK: the host-layer reuse decision was never made — ChatBot bypasses the Hexalith.EventStore.DomainService SDK with a hand-rolled 1221-line host + own AppHost/Aspire/ServiceDefaults — plus cross-increment dependency visibility edits and minor AC polish."
scope_classification: Moderate
recommended_approach: "Direct Adjustment (Hybrid - 1 new epic with 6 stories + Story 8.7 split + targeted artifact edits)"
owner: Jerome
prepared_at: 2026-06-09
approved_by: Jerome
approved_at: 2026-06-09
implementation_state: planning-artifacts-applied
source_report: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09-pass-2.md"
supersedes_open_items_from: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-readiness-blockers.md"
decisions:
  review_mode: Batch
  host_reuse_direction: "Full SDK adoption (option a) — ADR + platform pre-commit hook + staged host migration"
  scope: "All three readiness issues (#1 host-reuse, #2 cross-increment dependencies, #3 AC polish)"
  issue1_placement: "New Epic 11 (M2, release-readiness closure, before MVP sign-off)"
  issue2_87_split: "Story 8.7 split into 8.7a (projection) + 8.7b (periodic trigger) per fine-grained-story preference"
---

# Sprint Change Proposal - Host-Layer Reuse (DomainService SDK Adoption)

## 1. Issue Summary

The 2026-06-09 **pass-2** implementation readiness assessment (run with Jerome's explicit directive: *reuse existing submodule classes; minimal technical layer; no unneeded boilerplate*) returned **NEEDS WORK (narrowly scoped)**. Domain-logic reuse was verified strong — sibling Clients/Contracts consumed, adapter ports over siblings' own commands, CommandGateway a thin admission layer over EventStore's existing write path, FrontComposer Shell read-only, Memories for vectors. The gap is entirely in the **hosting/infrastructure layer**:

**Issue #1 (significant) — the host-reuse decision was never made, and the code drifted to the hand-rolled extreme.**
The platform ships a domain-centric SDK (`Hexalith.EventStore.DomainService`): per EventStore's `CLAUDE.md` "Domain-Module Authoring" rule, a domain module **must not** ship its own `AppHost`/`Aspire`/`ServiceDefaults`, hosts in **~2 lines** (`AddEventStoreDomainService()` / `UseEventStoreDomainService()`), implements queries as `IDomainQueryHandler`, projections as `IDomainProjectionHandler`, cursors via `IQueryCursorCodec`, and takes telemetry/health from SDK helpers; *"if a capability is missing, add it to the platform … not the domain."* Verified facts:

- `Hexalith.ChatBot.Server.csproj` references EventStore **`.Client` + `.Contracts` only** — not `.DomainService`.
- **Zero** SDK-contract usages (`IDomainQueryHandler`/`IDomainProjectionHandler`/`IReadModelStore`/`IQueryCursorCodec`/`AddEventStoreDomainTelemetry`) anywhere in ChatBot `src`.
- `src/Hexalith.ChatBot.Server/Program.cs` is **1221 lines** with ~15 inline `MapGet`/`MapPost` query endpoints; the module ships its own `AppHost` (94 lines) + `Aspire` + `ServiceDefaults`.
- The SDK **exists and is used today**: `AddEventStoreDomainService`/`UseEventStoreDomainService` are implemented, and the EventStore AppHost composes `tenants` and `sample` via `AddEventStoreDomainModule(...)` (`Hexalith.EventStore.AppHost/Program.cs:126,134`).
- The planning artifacts (`architecture.md`, `epics.md`, addendum) mention the SDK **0 times** — the decision was never made; ChatBot faithfully copied the sibling shape (Folders/Conversations/Tenants-repo layout) that is now the explicitly deprecated pre-SDK pattern.

**Fair nuance recorded:** the FR81a admission layer is a genuine reason the *plain* SDK host doesn't fit as-is — but the platform rule's prescribed response is to add the missing capability (a pre-commit admission hook) **to the SDK**, not to hand-roll a bypass host; and the query/projection/telemetry/health/cursor surfaces (the bulk of the 1221 lines) have no admission-layer reason to be hand-rolled.

**Issue #2 (major) — cross-increment forward dependencies live in coverage-map notes, not in the dependent stories.**
Stories 7.12–7.26 assert runtime outcomes ("intake … is blocked") but the control floor is wired-yet-inert until Story 8.7 (M2) materializes the durable control-state projection + periodic trigger. Stories 9.2/9.13 likewise assume 8.7's live runtime loop. A team reading these stories in isolation over-claims runtime enforcement. Story 8.7 itself is broad (projection + trigger + 4 consolidated feeds).

**Issue #3 (minor) — increment hygiene + AC polish.**
M1-tagged ACs leak into M0 stories (2.7 FR96 "(M1)", 4.9 "M0/M1" clause); 3.11 references FR35 (owned by Epic 4) without scoping; "As the system," role lines on 2.3/2.4/3.14/4.1/4.7 bury the beneficiary; the seven 3.2–3.8 render stories have templated ACs with thin per-item differentiation; Story 1.9 (the epic value proof) is under-tagged.

**Sprint context that shapes the fix:** Epics 1–7 and 9 are `done`, Epic 8 is `in-progress` (8.6, 8.7 backlog), Epic 10 is `backlog`. The hand-rolled host is shipped, working, tested code — so Issue #1 cannot be fixed by "updating Story 1.1 acceptance" (a done parent container). Following the CR-1/CR-2 precedent from the readiness-blockers proposal, the unmade decision must be converted into **assignable, gated work**.

## 2. Checklist Results (Change Navigation)

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the pass-2 readiness assessment, not a failed story. Impacted artifacts: `epics.md`, `architecture.md`, `sprint-status.yaml`, `index.md`, `docs/adrs/`, and (for Story 11.2) the `Hexalith.EventStore` submodule. |
| 1.2 Core problem | [x] Done | Issue type: *missed architectural decision + code drift* — the platform's domain-hosting SDK was never evaluated in planning, and the module hand-rolled the host layer the SDK provides. Secondary: dependency-visibility and AC-hygiene defects in `epics.md`. |
| 1.3 Evidence | [x] Done | Pass-2 report Steps 3/5/6; verified greps (csproj refs, 0 SDK usages, 1221-line `Program.cs`, `AddEventStoreDomainModule` composing `tenants`/`sample`); EventStore `CLAUDE.md` Domain-Module Authoring rules. |
| 2.1 Current epic completable? | [x] Done | Epic 8 (`in-progress`) absorbs the 8.7 split without scope change. Epic 1 is `done` and is **not** reopened — the host decision gets a new owning epic instead. |
| 2.2 Epic-level changes | [x] Done | **Add Epic 11** (M2 release-readiness closure: DomainService SDK host adoption, 6 stories). Modify Epic 8 (split 8.7 → 8.7a/8.7b). Annotate Epic 7 (runtime-activation riders on 7.12–7.26). |
| 2.3 Remaining epics impacted | [x] Done | Epic 9: riders on 9.2/9.13 naming 8.7a/8.7b as runtime backing. Epic 10: unaffected in scope; coordination note on 11.6 (topology change affects local-run flows Epic 10 verification uses). |
| 2.4 Obsolete / new epics | [x] Done | None obsolete. One new epic (11) — the gap is a coherent refactor program, not loose stories; keeps Epic 8/10 goals clean. |
| 2.5 Re-sequence / priority | [x] Done | M0→M1→M2 unchanged. Epic 11 is M2 and must close **before MVP readiness sign-off** (same gate class as Epic 10). Internal order: 11.1 (ADR, gates all) → 11.2 (platform hook) → 11.3/11.4 (parallelizable) → 11.5 → 11.6. Stories 11.5/11.6 land **after 8.7a/8.7b** so the host migration doesn't chase a moving enforcement seam. |
| 3.1 PRD conflicts | [N/A] | No FR/NFR changes. The host layer is below PRD altitude; FR81a's invariant (one shared admission path, no second pipeline) is *strengthened* by mounting the gateway as an SDK hook. MVP scope unchanged. |
| 3.2 Architecture conflicts | [!] Action | `architecture.md` is silent on the SDK and prescribes the (now deprecated) own-AppHost scaffold. Add **D8 (host-layer reuse)**, mark `Aspire`/`AppHost`/`ServiceDefaults` transitional, link the ADR. Edits in §4.2. |
| 3.3 UI/UX conflicts | [N/A] | No UI surface or UX-DR is touched. Epic 10 shell work is orthogonal (UI project vs Server host). |
| 3.4 Other artifacts | [!] Action | `sprint-status.yaml` (add epic-11 entries, split 8-7), `index.md` (counts + notes), `docs/adrs/domainservice-sdk-host-adoption.md` (authored by Story 11.1). Story 11.2 requires an `Hexalith.EventStore` repo change — explicit submodule approval + that repo's conventions (Conventional Commits, central package versions, xUnit v3/Shouldly, `ConfigureAwait(false)`, no copyright headers). |
| 4.1 Direct Adjustment | [x] Viable | New epic + targeted edits; no scope loss. Effort: **Medium** (staged refactor of a working host, protected by existing Tier-1/2/3 + conformance + isolation suites and new NetArchTest guards). Risk: **Medium**, mitigated by ADR gating, stage order, and behavior-parity acceptance criteria. |
| 4.2 Rollback | [N/A] Not viable | Nothing to roll back — the host works; the defect is *which layer owns it*. Reverting stories would not produce SDK adoption. |
| 4.3 MVP Review | [N/A] Not viable | MVP scope and goals unchanged; this is technical-layer alignment, not scope reduction. |
| 4.4 Recommendation | [x] Done | **Direct Adjustment (Hybrid):** 1 new epic (11) + 8.7 split + annotation/polish edits. |
| 5.1–5.5 Proposal components | [x] Done | This document. |
| 6.1–6.2 Final review | [x] Done | All edits trace to pass-2 findings; decisions (full adoption, all-three scope, batch, Epic-11 placement, 8.7 split) confirmed by Jerome 2026-06-09. |

## 3. Recommended Approach

**Direct Adjustment (Hybrid)** — chosen over rollback (nothing to revert) and MVP review (scope unchanged):

1. **Issue #1 → Epic 11 (new, M2):** Convert the unmade host decision into a gated refactor program: ADR first (11.1), platform capability second (11.2), then migrate surfaces in dependency order (queries 11.3, projections/telemetry/health 11.4), then reduce the host (11.5), then retire the self-orchestration (11.6). This is the same pattern the readiness-blockers proposal used for CR-1/CR-2 (decision → owning story; 10.6a ADR gates 10.6b).
2. **Issue #2 → visibility edits:** Per-story runtime-activation riders on 7.12–7.26 + a parenthetical scope clarifier in each lead Then-clause; preconditions on 9.2/9.13; split 8.7 into 8.7a/8.7b (matches the fine-grained, independently-sprintable story preference). Done stories stay `done` — the riders make the accepted scope (recorded, audited control decision; runtime effect at 8.7a/b) explicit instead of implicit.
3. **Issue #3 → polish edits:** beneficiary-first role lines, increment-scoped ACs (2.7, 4.9), FR35 scoping (3.11), per-item-type differentiation for 3.2–3.8, FR tags for 1.9.

**Why full SDK adoption (vs recorded exception):** Jerome's directive is explicitly "minimal technical layer / no unneeded boilerplate"; the SDK exists and is proven by `tenants`/`sample`; ChatBot has no migration debt excuse (greenfield module on a platform whose rule is dated *before* more code hardens); and the admission layer — the only defensible custom-host reason — is exactly the capability the platform rule says to push into the SDK. The recorded-exception path would mechanically cap the boilerplate but leave ~1221 hand-rolled lines as permanent drift from the platform rule.

**Effort/risk/timeline:** Medium/Medium. Epic 11 adds one M2 epic to the release-readiness closure set (alongside Epic 10); 11.2 is the long pole (platform release cycle). No M0/M1 work is affected.

## 4. Detailed Change Proposals

### 4.1 `epics.md` — Epic 11 (new), Epic 8 split, Epic 7/9 riders, polish

#### 4.1.1 Frontmatter & Epic List metadata

```
OLD: epicCount: 10 / storyCount: 121
NEW: epicCount: 11 / storyCount: 128   (+ hostReuseAlignedAt: "2026-06-09")
```

Epic List intro: "10 epics" → "11 epics"; "Epics 8-10 deliver M2" → "Epics 8-11 deliver M2"; add to the intro: "Epic 11 is the approved host-layer reuse closure epic added on 2026-06-09 (readiness pass-2); like Epic 10 it must close before MVP readiness sign-off."

New Epic List entry (after Epic 10):

> ### Epic 11: Minimal Technical Layer — DomainService SDK Host Adoption
> Align the ChatBot host layer with the platform's domain-centric SDK (`Hexalith.EventStore.DomainService`): record the decision as an ADR, add the FR81a pre-commit admission hook to the platform SDK, migrate queries/projections/cursors/telemetry/health to SDK contracts, reduce the Server host toward the 2-line shape with the CommandGateway mounted as the SDK admission hook, and retire the module-owned `AppHost`/`Aspire`/`ServiceDefaults` in favor of `AddEventStoreDomainModule(...)` composition. Platform-conformance epic: no new FRs; preserves the FR81a invariant by construction and enforces the architecture D8 minimal-technical-layer mandate mechanically (NetArchTest).
> **FRs covered:** none new — extends FR81/FR81a enforcement; closes readiness pass-2 Issue #1.

#### 4.1.2 New Epic 11 section (full text, inserted after Story 10.7)

> ## Epic 11: Minimal Technical Layer — DomainService SDK Host Adoption
>
> Make the ChatBot module domain-centric per the EventStore "Domain-Module Authoring" rule: domain code plus a ~2-line host, with all hosting boilerplate supplied by the platform SDK. The FR81a CommandGateway admission layer is preserved exactly — it mounts as the SDK's pre-commit admission hook instead of justifying a hand-rolled host. Decision evidence: readiness report 2026-06-09 pass-2 (1221-line `Program.cs`, 0 SDK-contract usages, module-owned `AppHost`/`Aspire`/`ServiceDefaults`, planning artifacts silent on the SDK).
>
> **Sequencing (binding):** 11.1 gates all other stories (ADR-first, mirroring 10.6a→10.6b). 11.2 precedes 11.3–11.6 (platform capability before consumption). 11.3 and 11.4 are parallelizable. 11.5 and 11.6 land **after Stories 8.7a/8.7b** so host migration does not chase a moving enforcement seam, and 11.6 coordinates with Epic 10 verification (local-run topology changes).
>
> ### Story 11.1: Host-reuse ADR — DomainService SDK adoption decision record
>
> As a platform architect,
> I want the host-layer reuse decision recorded as an accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md`,
> So that SDK adoption is a dated, reviewable architecture decision instead of silent drift.
>
> **Acceptance Criteria:**
>
> **Given** readiness pass-2 Issue #1
> **When** the ADR is authored
> **Then** it records: full adoption of `Hexalith.EventStore.DomainService`; the FR81a CommandGateway pre-commit admission hook as a **platform SDK capability** (not a domain bypass); the target ~2-line host shape; the SDK contract bindings (`IDomainQueryHandler`, `IDomainProjectionHandler`, `IReadModelStore`/`ReadModelWritePolicy`, `IQueryCursorCodec`/`QueryCursorScope`, `AddEventStoreDomainTelemetry`, `AddEventStoreDomainStateStoreHealthCheck`); the migration order (11.2 → 11.3/11.4 → 11.5 → 11.6); and an **explicit exception boundary** — anything ChatBot may still hand-roll (e.g. a thin umbrella local-dev AppHost for the multi-sibling topology), each with a dated justification.
>
> **Given** `architecture.md` decision D8
> **When** the ADR is accepted
> **Then** D8 and the ADR agree, and `architecture.md` links the ADR.
>
> **And** Stories 11.2–11.6 must not start before this ADR is accepted (gating mirrors 10.6a → 10.6b).
>
> ### Story 11.2: Platform pre-commit admission hook in the DomainService SDK
>
> As a platform architect,
> I want the `Hexalith.EventStore.DomainService` SDK to expose an opt-in pre-commit admission hook,
> So that a domain module can mount governance stages (the FR81a admission layer) without abandoning the 2-line host.
>
> **Acceptance Criteria:**
>
> **Given** the `Hexalith.EventStore` repository (work happens in the submodule's own repo with explicit approval, following its conventions: Conventional Commits, central package versions, xUnit v3 + Shouldly, `ConfigureAwait(false)`, no copyright headers, `.slnx` only)
> **When** the SDK gains the hook
> **Then** `AddEventStoreDomainService()` accepts a registered admission-stage chain executed **before** dispatch into the EventStore write path, failing closed on stage rejection, with the canonical DAPR endpoints (`/process`, `/replay-state`, `/query`, `/project`, `/admin/operational-index-metadata`) unchanged.
>
> **Given** the existing 2-line hosts (Counter sample, Tenants)
> **When** built against the new SDK
> **Then** they compile and behave unchanged (the hook is opt-in).
>
> **Given** an admission stage rejects a command
> **When** the hook executes
> **Then** the rejection surfaces as a typed domain rejection (rejections-as-events posture preserved) and telemetry flows through the SDK domain telemetry source.
>
> **And** the capability ships as a platform release (semantic-release) consumable by ChatBot via the pinned submodule.
>
> ### Story 11.3: Migrate ChatBot query endpoints to `IDomainQueryHandler` + `IQueryCursorCodec`
>
> As a ChatBot maintainer,
> I want the ~15 inline `MapGet`/`MapPost` query endpoints in `Program.cs` replaced by `IDomainQueryHandler` implementations with `IQueryCursorCodec`/`QueryCursorScope` pagination,
> So that query plumbing is SDK-provided, discovered, and routed — not hand-rolled.
>
> **Acceptance Criteria:**
>
> **Given** each existing query endpoint
> **When** it is reimplemented as an `IDomainQueryHandler`
> **Then** responses are behavior-identical (payload shape, RFC 9457 problem responses, redaction, tenant isolation, `stale|rebuilding|unavailable` signaling), proven by endpoint-parity tests plus the existing Tier-1/2 suites.
>
> **Given** paginated queries
> **When** cursors are issued
> **Then** they use `IQueryCursorCodec`/`QueryCursorScope`; no hand-rolled cursor codec remains in ChatBot `src`.
>
> **And** the migrated inline endpoints are deleted from `Program.cs` in the same change.
>
> ### Story 11.4: Migrate projections, telemetry, and health to SDK contracts
>
> As a ChatBot maintainer,
> I want projections on `IDomainProjectionHandler`, read models on `IReadModelStore` + `ReadModelWritePolicy`, and telemetry/health on the SDK helpers,
> So that no per-domain projection/telemetry/health plumbing is re-implemented in the domain.
>
> **Acceptance Criteria:**
>
> **Given** ChatBot projection handlers
> **When** migrated to `IDomainProjectionHandler` (SDK-dispatched `/project`)
> **Then** idempotent, order-tolerant behavior (version-stamped, last-writer-wins by source version) is unchanged, proven by the existing projection test suites.
>
> **Given** per-domain `ActivitySource`/`Meter`/health-check classes
> **When** replaced by `AddEventStoreDomainTelemetry("chatbot")` and `AddEventStoreDomainStateStoreHealthCheck("chatbot")`
> **Then** emitted telemetry and health endpoints remain functionally equivalent (correlation propagation, metadata-only logging intact).
>
> **And** read-model persistence uses `IReadModelStore` + `ReadModelWritePolicy`; no hand-rolled state-store wrapper remains for read models.
>
> ### Story 11.5: Reduce the Server host to the SDK shape with the CommandGateway admission hook
>
> As a ChatBot maintainer,
> I want `Hexalith.ChatBot.Server` hosted by `AddEventStoreDomainService()`/`UseEventStoreDomainService()` with the CommandGateway registered as the SDK admission-stage chain,
> So that the host is the platform's, the governance is ChatBot's, and the 1221-line `Program.cs` disappears.
>
> **Acceptance Criteria:**
>
> **Given** the migrated module (11.3/11.4 complete; 8.7a/8.7b landed)
> **When** the host is reduced
> **Then** `Hexalith.ChatBot.Server` references `Hexalith.EventStore.DomainService` (dropping direct `.Client`/`.Contracts` references where transitively provided) and `Program.cs` reduces to `AddEventStoreDomainService(...)` + admission-chain registration + `UseEventStoreDomainService()` (target ≤ ~50 lines).
>
> **Given** the FR81a invariant
> **When** the CommandGateway mounts as the SDK hook
> **Then** the stage order (`auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit`) is preserved, governance interfaces stay `internal`, and the differential-conformance + cross-tenant isolation + fail-closed suites are green unchanged.
>
> **And** NetArchTest is extended to forbid regrowth: no inline query endpoint mapping in the Server host, no per-domain telemetry/health classes, no hand-rolled host wiring beyond the SDK calls + admission registration (mechanical enforcement of "minimal technical layer").
>
> ### Story 11.6: Retire module-owned `AppHost`/`Aspire`/`ServiceDefaults`; compose via `AddEventStoreDomainModule`
>
> As a platform operator,
> I want ChatBot composed like `tenants`/`sample` — via `AddEventStoreDomainModule(eventStoreResources, "chatbot", …)` — instead of orchestrating itself,
> So that the module ships zero hosting boilerplate and the topology has one owner.
>
> **Acceptance Criteria:**
>
> **Given** the ADR's recorded composition boundary
> **When** composition moves to `AddEventStoreDomainModule(...)`
> **Then** ChatBot's DAPR resources (`chatbot-statestore`, `chatbot-pubsub`, access-control files) are supplied through the platform composition, and `Hexalith.ChatBot.AppHost`/`.Aspire`/`.ServiceDefaults` are removed — or reduced to a thin umbrella local-dev shim for the multi-sibling topology (siblings + Keycloak) **only if** the 11.1 ADR records that exception with justification.
>
> **Given** the new topology
> **When** the Tier-3 live Aspire/DAPR E2E suite runs
> **Then** it is green (placement/scheduler prerequisites, ACL posture, and sidecar wiring per the established Tier-3 run procedure), and the UI/CLI/MCP launch paths used by Epic 10 verification still work.
>
> **And** solution/project count shrinks accordingly; no orphan project remains in `Hexalith.ChatBot.slnx`.

#### 4.1.3 Story 8.7 split (Epic 8)

`### Story 8.7` heading becomes a **parent planning container** (non-assignable, precedent: 1.1, 7.27) keeping the CR-2 context note and the release-gate line (updated to name 8.7a/8.7b). Its six ACs redistribute:

> ### Story 8.7a: Durable control-state/rate-limit projection and enforcement-seam activation
> *(ACs: projection materializes control state per tenant × subject and replaces `AlwaysActive…`/`AlwaysUnlimited…` at the enforcement seam — FR74/FR75; disabled/quarantined subject blocked fail-closed with before/after proof — FR74, FR68, NFR7, NFR15a; rate-limited subject throttled, unrelated tenants unaffected — FR75, NFR30; staleness/revocation bounds — NFR6; mechanical no-defaults guard + Release-clean build.)*
>
> ### Story 8.7b: Periodic enforcement trigger and deferred evaluator consolidation
> *(ACs: Dapr-timer/`BackgroundService` trigger drives the 7.6–7.11 notification/escalation/throttle/backlog/rubber-stamp evaluators, the 8.4 `OperationalAlertWiringCoordinator`, the 8.5 weekly runbook sampler, and the per-tenant audit-checkpoint feed — FR67, FR72, FR73, NFR43, NFR44, NFR50a; trigger health observable; green default lane.)*

`8.7a` precedes and gates `8.7b` (the trigger consumes the projection).

#### 4.1.4 Epic 7 riders (stories 7.12–7.26) — Issue #2

Insert under **each** of the 15 story headings:

> \> **Runtime activation (M2):** enforcement is materialized by Stories 8.7a/8.7b. Until they land, this story's accepted scope is the recorded, audited control decision over `GovernedOperationAggregate` — wired and tested, inert at runtime (architecture control-floor note).

And append to each story's **lead Then-clause**: `(runtime enforcement activates via Story 8.7a/8.7b)`. Example (7.12):

```
OLD: **Then** intake from that mailbox source is blocked, existing workflow items remain auditable, and safe recovery guidance is shown.
NEW: **Then** intake from that mailbox source is blocked (runtime enforcement activates via Story 8.7a/8.7b), existing workflow items remain auditable, and safe recovery guidance is shown.
```

Same parenthetical on the lead Then of 7.13–7.26. Rationale: stories stay `done`; the accepted decision-recording scope is made explicit rather than rewriting accepted ACs into different promises.

#### 4.1.5 Epic 9 riders (9.2, 9.13) — Issue #2

Under **Story 9.2** heading:

> \> **Precondition — runtime backing (M2):** the completeness observable and the scheduled production assertion assume the live control-plane runtime loop delivered by Stories 8.7a/8.7b; NFR50a production sign-off must not be claimed before they land.

Under **Story 9.13** heading:

> \> **Precondition — runtime backing (M2):** scoped-outage validation across control/enforcement paths assumes Stories 8.7a/8.7b runtime activation; validation evidence produced earlier must be re-run after activation.

#### 4.1.6 Minor polish — Issue #3

**(a) Beneficiary-first role lines** (2.3, 2.4, 3.14, 4.1, 4.7): replace `As the system,` with the beneficiary and recast "I want" as a system capability; delete the now-redundant `**Beneficiary:**` line. Example (2.3):

```
OLD: As the system, / I want a deterministic-signals scorer that produces a confidence score and ranked authorized candidates, / So that … + **Beneficiary:** line
NEW: As an authorized reviewer receiving auto-associated mail or evidence-backed candidates, / I want the system to score associations deterministically and produce a confidence score with ranked authorized candidates, / So that strong deterministic matches auto-associate and everything else gets evidence-backed candidates.
```

(2.4 → "As a reviewer protected from silent workspace contamination…"; 3.14 → "As an approver/reviewer inspecting AI-context eligibility…"; 4.1 → "As a reviewer of actionable requests…"; 4.7 → "As a security owner…", each with "I want the system to…".)

**(b) Story 2.7 increment hygiene:** the third AC (`Given tenant policy permits it (M1) … (FR96)`) is re-labeled an explicit extension, not M0 acceptance:

```
NEW lead-in: **M1 extension (FR96 — activates with Epic 7 tenant policy; not part of M0 acceptance):** followed by the unchanged Given/When/Then.
```

**(c) Story 4.9:** `When all M0/M1 invalidation acknowledgements are complete` → `When all invalidation acknowledgements required by the active increment's correction-propagation contract (Story 2.8) are complete`.

**(d) Story 3.11 FR35 scoping:** `actionable items surface detected intent (FR35)` → `actionable items surface the detected intent produced by the Epic 4 task-intent kernel (FR35 — detection owned by Story 4.1; this story renders it)`.

**(e) Stories 3.2–3.8 differentiation:** add one type-specific And-clause per story (no new FR tags):

| Story | Added And-clause: "**And** it renders the item-type-specific fields: …" |
|---|---|
| 3.2 email | sender identity and source-mailbox provenance, subject, received timestamp, informational/actionable badge |
| 3.3 participant | participant class (internal/external/unresolved), resolution state, safe identity evidence |
| 3.4 attachment | authorized filename, type/size metadata, scan/quarantine status, governed-folder link |
| 3.5 decision | decision type (associate/correct/reject/defer/needs-review), deciding actor, evidence link, supersedes/superseded-by chain |
| 3.6 approval | requested-action summary, approval state, approver attribution, policy-snapshot link |
| 3.7 failure/retry/blocked | failure class, retry count and next-retry time, catalogued message code, next safe action |
| 3.8 AI outcome | proposal/denial/execution/outcome state, model+version provenance string, link to the governing approval record |

**(f) Story 1.9 FR tags:** AC1 flow Then-clause gains `(FR81, FR81a)`; AC3 ("tenant partitioning, fail-closed behavior, and audit/idempotency are real") gains `(FR16, FR55, FR68; NFR13a, NFR15a)`.

#### 4.1.7 Scaffold "Additional Requirements" + D-decisions block

- Module-layout bullet (the one naming `Aspire`, `AppHost`, `ServiceDefaults`): append `— **Transitional (D8):** the module-owned Aspire/AppHost/ServiceDefaults projects are retired by Epic 11 in favor of the `Hexalith.EventStore.DomainService` SDK host + `AddEventStoreDomainModule(...)` composition.`
- D1–D7 block gains:

> - **D8 — Host-layer reuse (added 2026-06-09, readiness pass-2):** ChatBot is an EventStore **domain module** hosted on the `Hexalith.EventStore.DomainService` SDK (~2-line host; `IDomainQueryHandler`/`IDomainProjectionHandler`/`IQueryCursorCodec`/`IReadModelStore`; SDK telemetry/health). The FR81a CommandGateway admission layer mounts as the SDK's **pre-commit admission hook** (platform capability, Story 11.2) — reinforcing, not weakening, "NOT a second pipeline". Module-owned `AppHost`/`Aspire`/`ServiceDefaults` are transitional until Epic 11. ADR: `docs/adrs/domainservice-sdk-host-adoption.md`.

### 4.2 `architecture.md` — D8 + transitional markers

1. **Decision Priority Analysis** "Critical decisions — now made" list: add the D8 line (same text as 4.1.7).
2. **New subsection** after "Infrastructure & Deployment" — *Host-Layer Reuse (D8)*: target state (SDK host, hook-mounted gateway, SDK query/projection/cursor/telemetry/health bindings, `AddEventStoreDomainModule` composition), transition state (hand-rolled 1221-line host remains until Epic 11 completes; any retained umbrella AppHost must carry the ADR-recorded exception), and mechanical enforcement (NetArchTest anti-regrowth rules from 11.5).
3. **Starter Template "Initialization"** bullet listing `Aspire`, `AppHost`, `ServiceDefaults`: append "(transitional — retired by Epic 11 per D8)".
4. **Directory tree**: annotate `Hexalith.ChatBot.Aspire/`, `.AppHost/`, `.ServiceDefaults/` lines with `[transitional — retired by Epic 11, D8]`.
5. **Control-floor note (M0-walking-skeleton paragraph)** and any "Story 8.7" references: → "Stories 8.7a/8.7b".

### 4.3 `sprint-status.yaml`

- Replace `8-7-control-plane-runtime-activation: backlog` with a parent comment + `8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation: backlog` + `8-7b-periodic-enforcement-trigger-and-deferred-evaluator-consolidation: backlog`.
- Append `epic-11: backlog` with the six `11-x` stories (`backlog`) + `epic-11-retrospective: backlog` (matching existing epic entry shape).
- Refresh `last_updated` + header comment referencing this proposal.

### 4.4 `index.md`

Update Current Readiness Notes: 11 epics / 128 assignable stories; Epic 11 host-reuse closure (must close before MVP sign-off; 11.1 ADR gates 11.2–11.6); 8.7 → 8.7a/8.7b; register this proposal and the pass-2 report.

### 4.5 New ADR (authored by Story 11.1, not by this proposal)

`docs/adrs/domainservice-sdk-host-adoption.md` — required content enumerated in Story 11.1's ACs. This proposal deliberately does **not** pre-write the ADR: the decision *record* belongs to the gated story, mirroring the 10.6a precedent.

## 5. Implementation Handoff

**Scope classification: Moderate** (backlog reorganization + architecture-document updates; no PRD change, no code rollback).

| Owner | Responsibility |
|---|---|
| PO/Developer (this session, on approval) | Apply §4.1–4.4 edits to `epics.md`, `architecture.md`, `sprint-status.yaml`, `index.md`. |
| Architect (Story 11.1) | Author + accept the ADR; confirm the 11.6 composition/exception boundary. |
| Platform developer (Story 11.2) | EventStore-repo SDK hook — requires explicit submodule approval; own PR/release in `Hexalith.EventStore`. |
| Developer agents (11.3–11.6, 8.7a/8.7b) | Staged migration per the binding sequencing; Tier-3 E2E proof on the new topology. |

**Success criteria:** planning artifacts mention and bind the DomainService SDK (0 → authoritative); every readiness pass-2 issue (#1–#3) has either an applied edit or an owning, gated story; Epic 11 closes before MVP readiness sign-off with `Program.cs` ≤ ~50 lines, 0 module-owned hosting projects (or an ADR-recorded exception), and NetArchTest anti-regrowth guards green.

**What does NOT change:** PRD/addendum (no FR/NFR edits); UX package; done-story statuses; the CommandGateway stage semantics (FR81a invariant preserved, relocated onto the SDK hook); M0→M1→M2 order.
