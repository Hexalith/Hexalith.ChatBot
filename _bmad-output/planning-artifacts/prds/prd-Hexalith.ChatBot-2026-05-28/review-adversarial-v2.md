# Adversarial Review v2 (Cynical) — Hexalith.ChatBot PRD + Addendum

**Reviewed artifacts:**
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (~1459 lines)
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` (~130 lines)

**Reviewer stance:** Cynical senior reviewer, second pass. Uncynical about the M0/M1/M2 restructure (that was the correct fix). Cynical about everything else, including new attack surface introduced by the rapid rewrite.

**Date:** 2026-05-28

---

## Verdict

The rewrite is a genuine, substantive response — not lipstick on the prior PRD. C1, C2, C3, C5 are closed; C4 is mostly closed with a residual gap; C6 is partially closed by the M0 scope cut; almost every High finding from the prior review has a load-bearing artifact attached to it. The PRD has matured from a maximalist policy manifesto into a buildable spec with a defensible M0 floor. But the rapid editing introduced a small cluster of new contradictions and dangling promises that will bite at story-creation time if not cleaned up before architecture starts: M0 simultaneously requires "reject all candidates" and defers the `Rejected` lifecycle state to M1; the `correcting` state introduced by FR91a is never registered in the canonical state model; FR48's five-class outbound sender-authority taxonomy is delegated to an addendum section that only addresses one of the five classes; the [NOTE FOR PM] approval-fatigue mitigation depends on per-action-class policy ratcheting that the Tenant Policy Schema's single boolean `ai-action.low-risk-allowed` cannot express; the addendum's ID Evolution Contract references "M7" which does not exist; and "Decomposition guidance (M3)" callouts use an "M3" tag that the increment system also doesn't define. None of these are M0 ship-stoppers individually, but they will create real story-time defects.

---

## Closed (from prior review)

### Critical

- **C1 (Minimum slice wider than PRD admits)** — **Closed.** The three-increment M0/M1/M2 sequencing is genuine, with explicit per-increment must-have lists, a stated dependency order, a documented out-of-scope-for-M0 set (CLI, MCP, outbound, multi-tenant, dashboards), and a non-negotiable safety floor that cannot be trimmed under deadline pressure. The M0 must-have list is now plausible against the named team.
- **C2 (Precision/recall targets have no provenance)** — **Closed.** A9a fixes cardinality (≥500 by M0, ≥2000 by M1), label taxonomy (9 named classes), refresh cadence (monthly during pilot, quarterly after), adversarial-example protocol (20 new per cycle from reviewer disagreements), and names the Test Architect as single owner. The 95%/90%/0-critical-FP numbers are explicitly relabeled "calibration targets, not contractual commitments." Honest framing.
- **C3 (T_high/T_low fictional knobs)** — **Closed.** Addendum §Confidence Thresholds specifies score domain `[0.0, 1.0]`, signal weights, M0 defaults (`T_high = 0.90`, `T_low = 0.60`), calibration protocol, security-sensitive change governance, lower-bound guardrails (`T_high ≥ 0.80`, `T_low ≥ 0.50` in M0), and failure-mode (kernel error → NeedsReview, empty candidate list, audited).
- **C4 ("Fail closed" is a token, not a contract)** — **Mostly closed.** NFR15a's path inventory enumerates 10 durable-write paths with explicit fail-closed conditions per path; the "audit writer down" carve-out is removed (now applies to every state-mutating operation, not just security-sensitive ones); the AI-outage escape hatch is closed by making the M0 risk classifier tag-and-heuristic (no AI dependency). Residual gap noted below.
- **C5 (Cross-surface parity is aspiration, not invariant)** — **Closed.** FR81a + addendum §Shared Command Pipeline state the invariant ("adapters MUST NOT replicate any pipeline stage"), make parity follow by construction, and explicitly demote contract tests (FR86) from enforcement to verification. The architecture review veto on bypass adapter designs is explicit.
- **C6 (Single release unbuildable on named team)** — **Partially closed.** M0 is now buildable on the named team (one frontend engineer covers S1+S2+S3, one CLI/MCP engineer deferred to M1, one security engineer's bandwidth concentrated on NFR15a / fail-closed contract). M1 and M2 still load the same singleton engineers heavily (S4-S7 for frontend in M1, parity adapters for CLI/MCP engineer in M1, audit-chain mechanism for security engineer in M2). The dependency-order rule ("don't compress trust controls when team shrinks") is the right escape valve. Not a blocker; revisit at M0 exit.

### High

- **H1 (Magic constants)** — **Closed.** Pilot thresholds, RPO/RTO, and approval-fatigue rate ceilings are now `[ASSUMPTION A11]`-tagged as starter values with a 2–4 week baseline measurement window before M0 release. The cache staleness (NFR6: 5min ordinary, 60s revocation) and p95 latencies remain unanchored but are framed as "default MVP operating baseline" with A4 as the revisit hook.
- **H2 (Tenant policy is universal escape hatch)** — **Closed.** Addendum §Tenant Policy Schema names the M0/M1/M2 knob sets, sensitivity classes, safe defaults, and the closure rule ("tenants cannot define new knobs"). Each PRD reference to "tenant policy" now resolves to a documented knob.
- **H3 (Risk classifier unnamed)** — **Closed.** Addendum §Risk Classifier specifies M0 mechanism (tag-and-heuristic, no AI dependency), input tuple, output set (`low-risk` / `approval-required`), misclassification fallback (indeterminate → `approval-required`), reviewer-disagreement audit chain, and error-rate target (≤1% on eval, ≤2% on production-sampled).
- **H4 (Allowlisted commands is load-bearing, no allowlist)** — **Closed.** M0 allowlist is exactly one command (`Project.AppendConversationMessage`); M1 allowlist is the full catalog minus tenant-tagged disallowed. Both are versioned artifacts under change control.
- **H5 (Evaluation dataset nobody owns)** — **Closed.** A9a names Test Architect as single owner with Product Lead consulting.
- **H6 (Audit completeness undefined)** — **Closed.** NFR50a defines completeness as "fraction of state-mutating operations whose audit chain reconstructs the operation end-to-end," sets target ≥99.5% per rolling 7-day window, distinguishes from NFR50 field-presence, and excludes replay events.
- **H7 (Idempotency keys no contract)** — **Closed.** Addendum §Idempotency Keys gives eight operation classes with key composition, replay window, equivalence rule, and conflict response per class.
- **H8 (Approval fatigue hand-waved)** — **Partially closed.** NFR46 now has concrete mechanisms (prioritization formula, grouping criteria, rate ceiling 8/hour, 30/day, ≥25 open items triggers admin alert, rubber-stamp rate >15% as the observable). BUT the [NOTE FOR PM] mitigation depends on per-action-class policy ratcheting that the schema cannot express — see new finding N3.
- **H9 (Rejected/Failed "terminal unless reprocessed")** — **Closed.** Both states are now marked unconditionally terminal; reprocessing creates a new workflow instance with a new ID, linked via `superseded_by_workflow` / `supersedes_workflow` audit edges.
- **H10 (WCAG on one frontend engineer)** — **Closed.** NFR60 now scopes WCAG conformance per-increment to the 3/4/3 enumerated surfaces; CLI/MCP explicitly outside scope. M0 floor (3 surfaces) is plausible.
- **H11 (No admin/debug bypass vs. required admin dashboards)** — **Closed.** FR75a–FR75g name the admin permission model: see-only vs. operate scopes, sub-roles (mailbox-admin, policy-admin, compliance-admin, operations-admin), two-person rule for security-sensitive knobs, audit obligation on every admin action including read-only above an aggregation threshold.

### Medium / Low

- **M1 (Source-context date pin)** — **Closed** (material-change re-check protocol added, Architect owns trigger, 5-business-day SLA, decision-log entry).
- **M2 (Derived state ownership)** — **Closed** (§Data Governance Surface lists 13 record classes with source, retention, redaction, isolation, and owning increment).
- **M3 (Capability at noun level)** — **Partially closed.** FR23, FR26, FR27, FR42, FR67, FR76, FR77 now carry "Accept when" bullets; FR22 and FR74 carry decomposition guidance. FR55, FR67 in its broader form, FR55–FR63 group, and several others still read at the noun level. Acceptable given the FR group acceptance-matrix at Functional Acceptance Guidance.
- **M4 (Cross-tenant cache pollution)** — **Closed** for M2 (FR55a + NFR9a + nightly isolation probe). See new finding N5 about M0/M1 gap.
- **M5 (Sender authority undefined)** — **Partially closed.** See new finding N2 — the addendum section the PRD points to only addresses 1 of the 5 outbound-authority classes.
- **M6 (Visible failure states)** — **Closed.** FR67 + FR76 + FR77 + NFR40 + NFR44 collectively define the visible-state UX contract (queue/health name, status enum, owner role, message catalog with stable codes, runbook-ready diagnostic sample).
- **M7 (ID evolution contract)** — **Partially closed.** Addendum §ID Evolution Contract exists. See new finding N1 — the PRD body never references it (the addendum's "Referenced from M7" tag points to nothing).
- **M8 (Replay/simulation)** — **Closed** (FR95a + addendum §Replay Isolation: dedicated test tenant, outbound interception, replay_run_id in audit, nightly probe gates M2).
- **M9 (Correction-path AI invalidation cost hidden)** — **Closed** (FR91a + NFR17a: per-store invalidation, `correcting` user-facing state, p95 ≤ 10 min M0/M1 / ≤ 60 min M2 SLO, P2 incident if breached). See new finding N4 — `correcting` is not in the canonical state model.
- **M10 (Phishing/spoofing)** — **Closed** (FR48a–FR48d + addendum §Inbound Message Authenticity).
- **M11 ("MVP must define" deflections)** — **Closed** (replaced with concrete FR text or Open Assumptions table entries).
- **M12 (Eight journeys, no UI inventory)** — **Closed** (§UI Surface Inventory enumerates S1–S10 with per-increment scoping).
- **L1–L9** — All **closed** (NOTE FOR PM for the ChatBot naming mismatch; Aspire defined in glossary; FrontComposer added to integration list; tamper-evident mechanism specified in NFR49a; glossary expanded with fail closed / low-risk / approval-required / policy snapshot / operating baseline / evaluation dataset / MVP parity set / MCP; canonical Context Ownership section nominated; competitive-claim disclaimer added).

---

## New findings introduced by the rewrite

### Critical

None. The rewrite did not introduce new shipping-blockers.

### High

**N1. The addendum's ID Evolution Contract references "M7" — there is no M7.**
**Section:** `addendum.md` §ID Evolution Contract (line 112: "Referenced from M7 and §Increment M1 / M2")

The addendum carries a fully-specified ID Evolution Contract (IdentityEvolved event, ProjectionIdentityMigration record, query-time reconstruction, split/deprecation handling). The PRD body never references it. The "M7" tag is a stale reference to the prior adversarial review's M7 finding label, not to the PRD's increment system (which has M0/M1/M2 only). Without an FR or NFR in the PRD body that requires the ID Evolution Contract, architecture has no obligation to build it. This is a real promise that vanished between the addendum and the FR catalog. Fix: add an FR (FR59a or FR91b) that points to the addendum, or delete the addendum section.

**N2. FR48's five-class sender-authority taxonomy is delegated to an addendum section that only addresses one class.**
**Section:** PRD FR48 (line 1223); `addendum.md` §Inbound Message Authenticity (lines 122–129)

FR48 says: "The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Inbound Message Authenticity." That section addresses on-behalf-of disambiguation and external-sender posture for inbound, plus one outbound rule (delegated send → delegate identity). It does NOT define the mapping for the other four outbound classes: `draft-only`, `authenticated-user send`, `shared-mailbox send`, `approved service-send`. The PRD says the addendum has the mapping; the addendum doesn't. Fix: extend addendum §Inbound Message Authenticity (or add a separate §Outbound Sender Authority section) with the five-class mapping rule and the conflict cases. M5 from the prior review is only partially closed.

**N3. The approval-fatigue [NOTE FOR PM] mitigation depends on policy structure the schema cannot express.**
**Section:** PRD line 1212 ([NOTE FOR PM] on FR41); `addendum.md` §Tenant Policy Schema line 67

The note prescribes: "ratchet `tenant-policy.ai-action.low-risk-allowed` to `true` for the action classes whose review consistently approves without revision." The Tenant Policy Schema's `ai-action.low-risk-allowed` is a single boolean (`default: false`). There is no per-action-class structure. So the mitigation move the PRD names cannot be performed within the schema the PRD ratifies. Either the schema needs a per-action-class structure, or the note's mitigation language needs to change. Fix: extend the M1 knob set in the addendum to `ai-action.low-risk-allowed-classes` (a set of action classes, default empty), and update the note to reference it.

**N4. The `correcting` lifecycle state introduced by FR91a is not in the canonical state model.**
**Section:** PRD FR91a (line 1301); §Shared Workflow Contract canonical state table (lines 430–438); FR87 (line 1296)

FR91a introduces `correcting` as the user-facing state during derived-store invalidation. NFR17a adds `correction-delayed` as the SLO-breach state. Neither state appears in the canonical state table (Received, Proposed, Associated, Rejected, Deferred, NeedsReview, Failed, Skipped, Corrected) or in the FR87 canonical lifecycle definition. The state model FR87/FR88 requires the system to validate transitions against an explicit state model; this state is invoked without being in the model. Fix: add `correcting` and `correction-delayed` to the canonical state table (between `Associated` and `Corrected` makes the most sense) or describe them as decorations on `Associated` / `Corrected` that don't extend the state set.

**N5. M0 "reject all candidates" is required but `Rejected` lifecycle state is M1.**
**Section:** PRD M0 increment (line 237); FR6 (line 1147); MVP scope bullets (lines 150, 199); Journey 2 (line 331); Increment M0 ambiguous-association review (line 233)

M0 increment says: "M0 lifecycle states: Received, Proposed, Associated, NeedsReview, Deferred, Failed, Corrected. (Rejected and Skipped land in M1.)" Then:
- FR6 (no increment guard): "Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review..."
- §Measurable Outcomes (line 150): "Users can choose a candidate project, reject all candidates, or defer..."
- §MVP scope bullets (line 199): "Allow the user to select a candidate project, reject all candidates, or defer association."
- §Increment M0 (line 233): "candidate projects with ranked evidence; user can confirm, reject, defer, or correct."
- Journey 2 / UJ2 (an M0 journey via S2): Marc's value moment includes "reject all candidates."

If `Rejected` doesn't exist in M0, the user action "reject all candidates" has no terminal state to land in. The most likely intent is that M0 users CAN reject but the state collapses to `NeedsReview` or `Deferred`, deferring real rejection terminology to M1. The PRD doesn't say that. Fix: either (a) move `Rejected` to M0, or (b) explicitly state in M0 that the "reject all candidates" affordance writes `NeedsReview` and the `Rejected` terminal state arrives in M1 with full-lifecycle semantics, and update FR6 and Journey 2 to match.

### Medium

**N6. "Decomposition guidance (M3)" uses an "M3" tag the increment system doesn't define.**
**Section:** PRD line 1170 (FR22 decomposition), line 1262 (FR74 decomposition)

The decomposition-guidance bullets say "(M3)" — the PRD's increments are M0/M1/M2 only. This is likely shorthand for "milestone-3 sub-stories" or "decomposition mode 3" but it reads as a fourth increment. Fix: rename to something unambiguous like "Story decomposition" or "Decomposition guidance" without the (M3) tag, or define M3 explicitly if a fourth increment is intended.

**N7. M0 derived stores exist but the structural cross-tenant isolation invariant (NFR9a/FR55a) is M2.**
**Section:** Data Governance Surface (lines 490–504); FR55a (line 1238); NFR9a (line 1344)

§Data Governance Surface lists 8 derived-store record classes owned by ChatBot starting in M0 (association record, candidate ranking, evidence snapshot, AI action proposal, approval record, projection, lifecycle state, workflow instance map). NFR9a and FR55a — "tenant isolation by construction at the store level, not the application level, verified by nightly cross-tenant probe" — only ship in M2. So for M0 and M1, the cross-tenant isolation invariant is whatever the application layer enforces, with NFR11's zero-tolerance test promise as the only check.

The PRD's non-negotiable safety floor names tenant isolation as non-trimmable, but the structural enforcement of that floor for derived stores arrives in M2. This is workable — architecture can deliver store-level partitioning earlier than NFR9a's M2 verification probe requires — but the PRD doesn't say so. Fix: add an NFR or M0 must-have stating that store-level tenant partitioning is in scope from M0 even though the nightly probe and the formal NFR9a contract land in M2.

**N8. Addendum's `Project.AppendConversationMessage` is described as "read-only side effects" but writes durable state.**
**Section:** `addendum.md` §Command Allowlist v0 (line 37)

The M0 allowlist command is described as "Read-only side effects on Hexalith.Conversations; no outbound communication; no file mutation; no task creation; no external tool invocation." Appending a conversation message is a write — it produces a durable record in Hexalith.Conversations. The risk classifier (addendum §Risk Classifier line 27) classifies "writes project state" as `approval-required`. So either the M0 command is approval-required (consistent with M0's stated approval-required default per the [NOTE FOR PM] at line 1212) or the addendum wording is wrong. Most likely the wording is sloppy: the command produces no outbound / file / task / tool side effect, but it does write to Conversations. Fix: rephrase to "non-outbound, non-file-mutating, non-task-creating; writes one conversation-message record."

**N9. NFR42a references `addendum.md` §Operating Baselines which doesn't exist.**
**Section:** PRD NFR42a (line 1407)

NFR42a says SLOs "must be published in the per-tenant operational view (M2) and in `addendum.md` §Operating Baselines (created during M2)." The addendum has no §Operating Baselines section. The parenthetical "(created during M2)" is honest about the deferral, but a reader checking the addendum for SLO targets finds nothing. Fix: either create a placeholder §Operating Baselines section in the addendum that says "populated during M2" (defensible — same pattern as A10's RPO/RTO calibration), or remove the addendum reference from NFR42a until M2.

**N10. M0 admin operations include security-sensitive knob changes but the two-person rule lands in M1.**
**Section:** PRD FR75d (line 1272); M0 knob set in addendum (lines 63–68); FR9 (line 1150)

M0 knob set includes `association.t-high` and `association.t-low` and `ai-action.low-risk-allowed` — all marked "security-sensitive" in the schema. FR9 ratifies tenant admin authority to change them in M0 with audit. But FR75d's two-person rule for security-sensitive knob mutations is M1. In M0, a single admin can change `T_high` to `0.80` or `low-risk-allowed` to `true` with an audit event and no second-admin gate. This is a known M0 → M1 ratchet, not a contradiction, but the PRD doesn't acknowledge it. Fix: add a one-line note to FR9 or the M0 increment narrative stating that M0 security-sensitive knob changes audit but do not require two-person approval; the two-person rule lands in M1 with FR75d.

### Low

**Count: 3.** (Sender-authority taxonomy is invoked in NFR15a path inventory before being fully defined — addendum repair fixes both; Risk Classifier output set of `{low-risk, approval-required}` does not enumerate the route from `Denied` / `Unsupported` requests when those originate from authorized users with disallowed commands; the M1 NOTE FOR PM tuning rule references "the action classes whose review consistently approves without revision" without defining how that signal is measured beyond the rubber-stamp telemetry — implies the same per-action-class structure that N3 already flags.)

---

## Counts

| Severity | Count |
|---|---|
| Critical | 0 |
| High | 5 |
| Medium | 5 |
| Low | 3 |
| **Total** | **13** |

Down from the prior review's 6 Critical, 11 High, 12 Medium, 9 Low (38 total).

---

## Shippability

**M0 is shippable as specified**, assuming the new High findings are addressed before story creation:

- N1 (ID Evolution Contract) — fix or delete the orphan addendum section.
- N2 (sender-authority five-class mapping) — extend addendum.
- N3 (approval-fatigue mitigation vs. boolean knob) — extend schema for M1; not M0-blocking but the [NOTE FOR PM] should be edited to acknowledge the gap.
- N4 (`correcting` not in canonical state model) — add to the canonical state table.
- N5 (M0 reject-all vs. M1 `Rejected`) — decide and document.

What would still break in pilot if shipped as-is:

- Story authors will invent a `correcting` state semantics that may not match FR91a (N4).
- Pilot users will hit the "reject all candidates" affordance and trigger an undefined transition (N5).
- A tenant admin trying to reduce approval load per the [NOTE FOR PM] guidance will find no per-action-class knob (N3).
- Auditors investigating a renamed/split sibling-context ID will hit a gap in the M1/M2 audit traceback because no FR requires the ID Evolution Contract (N1).
- Architecture review will accept an outbound-adapter design that maps four of five sender-authority classes from M365 to ChatBot with no canonical rule because the canonical rule was promised but not delivered (N2).

These are each a half-day's fix in the PRD/addendum, not a redesign. The rewrite is sound; it just needs a final pass to tighten the seams it introduced.

---

## Halt conditions

None met — content is non-empty, findings are substantive, and the verdict is defensible against both the prior review and the current artifacts.
