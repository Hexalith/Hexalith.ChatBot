# Sprint Change Proposal — Implementation-Readiness Rebaseline

- **Date:** 2026-07-17
- **Author:** Jerome (via Correct Course workflow)
- **Trigger:** The 2026-07-17 implementation-readiness assessment returned **NOT READY** with 12 issues: 3 critical, 4 major, 3 minor, and 2 UX/document-alignment issues.
- **Evidence:** `implementation-readiness-report-2026-07-17.md`, the authoritative PRD and addendum, `architecture.md`, `epics.md`, the two core UX specifications, the M1/M2 and Epic 10 UX elaborations, and `sprint-status.yaml`.
- **Review mode:** Batch
- **Scope classification:** **Major planning correction**. Product scope and the 111 functional requirements are unchanged. Existing code is preserved. The product epic hierarchy, acceptance contracts, evidence ownership, and sprint tracking require rebaselining before they can support implementation or release-readiness decisions.
- **Approval state:** **Approved by Jerome and implemented on 2026-07-17.** The artifact and status changes were applied exactly as specified below.

---

## 1. Issue Summary

The PRD, UX, and architecture provide a strong implementation basis, and functional traceability is complete at 111/111. The current epic plan is nevertheless not a trustworthy delivery contract:

1. Earlier epics claim behavior that becomes functional only in later epics: Stop/Cancel, production correction propagation, and runtime governance enforcement.
2. Epic 11 is a platform/host refactor with no standalone user outcome or new FR.
3. Epics 10 and 12 were closed before their declared UX outcomes were proven; Epics 12 and 13 are completion work for the same user-visible outcome rather than independent enhancements.
4. Permissive acceptance language, task-only stories, broad migration slices, missing negative/responsive cases, stale counts, ambiguous parent containers, citation errors, and UX-package drift make recurrence likely.

The correction must repair the planning contract without discarding valid implementation evidence or rolling back stable code.

## 2. Correct Course Checklist Status

| Checklist area | Status | Result |
| --- | --- | --- |
| Trigger and evidence | Complete | Readiness report and all authoritative inputs verified. |
| Epic and story impact | Complete | Affected dependencies, closure chain, technical epic, oversized stories, and tracking aliases mapped. |
| Artifact impact | Complete | Epics, architecture, UX package, sprint tracking, and technical-enabler tracking require changes; PRD does not. |
| Path evaluation | Complete | Direct rebaseline is viable; rollback and MVP reduction are not justified. |
| Detailed proposal | Complete | Exact target hierarchy, ownership moves, acceptance rules, status transitions, and handoff are defined below. |
| Approval and implementation | Complete | Jerome approved the batch proposal on 2026-07-17; the specified artifact and status changes were applied and validated. |

## 3. Impact Analysis

### 3.1 Product and MVP impact

- No FR, NFR, user journey, actor, safety invariant, or supported surface is removed.
- FR coverage remains **111/111**. The authoritative NFR inventory remains **77** identifiers.
- M0 → M1 → M2 remains the delivery order.
- Tenant isolation, authorization, fail-closed mutation, idempotency, audit completeness, governed AI approval, redaction, and CLI/MCP parity remain non-negotiable.
- No code rollback is proposed. Existing implementation records remain historical evidence and are cross-referenced from the rebaselined stories.

### 3.2 Epic impact

- Epics 1 and 2 retain their product goals but receive corrected acceptance ownership.
- Current Epic 7 is split into four independently valuable governance epics.
- Current Epic 8 retains operational hardening but gives first activation of correction and control behavior to the owning product epics.
- Current Epic 9 remains an independent audit/compliance/recovery epic.
- Current Epics 10, 12, and 13 become one user-value epic with surface-sized stories and a live-render closure gate.
- Current Epic 11 leaves the product epic hierarchy and becomes Technical Enabler TE-1.

### 3.3 Story and evidence impact

