---
title: Historical Story Evidence Crosswalk
project: Hexalith.ChatBot
created: "2026-09-17"
status: complete
sourceProposal: sprint-change-proposal-2026-09-15.md
canonicalEpicSource: epics.md
canonicalEpicCount: 13
canonicalStoryCount: 146
historicalActiveKeyCount: 116
statusCarryForwardCount: 0
---

# Historical Story Evidence Crosswalk

## Purpose and authority

This crosswalk implements Section 4.6 of the approved 2026-09-15 Sprint Change Proposal. It preserves discoverability of the superseded 116-story sprint decomposition without treating its identifiers, files, or statuses as the active backlog.

`epics.md` is the canonical work breakdown. The regenerated `sprint-status.yaml` contains its exact 13 epic keys and 146 parser-derived story keys. Historical identifiers are evidence only. They do not authorize assignment, completion, release, or qualification claims.

## Status decision

No historical status is carried forward. All 146 current stories are `backlog`, all 13 current epics are `backlog`, and all 13 retrospectives are `optional`.

The decision is evidence-based:

- The canonical 146-story hierarchy and the superseded 116-story ledger have zero exact story-key matches.
- No implementation Markdown filename exactly matches a current parser-derived story key.
- No active product story has a current TE-2 evidence contract or report. The only tracked contract/report belongs to TE-2 itself, whose protected-check activation remains open.
- Historical Markdown test summaries are not TE-2 machine evidence; the current policy accepts bound TRX evidence with current provenance.
- Current source and tests still lack the finalized `classifier-indeterminate`, `A11-M1`, and `A11-M2` semantics and retain active `Correction-delayed` usages. Historical completion therefore does not prove current acceptance scope.
- The approved proposal requires unsupported carry-forward to default to `backlog`.

Historical retrospective completion is also not transferred. A retrospective describes the superseded epic execution and cannot imply completion of a newly decomposed current epic.

## Classification vocabulary

| Classification | Meaning | Active-status effect |
| --- | --- | --- |
| Partial evidence | Historical implementation may support part of one or more current stories, but current scope and TE-2 completion are not proven. | None; current stories remain `backlog`. |
| Superseded evidence | The historical item records work or a decision intentionally removed or replaced by the current hierarchy. | None; no active key is created. |
| Unmapped | The artifact is maintenance, process, technical-enabler, retrospective, review, or other evidence without an approved product-story mapping. | None; retain for provenance only. |

There are no `exact carry-forward` classifications.

## Superseded active-ledger crosswalk

Every key covered by a row below is removed from active `development_status`. Candidate scope identifies where a future evidence review should begin; it is not a completion mapping.

| Superseded ledger keys | Historical status distribution | Candidate current scope | Classification and evidence disposition |
| --- | ---: | --- | --- |
| `1-1a-*`, `1-1b-*`, `1-1c-*`, `1-1d-*`, `1-1e-*`, `1-1f-*` | 3 `done`, 2 `in-progress`, 1 `backlog` | Epic 1, principally Story 1.1 | Partial/unmapped. Only the `1-1e` story file exists under its ledger key; the other split keys have no same-key story file. Foundation, topology, build, and dependency evidence requires current-scope review. |
| `1-2-*` through `1-13-*` | 12 `done` | Current Epic 1 | Partial evidence. Contract, gateway, audit, identity, lifecycle, UI-action, conformance, isolation, and evaluation assets cover narrower predecessor scopes. |
| `1-14-*` through `1-21-*` | 8 `done` | Current Epic 13 | Partial evidence. Visual, component, accessibility, responsive, localization, and recovery work predates the current integrated live-route acceptance scopes. |
| `2-1-*` through `2-10-*` | 9 `done`, 1 `in-progress` | Current Epic 2 | Partial evidence. Intake, association, correction, and retry artifacts require comparison against the current 13-story production-correction contract. |
| `3-1-*` through `3-14-*` | 14 `done` | Current Epic 3 | Partial evidence. Conversation, attachment, provenance, and AI-context work is distributed differently across the current 13 stories. |
| `4-1-*` through `4-9-*` | 9 `done` | Current Epic 4 | Partial evidence. The predecessor classifier accepts indeterminate inputs on an approval path, directly conflicting with current Stories 4.4 and 4.5. |
| `5-1-*` through `5-4-*` | 4 `done` | Current Epic 5 | Partial evidence. Identity, CLI, MCP, and parity work does not prove the current ten-story parity and adoption scope. |
| `6-1-*` through `6-5-*` | 5 `done` | Current Epic 6 | Partial evidence. Authenticity, sender authority, drafting, approval, send, reconciliation, and communication-history scope is now decomposed into ten stories. |
| `7-1-*` through `7-4-*` | 4 `done` | Current Epic 7 | Partial evidence. Predecessor admin scopes cover only part of the current bounded-administration safety matrix. |
| `8-1-*` through `8-7-*` | 7 `done` | Current Epic 8 | Partial evidence. Relevant predecessor implementation is also recorded under legacy `7-5-*` through `7-11-*`; no current status is inferred from the renumbering. |
| `9-1-*` through `9-6-*` | 6 `review` | Current Epic 9 | Partial evidence. The ledger rows summarize earlier control work recorded mainly under legacy `7-12-*` through `7-26-*` and `8-7a-*`/`8-7b-*`; the current eleven-story control plane is not proven. |
| `10-1-*` through `10-2-*` | 2 `done` | Current Epic 10 | Partial evidence. Lifecycle and allowlist work recorded under predecessor Story 1.6 and legacy Story 7.27 does not prove the current ten-story scope. |
| `11-1-*` through `11-5-*` | 5 `done` | Current Epic 11 | Partial evidence. The predecessor observability artifacts, primarily legacy `8-1-*` through `8-5-*`, do not contain the finalized A11-M1/A11-M2 split or exact-candidate qualification. |
| `12-1-*` through `12-15-*` | 14 `done`, 1 `review` | Current Epic 12 | Partial evidence. Audit, compliance, replay, data-rights, and recovery artifacts remain useful, but Story 12.15 explicitly retains open activation/current-run recovery evidence. Current Stories 12.14 and 12.15 have different scopes from their predecessor identifiers. |
| `12-16-bind-the-live-hexalith-memories-derived-store-backing` | 1 `backlog` | None | Superseded evidence. Story 12.16 does not exist in the canonical hierarchy. The retained `spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md` is historical and creates no active story. |
| `13-1-*` through `13-8-*` | 1 `done`, 7 `review` | Current Epic 13 | Partial evidence. Shell, chat, association, approval, admin, operations, audit, and conformance artifacts do not prove the current integrated live-route scopes; the nearest Story 13.2 spec remains in review. |

