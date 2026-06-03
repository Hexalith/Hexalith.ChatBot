# Epic 9 Documentation Update Audit

**Project:** Chatbot
**Epic:** 9 - Tamper-Evident Audit, Compliance Investigation & Recovery
**Date:** 2026-06-03
**Companion to:** `epic-9-retro-2026-06-03.md`
**Method:** For each candidate doc, the current content was read, compared against the actual Epic 9 implementation code, and updated only where a discrepancy was verified. Candidates where code matched the doc were discarded with the verification noted.

## Candidate List (from implementation learnings)

Epic 9's story records surfaced these implementation learnings with potential doc impact:

1. The project README's overview and runtime caveats stopped at Epic 8 (Epic 8 retro AI#6 asked to keep implementation-claim docs synchronized).
2. Story 9.3 added new public HTTP endpoints and noted the generated OpenAPI client lagged them ("new endpoints post-date the generated OpenAPI client; hand-written `ComplianceAuditTransport`").
3. Story 9.3 widened the audit `filterKey` set with `message-id` and `surface` "in lock-step across the enum and the predicate."
4. Architecture decisions (WORM two-phase audit, completeness = reconstructability, derived-store isolation by construction, RPO/RTO assumptions, replay isolation, cross-cutting #11/#13) — verify the architecture doc still matches what shipped.
5. Fourteen new ADRs were created under `docs/adrs/` during Epic 9 — verify they describe the shipped design.
6. Config documentation for new audit/retention/consent settings.

## Verification Results & Decisions

### 1. `README.md` — **UPDATE REQUIRED AND APPLIED** ✅

- **Verified discrepancy:** the project-overview paragraph ended at Epic 8 and carried no Epic 9 summary, and there was no Epic 9 runtime caveat. Both are implementation-claim gaps (the same class Epic 8 AI#6 flagged).
- **Verified discrepancy:** the README's standing claim (carried from the Epic 7 note) that admin/observability layers "add no new public HTTP paths" no longer holds for Epic 9 — Story 9.3 added real HTTP paths (see item 2).
- **Applied:**
  - Added an Epic 9 summary sentence to the overview paragraph (WORM audit, reconstructability completeness, S9 surface, replay/derived-store isolation, reindexing, retention/export/deletion/consent, recovery validation harnesses, with NFR/FR references).
  - Added an Epic 9 runtime/contract caveat paragraph: the build-the-logic / defer-the-runtime pattern repeats (nightly verifier, completeness measurer, isolation probes, SLO sweep, recovery drills all lack a runtime trigger; live drivers deferred; A10 RPO/RTO targets un-recalibrated), and Epic 9 *did* add new public HTTP paths now reflected in the OpenAPI yaml, with the client regeneration noted as the retro follow-up.

### 2. `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` — **UPDATE REQUIRED AND APPLIED** ✅

This is the contract-first API documentation and it had verified divergences from the Story 9.3 server implementation.

- **Verified discrepancy (paths):** `Program.cs` maps `POST /api/v1/compliance/audit/search` (line 428) and `GET /api/v1/compliance/audit/{auditRecordRef}` (line 475), and a client transport exists (`src/Hexalith.ChatBot.Client/Generated/ComplianceAuditTransport.cs`), but **neither path existed in the OpenAPI `paths` section** (the yaml was last modified in Story 7.26 and not touched in Epic 9). The supporting component schemas (`ComplianceAuditQueryFilters`, `ComplianceAuditSearchResult`, `ComplianceAuditResultRow`, `ComplianceAuditDetail`, `ComplianceAuditRedactionState`, `ComplianceEscalationStatus`) were already present from the Story 7.4 forward-scaffold and were verified to match the server wire models (`ComplianceAuditHttpResults`) field-for-field, including the kebab-token redaction/escalation enums.
  - **Applied:** added the two `paths` entries (referencing only the already-present, verified-matching schemas and the shared `CorrelationId`/`TaskId` parameters and `SafeAuthorizationDenial401/403`/`InternalFailure` responses), plus a `compliance` tag to the top-level tag list. The error responses are 401/403/500 only — no 400 — because invalid queries collapse to a safe authorization denial by design (NFR2), matching the handler.
- **Verified discrepancy (filter keys):** `ComplianceAdministrationContracts.AuditFilterKeys` (lines 193–214) includes `message-id` and `surface` (added by Story 9.3, FR56), and `ComplianceAuditReadPolicy.MatchesFilter` handles both — but the yaml's `ComplianceAuditFilterRef.filterKey` enum listed neither.
  - **Applied:** added `message-id` and `surface` to the `filterKey` enum in code order (before `time`).
- **Validation:** the edited yaml was re-parsed (valid OpenAPI 3.1.0); both new paths and both new filter-key tokens are present.
- **Note (out of Epic 9 scope, not applied):** `GET /api/v1/governed-operations/{noteId}` (Program.cs line 384) is also absent from the OpenAPI yaml, but `git log -S` shows it was introduced in **Story 1.9**, not Epic 9. It is a pre-existing divergence unrelated to Epic 9 learnings and is flagged here for a separate follow-up rather than fixed in this Epic 9 audit. (Fixing it would require authoring the `GovernedOperationViewResponse` schema, which is out of this pass's verified scope.)

### 3. `_bmad-output/planning-artifacts/architecture.md` — **VERIFIED, NO UPDATE NEEDED** ✓

Checked the Epic 9-relevant claims against implementation:

- Two-phase audit (D4): pre-commit fail-closed gate + post-commit WORM fail-open-then-reconcile (lines 143–146, 299, 370–371, 525) — matches Story 9.1 (`audit-two-phase.md` ADR + code).
- Completeness = reconstructability, not field presence (lines 146, 371–372) — matches Story 9.2 (`AuditOperationReconstructor`).
- WORM-vs-erasure via key-destruction with redaction key in a separate KMS (cross-cutting #13, lines 167, 392–393) — matches Story 9.1/9.9.
- Derived-state versioning & deterministic replay (cross-cutting #11, lines 156–160, 331–332) — matches Story 9.12 (schema-version match + as-of resolution).
- Tenant isolation by construction incl. derived stores/caches/vector indexes (line 342) — matches Story 9.5 physical partitioning.
- Replay/simulation against an isolated test tenant (FR95a, lines 62, 401–402) — matches Story 9.4.
- RPO ≤ 15 min / RTO ≤ 4 hr "(assumption pending M2 drill)" (lines 85, 401–402) — **still accurate**: the drill harness is built (9.11) but the live driver is deferred, so the targets remain un-recalibrated assumptions. The doc's hedge is correct, not stale.
- "Deferred (post-M0, mostly M2)" listing vector isolation (NFR9a) and replay isolation (FR95a) (line 306) — these are design-intent statements in a planning artifact; the capabilities are now built-but-runtime-deferred, which the doc's M2 framing still fairly represents. No implementation-status claim is contradicted.

**Decision:** discard — architecture.md describes intent and matches the shipped design; no false implementation-status claim.

### 4. `docs/adrs/*.md` (14 ADRs) — **VERIFIED, NO UPDATE NEEDED** ✓

These ADRs were authored during Epic 9 (file timestamps 2026-06-03) by the same stories whose code they describe, and each was cross-checked by the per-story review. Spot-verified that the decisions (WORM backing, completeness-observable, replay isolation, derived-store partition, reindexing, retention/export/deletion/consent, recovery validation) correspond to the shipped types and seams. They are fresh provenance, not stale claims.

**Decision:** discard — no update needed.

### 5. PRD / `epics.md` — **VERIFIED, NO UPDATE NEEDED** ✓

These are requirements documents that specify intended behavior; they do not assert implementation status. The Epic 9 story acceptance criteria in `epics.md` (§Epic 9, stories 9.1–9.13) match what shipped.

**Decision:** discard.

### 6. Configuration documentation — **VERIFIED, NO UPDATE NEEDED** ✓

No dedicated configuration-doc file exists for ChatBot beyond the README's Aspire/DAPR section, which describes component names (`statestore`, `chatbot-statestore`, `chatbot-pubsub`) and the access-control posture. Epic 9 introduced no new runtime component or config knob that the README's Aspire section misstates (the new audit/retention/consent behavior rides existing stores; the live drivers and schedulers that *would* need configuration are deferred and explicitly not yet active).

**Decision:** discard — no config-doc discrepancy.

## Summary

| Doc | Decision | Reason |
|-----|----------|--------|
| `README.md` | **Updated** | Overview stopped at Epic 8; no Epic 9 summary or runtime caveat; stale "no new HTTP paths" implication |
| `openapi/hexalith.chatbot.v1.yaml` | **Updated** | Missing the two Story 9.3 paths and the `message-id`/`surface` filter keys; schemas already present and verified-matching |
| `architecture.md` | Discarded | Matches shipped design; "pending M2 drill" hedge still accurate |
| `docs/adrs/*` (14) | Discarded | Fresh, story-authored, review-checked provenance |
| PRD / `epics.md` | Discarded | Requirements docs, no implementation-status claims |
| Config docs | Discarded | No discrepancy; new runtime/config is deferred, not active |

**Pre-existing divergence flagged for separate follow-up (not Epic 9):** `GET /api/v1/governed-operations/{noteId}` (Story 1.9) is absent from the OpenAPI yaml.

**Two docs updated, four candidate areas verified-and-discarded.** All updates reference verified, implementation-matching content; the OpenAPI yaml was re-parsed clean after editing.