- Historical story files and test summaries are not deleted or rewritten as if they never existed.
- A migration table maps every superseded story ID to a canonical product story or TE-1 task.
- Affected canonical stories begin in `review`, and affected epics begin `in-progress`, until their existing evidence is reconciled against the corrected acceptance criteria.
- Passing historical evidence may satisfy a corrected criterion only when it exercised the required primary path and directly proves the invariant. Static fixtures and diagnostic fallbacks cannot satisfy live topology or live-render claims.

### 3.4 Document impact

| Artifact | Required change |
| --- | --- |
| `epics.md` | Replace the epic inventory, repair ownership and ACs, remove numbered parent containers, add canonical story mapping and correct counts/citations. |
| `architecture.md` | Correct 96/70 totals to 111/77, replace Epic 11 references with TE-1, align runtime ownership, and fix NFR citations. |
| UX package | Add a binding dated conformance addendum and `index.md` manifest covering Epic 12/13 rules and live-route evidence. |
| `sprint-status.yaml` | Migrate canonical epic/story keys, retain legacy aliases in comments/mapping, and move only affected outcomes back to verification. |
| `technical-enablers.md` | New ledger for TE-1, including the EventStore SDK prerequisite and behavior-preservation evidence. |
| PRD and addendum | No requirement-content change. Only manifests/source lists may reference the approved rebaseline. |
| Readiness report | No change; it remains dated evidence. |

### 3.5 Delivery and technical impact

- **Planning/document rebaseline:** estimated 2–4 person-days across product, architecture, UX, and documentation roles.
- **Evidence remap and targeted primary-path re-verification:** estimated 2–5 engineer/test days.
- **Code change:** none assumed. Any failed corrected gate becomes explicit remediation work and is not silently absorbed into this proposal.
- **Risk:** medium. The main risk is losing historical traceability during story-ID migration; the mandatory legacy-to-canonical map mitigates it.

## 4. Options Evaluated

### Option A — Direct rebaseline plus technical-enabler extraction (recommended)

Rebuild the canonical epic map, move minimum runtime into the owning user-value epic, consolidate the UI completion chain, extract Epic 11 to TE-1, and reconcile existing evidence.

- **Benefits:** fixes all 12 findings, preserves delivered code and audit history, restores independent epic value, and creates one observable completion condition per story.
- **Cost:** major planning rewrite and evidence remapping.
- **Risk:** medium; controlled through an explicit migration table and targeted status reset.

### Option B — Roll back later implementation and replay the original sequence

- **Rejected.** The later runtime and UI work is valuable and reportedly stable. Removing it would increase delivery risk without improving the product or the historical planning contract.

### Option C — Reduce MVP scope

- **Rejected.** The readiness failure is decomposition and acceptance integrity, not requirement overload or an unachievable MVP. The PRD remains complete and traceable.

### Recommendation

Approve **Option A**. Treat the change as a major planning correction routed to Product Owner/Product Manager, Architect, UX, Test Architect, and Technical Writer. Developers participate in evidence reconciliation and only implement new code if a corrected gate exposes a real defect.

## 5. Proposed Canonical Epic Map

The rebaseline contains **13 product epics and 111 assignable product stories**, plus one separately tracked technical-enabler workstream.

Count reconciliation:

```text
147 current assignable stories
- 7 Epic 11 technical stories moved outside the product count
- 18 stories removed by consolidating 26 UI-chain stories into 8 surface stories
- 11 stories removed by consolidating 17 control-plane slices into 6 user-value stories
= 111 canonical assignable product stories
```

