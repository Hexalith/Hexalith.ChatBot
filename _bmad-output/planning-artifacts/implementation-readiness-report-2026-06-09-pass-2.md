---
stepsCompleted: ['step-01-document-discovery', 'step-02-prd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
overallStatus: 'NEEDS WORK (narrowly scoped) — 100% FR coverage + high planning quality; one significant host-layer reuse/boilerplate decision unresolved (DomainService SDK bypassed), plus targeted story-dependency edits.'
assessmentFocus: "Reuse existing submodule classes; minimal technical layer; no unneeded boilerplate — domain module should carry only what's needed to spin up required servers."
documentsAssessed:
  prd: 'prds/prd-Hexalith.ChatBot-2026-05-28/prd.md'
  prdAddendum: 'prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md'
  architecture: 'architecture.md'
  epics: 'epics.md'
  uxDesign: 'ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md'
  uxExperience: 'ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md'
  uxElaborations:
    - 'ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md'
    - 'ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md'
priorReport: 'implementation-readiness-report-2026-06-09.md'
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-09 (pass 2)
**Project:** Chatbot (Hexalith.ChatBot)

**Assessment focus (user-directed):** Verify the plan reuses existing classes from the technical submodules and does **not** introduce unneeded boilerplate. This is a **domain module** — it should carry a minimal technical layer, just what is needed to spin up the required servers.

---

## Step 1 — Document Inventory

All canonical artifacts resolve unambiguously (one source per type; no whole-vs-sharded duplicate conflicts). Selections cross-checked against `index.md`.

| Type | Canonical source | Size | Modified |
|------|------------------|------|----------|
| PRD | `prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | 192 KB | 2026-06-09 |
| PRD addendum (binding) | `prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | 22.8 KB | 2026-06-09 |
| Architecture | `architecture.md` | 70 KB | 2026-06-09 |
| Epics & Stories | `epics.md` (10 epics / 121 stories) | 218 KB | 2026-06-09 |
| UX — Design spine | `ux-designs/.../DESIGN.md` | 15.6 KB | 2026-06-05 |
| UX — Experience spine | `ux-designs/.../EXPERIENCE.md` | 36 KB | 2026-06-05 |
| UX — M1/M2 elaboration | `ux-designs/.../m1-m2-surface-elaboration.md` | 4.3 KB | 2026-06-05 |
| UX — Epic 10 elaboration | `ux-designs/.../epic10-chat-surface-elaboration.md` | 4.4 KB | 2026-06-09 |

**Supporting (informative, not assessment targets):** `index.md`, `product-brief-Hexalith.ChatBot.md`, 5 prior readiness reports (05-29 → 06-09), 9 sprint-change proposals (latest: `…06-09-readiness-alignment.md`, `…06-09-readiness-blockers.md`, `…06-09-packages.md`).

**Discovery result:** ✅ No missing required documents. ✅ No duplicate-format conflicts. Output written to `-pass-2` to preserve the existing 2026-06-09 report.

---

## Step 2 — PRD Analysis

The PRD was read in full (1,481 lines) together with its **binding** `addendum.md` (189 lines, `status: approved`, `approvedAt: 2026-06-09`). Rather than re-paste ~190 requirement paragraphs that already live verbatim in `prd.md`, this section records the **complete enumerated catalog, structure, and binding contracts** — which is what traceability validation (Step 3+) consumes — and flags the items most material to the user-directed reuse/boilerplate concern.

### Functional Requirements — catalog (≈111 items)

- **Base:** FR1–FR96 (96 items).
- **Lettered refinements (15):** FR48a–d (inbound authenticity), FR55a (derived-store cross-tenant isolation), FR75a–g (bounded tenant-admin permission model), FR81a (**shared command pipeline — architectural invariant**), FR91a (correction-propagation contract), FR95a (replay isolation).
- **FR groups (10):** Project Email Intake & Association (FR1–12) · Participants/Identity/Authorization (FR13–20) · Project Conversation & Context (FR21–28) · Files & Attachments (FR29–34) · Task Intent & AI Action Mediation (FR35–46) · Outbound Communication (FR47–50) · Admin/Governance/Audit (FR51–63) · Reliability/Failure/Ops (FR64–80) · Cross-Surface Command Parity (FR81–86) · Workflow State/Contracts/Testability (FR87–96).

### Non-Functional Requirements — catalog (77 items)

- **Base:** NFR1–NFR70 (70 items).
- **Lettered refinements (7):** NFR9a (derived-store isolation), NFR13a (per-op idempotency contract), NFR15a (**fail-closed path inventory — invariant, 10 code paths**), NFR17a (correction-propagation latency), NFR42a (published SLOs), NFR49a (tamper-evident WORM/hash-chain mechanism), NFR50a (audit completeness as production observable).
- **Groups (9):** Security & Privacy (NFR1–12) · Reliability & Data Integrity (NFR13–22) · Performance & Scalability (NFR23–30) · Integration & Interoperability (NFR31–36) · Operability & Observability (NFR37–48) · Auditability/Compliance/Governance (NFR49–55) · Recovery & Continuity (NFR56–59) · Accessibility & Usability (NFR60–64) · Validation & Quality Gates (NFR65–70).

### Binding addendum contracts (downstream-load-bearing)

Confidence Thresholds (T_high/T_low) · Risk Classifier (tag-and-heuristic) · **Command Allowlist v0 (exactly one command: `Project.AppendConversationMessage`) / v1** · Tenant Policy Schema (closed knob set, M0/M1/M2) · **§Shared Command Pipeline (FR81a invariant)** · Idempotency Keys (8 operation classes) · Replay Isolation · ID Evolution Contract · Inbound Message Authenticity (+ FR48 5-class authority mapping) · Operating Baselines (M2 SLO catalog, code-authoritative `OperatingBaselineCatalog`).

### Open Assumptions (A1–A11) — confirm/replace before lock-in

A1 (M365 first mailbox) · A2 (one mailbox pattern first) · A3 (CLI/MCP parity proof scope) · A4 (default operating baselines) · A5 (AI provider policy) · A6 (audit retention vs GDPR) · A7 (no external portal in MVP) · A8 (fixed command allowlist) · A9/A9a (evaluation dataset: ≥500 @ M0, ≥2000 @ M1) · A10 (RPO ≤15m/RTO ≤4h starter) · A11 (pilot thresholds starter).

### PRD completeness assessment

- **Strong.** The PRD is unusually rigorous: explicit traceability matrix (journey → FR/NFR), per-FR "Accept when" clauses on high-risk items, an enumerated fail-closed path inventory (NFR15a), and machine-checkable observables on many NFRs. Requirements are testable and decomposition guidance is provided for the packed FRs (FR22, FR74, FR75).
- **Reuse/boilerplate signal (preliminary — confirmed strongly positive in Step 3):** FR81a + addendum §Shared Command Pipeline define a **"ChatBot admission layer"** whose stages substantially overlap platform capabilities. Architecture decision **D3** resolves this in the right direction (see Step 3). Only **risk classification, the approval gate, the command allowlist, association/confidence scoring, and the mailbox-intake adapter** are genuinely ChatBot-domain-specific; the rest is delegated to the platform. The one residual structural tension (ChatBot shipping its own `Aspire`/`AppHost`/`ServiceDefaults` vs the EventStore `DomainService` "two-line host" rule) is carried into Step 5.

---

## Step 3 — Epic Coverage Validation

### Coverage statistics
- **Total PRD FRs:** ~111 (96 base + 15 lettered).
- **FRs covered in epics:** **100%.** The epics `FR Coverage Map` maps every FR1–FR96 **and** every lettered sub-FR (FR48a–d, FR55a, FR75a–g, FR81a, FR91a, FR95a) to exactly one **primary** epic; cross-cutting M1/M2 extensions are noted (e.g. FR55 emission in E1, WORM in E9; FR80 UI in E1, CLI/MCP in E5). Programmatic check for FR1–FR96 returned **zero MISSING**.
- **FRs in epics but not in PRD:** none found (no orphan FRs).
- **NFRs:** treated as cross-cutting quality bars mapped to constraining epics (security/isolation across all; reliability/idempotency E1–E2; accessibility E2–E3/E7/E8; audit/recovery E1+E9; performance/observability E8) — consistent with PRD intent.
- **UX-DRs:** all 46 mapped to surface stories; cross-cutting visual/a11y/localization UX-DRs anchored in Stories 1.14–1.21 and inherited downstream.

### Coverage structure (10 epics / 121 stories, M0→M1→M2 dependency-ordered)
| Inc | Epic | Primary FRs |
|-----|------|-------------|
| M0 | E1 First Safe Governed Action & Command Spine | FR16,55,57,59,61,68,77,80,81,81a,85,86,87,88,89,90,92,93 |
| M0 | E2 Email Intake & Project Association | FR1–15,17,60,62–66,71,76,79,91,91a,96 |
| M0 | E3 Conversation Context, Files & Attachments | FR21–34 |
| M0 | E4 Governed AI Action Mediation | FR35–46 |
| M1 | E5 Cross-Surface Parity — CLI & MCP | FR19,82,83,84 (+80,85,86 extend) |
| M1 | E6 Outbound Communication & Inbound Authenticity | FR47,48,48a–d,49,50 |
| M1 | E7 Tenant Administration & Governance Policy | FR18,51–53,69,70,72–75,75a–g,78 |
| M2 | E8 Operational Dashboards & Observability | FR67,94 (+8.7 runtime activation) |
| M2 | E9 Tamper-Evident Audit, Compliance & Recovery | FR20,54,55a,56,58,95,95a |
| M2 | E10 Interactive Chat Surface & FrontComposer Shell | extends FR21,40–46,81,81a,85,86 |

### Coverage observations
- **No requirement falls through the cracks.** Traceability is bidirectional (PRD→epic map + epic→FR "FRs covered" rosters) and internally consistent.
- **Known deferred-runtime dependency is explicit, not hidden:** FR74/FR75 control floor is wired+tested in E7 but **inert until Story 8.7** materializes the durable control-state projection + periodic trigger (matches `index.md` CR-2). This is a sequencing dependency to watch, not a coverage gap.
- **Architecture (D1–D7) + scaffold seeds are inventoried as first-class "Additional Requirements"** driving E1/Story 1, which is what makes the reuse posture auditable. Key reuse-positive decisions captured below.

### Reuse / boilerplate posture — Step 3 evidence (the user's focus)
The plan is **strongly reuse-oriented** and explicitly anti-boilerplate:
- **No external starter template.** Generic .NET/Blazor/Clean-Arch/ABP starters are *rejected*; the module is scaffolded **by convention from the canonical sibling-module template** — structural reference **Hexalith.Folders**, domain reference **Hexalith.Conversations** (reuse `IParticipantDirectory` adapter pattern).
- **D3 (FR81a placement) — decisive:** the `CommandGateway` admission layer runs only the ChatBot-specific stages (`auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit`) and then **dispatches into EventStore's *existing* write path** (`fine-idempotency → execute → publish → projection`) + post-commit audit. Explicitly **"NOT a second pipeline."** Governance interfaces (`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are `internal` to `.Server`; **stage-replication is a compile error** (NetArchTest-enforced).
- **D1 — sibling integration:** writes to Projects/Parties/Folders/Conversations go through **their** EventStore commands via ChatBot-owned adapter ports; ChatBot maintains derived state from their published events and **"stays an orchestrator, never a source of truth."**
- **Platform reuse pinned:** EventStore (CQRS/ES, AggregateActor, SignalR, CLI/MCP primitives) as root submodule; FrontComposer (Blazor + Fluent UI v5, Roslyn source-gen, Fluxor, contract-first annotations) for UI; Hexalith.Memories (Redis Vector/FalkorDB) for M2 AI context; no hand-rolled vector store.
- **⚠️ One residual structural tension to resolve (carried to Step 5):** the scaffold (epics line 299) has ChatBot ship its own **`Aspire` + `AppHost` + `ServiceDefaults`** projects, whereas EventStore's `CLAUDE.md` **"Domain-Module Authoring (domain-centric)"** rule says a domain module **must not** ship those — it should reference the **`Hexalith.EventStore.DomainService`** SDK and reduce to a **~2-line host**, with the EventStore AppHost composing it via `AddEventStoreDomainModule(...)`. This is the single most important item for the "minimal technical layer / no unneeded boilerplate" directive and is examined in detail in Step 5.

---

## Step 4 — UX Alignment

### UX document status: **Found** (spine-only by design)
`DESIGN.md` (visual foundation) + `EXPERIENCE.md` (IA, components, states, interactions, a11y floor, 9 Key Flows) + two assignment-ready elaborations (`m1-m2-surface-elaboration.md`, `epic10-chat-surface-elaboration.md`). The package intentionally ships **no mockups/wireframes**; the spine tables are imported into every S-tagged surface story as **binding acceptance context**.

### UX ↔ PRD alignment: ✅ consistent
- EXPERIENCE.md's **9 Key Flows map 1:1** to the PRD's 8 user journeys + the system journey (UJ1–UJ8 + governed AI execution), and are tied through UX-DR46.
- All **46 UX-DRs** are inventoried in the epics and mapped to surface stories; no UX requirement is absent from the PRD/epic scope, and no surface invents behavior beyond PRD/addendum contracts ("spine wins over future mockups").
- Governed-write principle is preserved end-to-end: **no fake/freeform chat textbox** — every message is admitted via CommandGateway; risky requests become Epic 4 approval-required proposals (`Project.AppendConversationMessage`).

### UX ↔ Architecture alignment: ✅ supported (and reuse-positive)
- `architecture.md` §Frontend Architecture (lines 378–402) backs every UX surface on the **platform UI substrate**: Blazor + Fluent UI v5 (RC, via **FrontComposer** — Roslyn source-gen, Fluxor, REST commands/queries + SignalR projection-nudge, contract-first annotations).
- **FrontComposer Shell adoption is the headline reuse win (Epic 10, Story 10.1):** `Hexalith.ChatBot.UI` composes through `FrontComposerShell` via `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()`, consuming the **FrontComposer submodule read-only** — exactly the startup wiring the FrontComposer project-context prescribes. No custom UI shell, no bespoke design system. This also closes the Story 1.14 temporary token-alias bridge.
- Performance/responsiveness UX needs are backed by NFR24 (p95 ≤ 2s user-facing) and the SignalR-nudge "re-query, never trust payload" model.

### Warnings / open items (tracked, not gaps)
- **Open (tracked): AI-response streaming transport.** UX-DR32 requires progressive render + always-reachable Stop/Cancel; the current spine carries only SignalR projection-nudge. Resolution is an **ADR owned by Story 10.6a** (`docs/adrs/ai-response-streaming-transport.md`) that must be accepted **before Story 10.6b** implements it (matches `index.md` CR-1). The UX contract holds regardless of transport, and the decision must not weaken the fail-closed/never-trust-payload posture. ✅ Properly sequenced.
- **M1/M2 surface-elaboration gate** is explicit: S4/S6/S7/S8/S9/S10 stories cannot be assigned until the surface map + surface-specific acceptance context exists (`m1-m2-surface-elaboration.md` is the approved gate artifact). ✅ Guard in place.
- No UX↔architecture misalignment found.

---

## Step 5 — Epic Quality Review

Two independent sub-agent sweeps (epic-quality enforcer + reuse/anti-boilerplate auditor) read `epics.md` and `architecture.md` in full; their load-bearing claims were then **verified directly against the platform code**. Findings below are evidence-backed (every item traces to a quote, a file reference, or a verified grep).

### 5.1 Epic structure & story quality — verdict: **Ready-with-fixes** (no critical defects)

**Story counts (assignable):** E1≈24 (1.1 parent → 1.1a–d), E2=9, E3=14, E4=9, E5=4, E6=5, E7≈27 (7.27 parent → a/b), E8=7, E9=13, E10=8 (10.6→a/b). Parent "planning container" headings (1.1, 7.27) correctly marked non-assignable. Matches the "121" metadata.

**Strong practices (evidence-backed):**
- **Value-anchor invariant** on the scaffold epic: *"Story 1.9 is the epic's value proof. Every foundation story (1.1–1.8) must either unblock that first governed UI command or add a mechanical guardrail … a foundation story with no traceable link to Story 1.9 is out of scope."* Textbook framing of a Command-Spine epic around a first user-observable governed action.
- Per-story **"Anchor:"** lines tie each foundation story to Story 1.9 (clean dependency hygiene); BDD Given/When/Then is consistent with **negative/error paths in nearly every story** (fail-closed, redacted-denial, conflict-response, expired-evidence).
- Deferrals converted into **named, sequenced, owned** stories (10.6a ADR gates 10.6b; 8.6 saga binding) rather than vague "future" hooks.
- Aggregate/projection timing is **just-in-time** (scorer in 2.3, WORM chain in 9.1, vector reindex in 9.6), not front-loaded — correct for event-sourcing.

**🟠 Major (targeted edits, not rework):**
- **Cross-increment forward dependencies live in coverage-map notes, not in the dependent stories' own preconditions.** E7 control stories 7.12–7.26 assert runtime outcomes ("intake … is blocked") but the control floor is *"wired+tested in Epic 7 but inert until 8.7"* (M2). A team reading 7.12–7.26 in isolation would over-claim runtime enforcement. → Add an explicit "Runtime activation: Story 8.7 (M2)" precondition + soften ACs to "records the control decision."
- **E9 9.2 / 9.13 assume Story 8.7's live runtime loop** (*"Story 8.7 is the runtime backing … should land before MVP readiness sign-off"*) — add the dependency to those stories' preconditions.
- **Story 8.7 is broad** (control-state projection + periodic trigger driving 7.6–7.11, 8.4, 8.5, audit-checkpoint feed) — consider splitting 8.7a/8.7b.

**🟡 Minor:** M1-tagged ACs leak into M0 stories (2.7 FR96 "(M1)", 4.9 "M0/M1" clause); 3.11 references FR35 (owned by E4) inside an M0 E3 story; **"As the system…" role lines** on 2.3/2.4/3.14/4.1/4.7 (mitigated by an added "Beneficiary:" line — promote it into the role line); the seven near-identical 3.2–3.8 render stories have **templated ACs** with thin per-item differentiation; Story 1.9 (the value proof) is lighter on explicit FR tags than its siblings.

### 5.2 Starter-template / greenfield check ✅
Architecture specifies a starter (a new Hexalith module scaffolded by convention from the sibling template), and **Epic 1 Story 1.1a is the corresponding "scaffold the buildable module" story** (root config, build-green baseline) — the required first implementation story is present and correctly placed. Brownfield integration points (EventStore submodule, sibling adapter ports) are explicit.

### 5.3 Reuse / boilerplate audit — the user's directive (headline finding)

**✅ Domain-logic reuse is strong and correctly designed.** Verified in plan **and** code:
- `Hexalith.ChatBot.Server.csproj` references sibling **Clients + Contracts** — EventStore `.Client`/`.Contracts`, Folders `.Client`/`.Contracts`, Parties `.Client`/`.Contracts`, Tenants `.Contracts` — i.e., it consumes siblings as dependencies rather than copying them.
- Cross-context writes go through siblings' **own** EventStore commands via ChatBot-owned adapter ports (`IProjectDirectory`, `IParticipantDirectory` over Parties, `IConversationWriter` over Conversations) — reusing the Conversations adapter pattern; "ChatBot stays an orchestrator, never a source of truth."
- **CommandGateway (D3) is correctly a thin admission layer, NOT a second pipeline**: it runs only the ChatBot-specific stages (`risk-classify → approval-gate → coarse-idempotency → pre-commit-audit`) then dispatches into *EventStore's existing write path* (`fine-idempotency → execute → publish → projection`); fine idempotency reuses *"EventStore idempotency cache."* Stage-replication is a NetArchTest compile error.
- FrontComposer Shell consumed **read-only**; Memories reused for vectors; AI execution constrained to the sibling command `Project.AppendConversationMessage`. No parallel persistence/messaging/UI stack.

**⚠️ Hosting / infrastructure reuse is weak — and this is the single most important readiness gap for "minimal technical layer / no unneeded boilerplate":**

The platform ships a **domain-centric SDK** (`Hexalith.EventStore.DomainService`, **verified to exist and be usable today** — `AddEventStoreDomainService()`/`UseEventStoreDomainService()` are implemented, and the EventStore AppHost already composes `tenants` and `sample` via `AddEventStoreDomainModule(...)`). The EventStore `CLAUDE.md` "Domain-Module Authoring" rule is explicit: a domain module **must not** ship its own `AppHost`/`Aspire`/`ServiceDefaults`, the host `Program.cs` should be **~2 lines**, queries implement `IDomainQueryHandler`, projections `IDomainProjectionHandler`, cursors `IQueryCursorCodec`, telemetry/health via `AddEventStoreDomainTelemetry`/`AddEventStoreDomainStateStoreHealthCheck`; *"if a capability is missing, add it to the platform (SDK/Client/Aspire/ServiceDefaults), not the domain."*

Verified facts against that bar:
- **The planning artifacts never mention the SDK.** `DomainService`, `AddEventStoreDomainService`, `AddEventStoreDomainModule`, "domain-centric", "2-line host" appear **0 times** in `architecture.md`, `epics.md`, and the addendum. The host-reuse decision was never made in the plan.
- **The implemented module bypasses the SDK.** `Hexalith.ChatBot.Server` references EventStore **`.Client` + `.Contracts` only** (not `.DomainService`, not `.Server`). **Zero** SDK-contract usages (`IDomainQueryHandler`/`IDomainProjectionHandler`/`IReadModelStore`/`IQueryCursorCodec`/`AddEventStoreDomainTelemetry`) exist anywhere in ChatBot `src`. The host is a **1221-line hand-rolled `Program.cs`** with a custom `MapChatBotDomainServiceEndpoints()` and ~15 inline `MapGet/MapPost` query endpoints.
- The module ships its own **`AppHost` + `Aspire` + `ServiceDefaults`** (11 src projects, 47 in the solution), contradicting the "must not ship" rule.

**Fair nuance (so this isn't overstated):**
1. ChatBot's **FR81a admission layer** (pre-commit governance gates in front of EventStore) is a genuine reason the *plain* `AddEventStoreDomainService()` host may not fit as-is — some custom host wiring is defensible. **But the rule's prescribed response is to push that capability into the platform SDK as a pre-commit hook, not to hand-roll a 1221-line bypass host in the domain.** And the query/projection/telemetry/health/cursor surfaces (the bulk of the 1221 lines) are SDK-provided primitives with no admission-layer reason to be hand-rolled.
2. The reference siblings (**Folders, Conversations, Tenants**) themselves **still ship** their own `AppHost`/`Aspire`/`ServiceDefaults` — they predate the SDK and aren't migrated yet. So ChatBot faithfully copies the *current* sibling shape — but that shape is now the **explicitly-deprecated pre-SDK pattern**, and ChatBot is greenfield (no migration cost to start clean).

**Recommendation (carried as the top item into Step 6):** Make the host-reuse decision **explicit and recorded** (ADR + Story 1.1 acceptance update) — either (a) **adopt the `DomainService` SDK**: reduce the Server host toward `AddEventStoreDomainService()`/`UseEventStoreDomainService()`, drop/slim `AppHost`/`Aspire`/`ServiceDefaults` (compose via `AddEventStoreDomainModule(...)`), move query reads to `IDomainQueryHandler`, projections to `IDomainProjectionHandler`, cursors to `IQueryCursorCodec`, telemetry/health to the SDK helpers, and add the CommandGateway pre-commit governance gate **to the SDK** as a reusable hook; or (b) record a **dated, justified exception** explaining precisely why the admission layer requires a custom host, while still binding the query/projection/telemetry/health/cursor surfaces to the SDK contracts so the "minimal technical layer" intent is enforced mechanically. Today the plan does neither — it is silent, and the code has drifted to the hand-rolled extreme.

---

## Step 6 — Summary and Recommendations

### Overall readiness status: **NEEDS WORK (narrowly scoped)**

The planning artifacts are **high quality and substantially implementation-ready**: PRD/addendum are rigorous and testable, **FR coverage is 100%**, traceability is bidirectional and complete, UX is aligned and reuse-positive, and the epics have **no critical structural defects**. Against your specific directive, however, there is **one significant unresolved decision** — the host/infrastructure-layer reuse vs boilerplate question — plus a small set of story-level dependency edits. None of these is a deep rework; all are recordable/fixable without disturbing scope. The qualifier "needs work" attaches almost entirely to the reuse/boilerplate axis you asked me to scrutinize, not to the artifacts' completeness.

### Readiness by dimension
| Dimension | Status | Note |
|-----------|--------|------|
| Document completeness | ✅ Ready | All canonical artifacts present; no duplicates; `index.md` authoritative |
| PRD requirements | ✅ Ready | ~111 FR / 77 NFR, testable, binding addendum contracts |
| FR→Epic coverage | ✅ Ready | 100%; zero gaps; no orphan FRs |
| UX alignment | ✅ Ready | 9 flows ↔ journeys; FrontComposer Shell reuse; 1 tracked ADR (10.6a) |
| Epic/story quality | 🟠 Ready-with-fixes | No critical; cross-increment deps in notes; minor AC issues |
| **Reuse — domain logic** | ✅ Strong | Sibling clients/contracts reused; CommandGateway thin over EventStore; orchestrator-not-owner |
| **Reuse — hosting/infra (your focus)** | 🔴 Needs decision | DomainService SDK bypassed (verified: 1221-line host, 0 SDK usages, own AppHost/Aspire/ServiceDefaults); plan silent |

### Critical issues requiring action (in priority order)
1. **🔴 Host-layer reuse / boilerplate decision is missing and the code has drifted to a hand-rolled host (your directive).** The `Hexalith.EventStore.DomainService` SDK exists and is used by siblings, but ChatBot references only EventStore `.Client`/`.Contracts`, uses zero SDK contracts, hand-rolls a 1221-line `Program.cs` with ~15 inline query endpoints, and ships its own `AppHost`/`Aspire`/`ServiceDefaults`. The planning docs never evaluate the SDK. **This is the "unneeded boilerplate / minimal technical layer" risk made concrete.**
2. **🟠 Cross-increment forward dependencies are recorded only in the FR-coverage map, not in the dependent stories.** E7 control stories 7.12–7.26 read as live but are inert until M2 Story 8.7; E9 9.2/9.13 assume 8.7's runtime loop. A sprint team reading those stories in isolation will over-claim runtime enforcement.
3. **🟡 Increment hygiene + AC polish.** M1-tagged ACs leak into M0 stories (2.7, 4.9); "As the system…" role lines (2.3/2.4/3.14/4.1/4.7); templated near-duplicate ACs across 3.2–3.8; Story 1.9 under-tagged.

### Recommended next steps
1. **Author an ADR + update Story 1.1 acceptance** recording the host-reuse decision: adopt the `DomainService` SDK (2-line host; `IDomainQueryHandler`/`IDomainProjectionHandler`/`IQueryCursorCodec`; SDK telemetry/health; compose via `AddEventStoreDomainModule`; push the CommandGateway pre-commit governance gate **into the platform SDK** as a reusable hook), **or** record a dated, justified exception — and in either case bind the query/projection/telemetry/health/cursor surfaces to the SDK contracts so "minimal technical layer" is mechanically enforced (extend the NetArchTest suite to forbid hand-rolled host/query plumbing). Run this through `correct-course` (sprint-change proposal) since it touches architecture + Epic 1.
2. **Edit stories 7.12–7.26, 9.2, 9.13** to carry an explicit "Runtime activation: Story 8.7 (M2)" precondition and scope their ACs to "records the control decision"; consider splitting Story 8.7 into 8.7a/8.7b.
3. **Sweep M0 stories for increment leakage** (2.7, 4.9), promote "Beneficiary" into the "As a…" role line on the system-actor stories, and differentiate the 3.2–3.8 render-story ACs by the concrete fields unique to each item type.
4. **Keep the two already-guarded open items on the radar** (streaming-transport ADR 10.6a before 10.6b; M1/M2 surface-elaboration gate before S4/S6–S10) — these are correctly sequenced and need no change.

### Final note
This assessment found **1 significant reuse/boilerplate issue (your focus), ~3 major epic-dependency issues, and ~6 minor issues**, against a backdrop of **complete FR coverage and high planning quality**. The single most valuable action is **#1**: record the host-reuse decision and pull the hand-rolled host back toward the `DomainService` SDK before more code hardens around it. Domain-logic reuse is already done right; the gap is purely in the technical/hosting layer — exactly where you asked me to look. These findings can be applied via `correct-course`, or you may proceed as-is with the trade-offs documented above.

---

*Assessment by: Implementation Readiness workflow (BMAD) · Assessor focus per Jerome's directive: submodule reuse & minimal technical layer · Date: 2026-06-09 (pass 2) · Output preserves the existing 2026-06-09 report.*