The distributions above total the former 116 active story rows: 97 `done`, 14 `review`, 3 `in-progress`, and 2 `backlog`.

## Historical artifacts outside the former active ledger

The implementation-artifacts directory contains additional numbered stories, specs, reviews, contexts, retrospectives, and test summaries from still earlier decompositions. They remain discoverable in place and are classified as follows:

| Historical collection | Candidate current scope | Classification |
| --- | --- | --- |
| Legacy `7-5-*` through `7-11-*` | Epic 8 | Partial evidence only. |
| Legacy `7-12-*` through `7-26-*`, `8-7a-*`, and `8-7b-*` | Epic 9 | Partial evidence only. |
| Legacy `7-27-*` | Epic 10 | Partial evidence only. |
| Legacy `8-1-*` through `8-5-*` | Epic 11 | Partial evidence only. |
| Legacy `8-6-*` | Epic 2 | Partial evidence only. |
| Legacy `9-1-*` through `9-13-*` and predecessor `12-14-*`/`12-15-*` | Epic 12 | Partial evidence only. |
| Legacy UI Stories `10-*`, `12-*`, and `13-*`, plus associated review/spec files | Epic 13 | Partial evidence only. `spec-12-14-unblock-forced-colors-regression.md` is explicitly superseded. |
| Legacy `11-1-*` through `11-7-*` DomainService host-adoption work | TE-1 | Unmapped to product stories. TE-1 remains complete only in `technical-enablers.md`. |
| TE-2 contract, report, and specification | TE-2 | Unmapped to product stories. TE-2 remains in review only in `technical-enablers.md`. |
| General maintenance specs, SDK/package updates, repository-instruction work, run-all-tests records, deferred-work records, contexts, documentation audits, retrospectives, and review prompts | None | Unmapped; retain for provenance and future targeted review. |

## Action-item disposition

The first six open action items from the superseded ledger remain valid and are preserved unchanged. Their wording is retained because the TE-2 evidence contract binds the first action exactly and the remaining items still describe open evidence or implementation debt.

The former Epic 12 action requiring predecessor Stories 12.14, 12.15, and 12.16 to become `done` is removed from active sprint tracking. It is superseded because Story 12.16 is non-canonical and current Stories 12.14/12.15 have different scopes. This record preserves that disposition without inventing a replacement mapping for the Memories work.

## Reuse rule

When a current story is prepared, its owner may cite the historical clusters above as discovery input. Before raising the status above `backlog`, the owner must create the exact current story artifact, reconcile every current acceptance criterion and requirement link, disclose partial or conflicting predecessor behavior, and satisfy the repository's current evidence policy for the proposed transition. Historical `done` never substitutes for that process.