| Canonical epic | User-visible outcome | Legacy source |
| --- | --- | --- |
| 1. First Safe Governed Action & Command Spine | A user can complete one safe, governed, auditable action through a runnable foundation. | Current Epic 1, with Story 1.16 narrowed. |
| 2. Email Intake, Association & Production Correction | Contributors can ingest, associate, correct, and safely recover email context in production. | Current Epic 2 + current Story 8.6. |
| 3. Project Conversation Context, Files & Attachments | Contributors see governed project conversation, files, provenance, and state. | Current Epic 3. |
| 4. Governed AI Action Mediation | Users can request AI help without bypassing approval, policy, or audit. | Current Epic 4. |
| 5. Cross-Surface Parity — CLI & MCP | Developers and agents can perform the same governed operations through machine surfaces. | Current Epic 5. |
| 6. Outbound Communication & Inbound Authenticity | Authorized users can draft/send safely while preserving sender authority and authenticity evidence. | Current Epic 6. |
| 7. Tenant Policy & Bounded Administration | Tenant, policy, mailbox, and compliance administrators can configure only their authorized scopes. | Current Stories 7.1–7.4. |
| 8. Review Operations, Notifications & Escalation | Reviewers can manage queues and receive bounded, actionable escalation and fatigue signals. | Current Stories 7.5–7.11. |
| 9. Runtime Governance Control Plane | Administrators can actually disable, quarantine, and rate-limit governed subjects at runtime. | Current Stories 7.12–7.26 + 8.7a/8.7b. |
| 10. Command Allowlist & Lifecycle Governance | Policy administrators can govern the command catalog and prove lifecycle isolation. | Current Stories 7.27a/7.27b. |
| 11. Operational Dashboards & Observability | Operators can see health, SLOs, alerts, degradation, and safe recovery guidance after behavior is already functional. | Current Stories 8.1–8.5. |
| 12. Audit, Compliance Investigation & Recovery | Compliance and operations users can reconstruct, retain, export, erase, rebuild, and recover safely. | Current Epic 9. |
| 13. Governed Interactive Workspace & UI Conformance | Users can work through live, accessible, responsive, Fluent/FrontComposer-composed routes with governed chat. | Current Epics 10, 12, and 13 + Stop/Cancel from Story 1.16. |

### Technical Enabler TE-1 — DomainService SDK Host Adoption

Current Epic 11 is removed from the product epic hierarchy. Its work is retained in a separate technical-enabler ledger:

- TE-1.1: accept the host-reuse ADR.
- TE-1.2: deliver the EventStore SDK pre-commit admission hook as a platform prerequisite owned by the EventStore plan.
- TE-1.3–TE-1.5: migrate ChatBot queries, projections/telemetry/health, and the Server host while proving unchanged UI/API/CLI/MCP behavior.
- TE-1.6: **retire module-owned Aspire and ServiceDefaults projects while retaining the ADR-scoped local AppHost shim required for dedicated ChatBot Dapr resources**.
- TE-1.7: initialize AppHost security services through EventStore Aspire helpers.

TE-1.6 has one selected outcome. The former “remove AppHost or retain a shim” alternative is deleted. TE-1 is marked complete only after its behavior-preservation evidence and EventStore prerequisite link are recorded.

## 6. Critical Dependency and Closure Repairs

### 6.1 Stop/Cancel ownership

**Before — Story 1.16:**

> Foundation interaction guardrails include keyboard-reachable streaming Stop/Cancel, although the streaming transport and interaction arrive in Stories 10.6a/10.6b.

**After:**

- Story 1.16 becomes **Interaction guardrails and keyboard safety** and retains banned-interaction rules plus shortcut behavior.
- Stop/Cancel, progressive rendering, generation-target validation, live-region behavior, focus return, reduced motion, and transport evidence move into canonical Story 13.2, **Project Workspace, Conversation, Composer & Streaming**.
- Story 13.2 owns its ADR task and cannot close until the live interactive route proves the complete behavior.

### 6.2 Production correction ownership

**Before — Story 2.8 / Story 8.6:**

> Epic 2 claims correction propagation but defers hosted Dapr Workflow binding, retry/backoff, status, observability, and failure validation to Epic 8.

**After:**

- Story 2.8 owns the correction contract, lifecycle, coordinator, invalidation acknowledgements, and user-visible `correcting`/delayed states.
- New Story 2.9, **Execute correction propagation reliably in production**, absorbs current Story 8.6 and owns hosted workflow binding, bounded retry/backoff, stable operation status, failure recovery, observability required to operate the workflow, and saga-failure validation.
- Current Story 2.9 becomes canonical Story 2.10.
- Canonical Epic 11 retains only cross-product observability, SLO, alert, dashboard, and scale hardening after correction already works.

### 6.3 Runtime governance ownership and Epic 7 split

**Before:**

> Stories 7.12–7.26 record disable/quarantine/rate-limit decisions that remain inert until Stories 8.7a/8.7b.

**After:**

Canonical Epic 9 owns the full functional control plane in six assignable stories:

1. **Durable runtime control foundation** — projection, revocation-sensitive invalidation, admission enforcement, periodic evaluator, and typed/redacted status; current 8.7a/8.7b become tasks here.
2. **Control mailbox sources at runtime** — disable, quarantine, and rate-limit scenarios from current 7.12–7.14.
3. **Control service clients at runtime** — current 7.15–7.17.
4. **Control AI actors at runtime** — current 7.18–7.20.
5. **Control command capabilities at runtime** — current 7.21–7.23.
6. **Control outbound channels at runtime** — current 7.24–7.26.

Each subject story proves policy mutation, actual runtime enforcement, recovery, audit preservation, redaction, and bounded-staleness behavior. No AC names a later epic as the mechanism that makes the control work.

### 6.4 UI closure chain

**Before:**

> Epic 10 closes after shell adoption while raw controls/custom design primitives remain. Epic 12 closes after leaf-control migration while live page composition remains broken. Epic 13 supplies the first trustworthy live-render proof.

**After:**

Current Epics 10, 12, and 13 are superseded as product-planning units and mapped into canonical Epic 13. Their implementation artifacts remain evidence. Canonical Epic 13 is not marked done until all eight corrected stories reconcile their evidence and Story 13.8 passes as a regression confirmation:

| Canonical story | Actor and outcome | Legacy evidence absorbed |
| --- | --- | --- |
| 13.1 Shell, component, layout, and asset foundation | As a product user, I want one stable Fluent/FrontComposer application frame so navigation, headings, controls, and assets render consistently. | 10.1, 12.1, 12.8, 13.1, 13.8, relevant 13.9 asset evidence. |
| 13.2 Project Workspace, Conversation, Composer & Streaming | As a project contributor, I want to converse and interrupt AI work safely inside live project context. | 10.2, 10.4–10.6b, 12.2–12.3, relevant 13.2–13.4/13.7 slices, Stop/Cancel from 1.16. |
| 13.3 Association Review | As a reviewer, I want to resolve ambiguous association from an accessible, redaction-safe live route. | Association slices from 10.2, 12.4, and 13.2–13.4/13.7. |
| 13.4 AI Action Review | As an authorized reviewer, I want to inspect and decide risky AI actions without losing policy, evidence, focus, or disabled reasons. | Approval slices from 10.2, 12.5, and 13.2–13.4/13.7. |
| 13.5 Tenant Administration & Review Operations | As an authorized administrator, I want usable policy, notification, escalation, and queue surfaces that preserve bounded authority. | 10.3, 12.6, and relevant 13.2–13.4/13.7 slices. |
| 13.6 Operational Dashboards | As an operator, I want live health and queue information in a responsive, understandable Fluent presentation. | Dashboard slices from 10.3, 12.7, 13.5, and layout work. |
| 13.7 Compliance Investigation | As a compliance reviewer, I want to search and reconstruct permitted audit evidence from a responsive, redaction-safe live route. | Audit slices from 10.3, 12.7, 13.6, and layout work. |
| 13.8 Live cross-surface release confirmation | As the product-quality owner, I want all affected live routes regression-checked so a release cannot reintroduce a broken shell, control, layout, asset, accessibility, localization, or governed-state contract. | 10.7, 12.9, 13.9. |

Engineering guards, CSS retirement, ADR work, and bulk mechanical migrations become tasks under the user-value story they protect; they are not standalone product stories.

## 7. Acceptance-Contract Repairs

### 7.1 Runnable topology

**Before — Story 1.1c:**

> When `aspire run` is attempted, evidence may record success or blocked prerequisites.

**After:**

> Given the declared root resources and local prerequisites, when the supported local topology starts, every required resource reaches its documented healthy/running state, ChatBot executes one tenant-bound smoke path, and the run records the actual endpoints and resource states. A missing external prerequisite may block only its named lane under a separately approved, time-bounded environmental exception; the story does not become done from an attempted run or diagnostic fallback.

### 7.2 Singular implementation outcomes

- Canonical Story 13.1 requires the temporary token/design-system bridge to be **retired**, not “retired or reconciled.”
- TE-1.6 names the selected retained-AppHost-shim result and aligns its title, ACs, ADR, and completion record.
- Visual/a11y closure requires browser navigation to the live route and direct assertion of load-bearing invariants; hand-authored fixtures remain supporting tests only.

### 7.3 Local acceptance for every UI surface story

Each Story 13.2–13.7 imports the applicable states from `EXPERIENCE.md` and must locally prove:

- successful live route render and the surface's primary user action;
- unauthorized/redacted behavior without existence leakage;
- degraded, retryable, terminal, validation, and empty/loading states where applicable;
- keyboard operation, focus landing/return, reachable disabled reasons, and live-region behavior;
- desktop, tablet, and phone/fallback behavior;
- light, dark, forced-colors, and reduced-motion behavior where applicable;
- English/French parity and text expansion;
- Fluent-only controls, FrontComposer page composition, scoped asset loading, and no custom theme redefinition;
- preservation of governed commands, server-verified state, authorization, audit, and redaction.

Story 13.8 repeats these checks as cross-surface regression confirmation; it is not the first acceptance point for any migrated behavior.

## 8. Exact Artifact Edits After Approval

### 8.1 `epics.md`

**Inventory before:**

```yaml
epicCount: 11
storyCount: 129
```

```text
11 epics across the 3 fixed increments...
```

**Inventory after:**

```yaml
epicCount: 13
storyCount: 111
technicalEnablerCount: 1
updated: 2026-07-17
```

```text
13 independently valuable product epics across M0 → M1 → M2, plus one separately tracked technical-enabler workstream. The product plan contains 111 assignable stories. Legacy story IDs remain available through the approved migration map and are excluded from the canonical count.
```

Additional edits:

- Replace the Epic List and story sections with the canonical map in Sections 5–7.
- Add a complete legacy-to-canonical story migration appendix.
- Convert the numbered non-assignable parents 1.1, 7.27, and 8.7 into unnumbered work-package headings; only assignable children retain story numbers.
- Add actor/outcome statements to every canonical story. Technical guards and cleanup become tasks.
- Correct accessibility references from `NFR6` to `NFR60–NFR64` in the Epic 12/13 legacy notes and canonical UI criteria.
- Retain valid `NFR6` citations where bounded policy/revocation staleness is actually intended; pair user-visible degraded/freshness behavior with `NFR42`/`NFR48` as applicable.
- Refresh input-document manifest, alignment/completion timestamps, and count explanation.

### 8.2 `architecture.md`

**Before:**

```text
Functional Requirements (96 FRs)
Non-Functional Requirements (70 NFRs)
... NFR6 accessibility affordances ...
D8 ... implemented by Epic 11 ... Story 11.2 ...
```

**After:**

```text
Functional Requirements (111 identifiers: FR1–FR96 plus lettered extensions)
Non-Functional Requirements (77 identifiers: NFR1–NFR70 plus lettered extensions)
... accessibility affordances (NFR60–NFR64) ...
D8 ... delivered through Technical Enabler TE-1; TE-1.2 is an EventStore platform prerequisite ...
```

Also update the implementation sequence so correction hosting is owned by Epic 2, runtime control activation by Epic 9, and operational hardening by Epic 11. Keep the existing architectural invariants and delivered implementation notes.

### 8.3 UX package

Create:

- `ux-designs/ux-Hexalith.ChatBot-2026-05-28/index.md`
- `ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md`

The addendum makes the following binding UX rules discoverable from the UX source of truth:

- Fluent UI v5 → FrontComposer → `DESIGN.md` → `EXPERIENCE.md`; no module-owned theme or parallel primitive system.
- Every routable page composes through `FcPageLayout` and `FcPageHeader`.
- Use Fluent layout/data/form components when an equivalent exists; primary data is not a monospace definition-list dump.
- Page-like sibling sections follow the FluentAccordion page-section rule.
- Separate non-vacuous guards enforce Fluent controls, FrontComposer layout composition, and scoped CSS asset loading.
- Each surface migration owns its live route plus negative, responsive, focus, accessibility, localization, and redaction states.
- The final live-render suite confirms, rather than substitutes for, local story acceptance.

The index lists `DESIGN.md`, `EXPERIENCE.md`, both existing elaborations, and the new addendum as the complete UX handoff package.

### 8.4 Sprint and technical-enabler tracking

After approval:

- Preserve legacy IDs in a migration comment/table; do not delete historical story artifacts.
- Mark unaffected canonical outcomes done when their current evidence remains valid.
- Set `epic-1`, `epic-2`, canonical `epic-9`, and canonical `epic-13` to `in-progress` while their corrected evidence is reconciled.
- Set Story 1.1c, new Story 2.9, Stories 9.1–9.6, and Stories 13.1–13.8 to `review` initially.
- Promote a story to done only after its corrected primary-path evidence is linked.
- Resolve current Epic 11's aggregate mismatch by removing it from product sprint tracking and recording TE-1 in `technical-enablers.md`; no implementation artifact is deleted.
- Re-run implementation readiness before release-readiness sign-off. A READY result is required before the corrected aggregate epics return to done.

## 9. Handoff and Success Criteria

### Ownership

- **Product Owner / Product Manager:** approve canonical epic boundaries, legacy mapping, and status reset.
- **Architect:** update ownership/sequencing, requirement totals, D8/TE-1, and singular architecture outcomes.
- **UX Designer / Technical Writer:** publish the UX addendum/index and synchronize traceability.
- **Test Architect:** adjudicate historical evidence, run missing primary paths, and own the readiness rerun.
- **Developer:** assist with evidence mapping and implement only defects exposed by corrected gates.

### Completion criteria

1. `epics.md` contains 13 independently valuable product epics, 111 assignable stories, no numbered non-assignable containers, and no forward dependency required to make an earlier outcome functional.
2. Epic 11 is absent from the product epic hierarchy and TE-1 preserves its complete platform/behavior evidence.
3. The UI outcome is one canonical user-value epic whose surface stories have actor/outcome statements and local live-route acceptance.
4. The canonical counts and all requirement citations match 111 FR / 77 NFR identifiers.
5. UX mechanical conformance rules are discoverable from a UX package index and binding addendum.
6. Affected sprint statuses remain in verification until direct evidence satisfies the corrected criteria.
7. A fresh implementation-readiness assessment returns READY before release-readiness sign-off.

## 10. Approval Result

Jerome approved edits to `epics.md`, `architecture.md`, the UX package, sprint tracking, and technical-enabler tracking on 2026-07-17.

- **Decision:** Approved
- **Approved by:** Jerome
- **Date:** 2026-07-17
- **Authorized action:** Implement the artifact and status changes exactly as proposed; preserve code and historical implementation evidence.

## 11. Implementation Result

- Canonical `epics.md` inventory validates at 13 product epics and 111 assignable stories.
- Canonical story IDs and `sprint-status.yaml` story IDs match exactly; YAML parsing succeeds.
- Affected Epics 1, 2, 9, and 13 and their specified evidence gates are in verification status.
- TE-1 is recorded outside product sprint tracking with all seven historical evidence links.
- Architecture totals, ownership, D8 tracking, UI conformance, and NFR citations are synchronized.
- The UX package has a binding index and dated implementation-conformance addendum.
- Existing code, historical implementation artifacts, retrospectives, and the dated readiness report were not modified or deleted.
- Whitespace/error validation (`git diff --check`) passes for every changed artifact.
