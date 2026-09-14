---
title: Source Manifest Reconciliation — Hexalith.ChatBot Architecture Spine
date: '2026-09-14'
reviewedArtifact: ../ARCHITECTURE-SPINE.md
authority: ../../../prds/prd-Hexalith.ChatBot-2026-05-28/source-manifest.md
reviewStage: post-remediation
result: pass
blockingFindings: 0
advisoryFindings: 0
---

# Source Manifest Reconciliation

## Post-remediation verdict

This review compares only the current `ARCHITECTURE-SPINE.md` (445 lines) with `source-manifest.md` (79 lines).
Both were read completely. Citations use `Spine:Lx` and `Manifest:Lx` for those reviewed versions.

**PASS — zero blocking findings and zero advisory findings.** Every initial B-1–B-4 and A-1–A-4 remediation is
present. The current candidate preserves the source date/baseline, the single-current-result supersession rule, all
nine ownership boundaries, producer-versus-consumer acceptance, recheck triggers, and recovery supplement status.
No new source-manifest conflict was found.

A5, A6, A10, A11, and A13 remain open. This pass validates architecture-to-manifest consistency only; it supplies
no implementation, qualification, activation, or release-readiness evidence.

## Current alignment evidence

### Source, supersession, and recheck posture

- The spine binds the manifest's exact repository-relative inputs and nine-context baseline at workspace revision
  `76f355a038c4abdb3b9fdb3fb836c25053a18fb0`, retaining `material-gaps-recorded` (`Spine:L60-L64`;
  `Manifest:L6-L13`).
- The current full-sibling/A13 result is the sole gate-status authority; it supersedes only the initial five-gap
  extract, incorporates H4/H12, and cannot be replaced by narrow or historical evidence (`Spine:L65-L68`;
  `Manifest:L71`).
- Compatibility, a revision/hash match, typed transport, interface, or `[ADOPTED]` target does not prove producer
  acceptance, execution, qualification, or readiness; repository revision cannot close A6/A12/A13
  (`Spine:L69-L71`; `Manifest:L41`, `Manifest:L45`, `Manifest:L71`).
- AD-14 preserves the five-business-day deadline and all manifest triggers—consumed schema,
  authorization/identifier semantics, topology, and RBAC—plus memlog/manifest update and blocker behavior
  (`Spine:L291-L301`; `Manifest:L79`).
- `IdentityEvolved` remains unbound pending every producer's accepted versioned reconciliation contract, and history
  is never rewritten (`Spine:L298-L301`, `Spine:L442`; `Manifest:L41`).

### Nine bounded-context ownership and acceptance

| Context | Post-remediation spine contract | Manifest alignment |
| --- | --- | --- |
| Conversations | Owns identity, messages, append/history, and sole conversation-to-Project assignment/reassignment (`Spine:L144-L146`). The transport mapping remains unusable until production handler, exact v1 mapping, caller revision/equivalent, lifetime concurrency, atomic audit, assignment, and owner tests pass A13 (`Spine:L187-L191`). Cross-context effects use owner-command choreography only after owner acceptance (`Spine:L303-L314`). | Matches conversation/message ownership and sole assignment (`Manifest:L31`, `Manifest:L49-L55`). |
| Projects | Owns Project identity, lifecycle, membership, access, and authorization (`Spine:L144`). Its ChatBot resource-grant mapping remains owner-acceptance/test gated (`Spine:L103-L111`). | Matches Project identity/membership/boundary and A13 owner acceptance (`Manifest:L32`, `Manifest:L56`). |
| Folders | Owns governed files, metadata, and access (`Spine:L146`); stable owner facts cannot become local authority (`Spine:L152-L153`). Generated-client churn cannot supersede the pinned OpenAPI, and IDs stay opaque/tenant-scoped (`Spine:L72-L74`). | Matches the reviewed OpenAPI and generated-client qualification boundary (`Manifest:L33`, `Manifest:L41`, `Manifest:L57`). |
| Parties | Owns identity and external participants (`Spine:L145-L146`); trust-bearing operations use current Parties gateway evidence and never display mirrors as authority (`Spine:L108-L111`). A6 remains open for production custody/runtime evidence (`Spine:L431`). | Matches Parties ownership, current-authorization requirement, and A6 blocker (`Manifest:L34`, `Manifest:L58-L59`). |
| Tenants | Owns tenant facts, boundaries, membership, policy context, and native roles; ChatBot separately owns its application permission vocabulary (`Spine:L146-L151`). Deny-by-default mappings remain A13-pending (`Spine:L108-L111`). | Matches tenant-policy/role ownership and pending mapping acceptance (`Manifest:L35`, `Manifest:L60-L61`). |
| EventStore | Owns aggregate-local durability and target canonical-envelope chaining, protection, and erasure seams; ChatBot owns domain audit facts and investigation views (`Spine:L147-L151`). AD-2 labels the atomic path a target, prohibits projection-outbox/claims fallback, and records the current conditional batch support plus all owner-unaccepted gaps (`Spine:L84-L97`). AD-10 prohibits completeness/tamper claims while A13 remains open (`Spine:L218-L233`). | Matches EventStore ownership and current compatibility limits (`Manifest:L36`, `Manifest:L62-L67`). |
| FrontComposer | Owns shell/progress transport while ChatBot owns governed-composer lifecycle/authorization (`Spine:L148-L149`); AD-16 uses the scoped `ProjectionChangedDetail` contract as advisory metadata followed by authorized re-query (`Spine:L316-L331`). | Matches the shell/interactive-surface ownership split and compatible transport (`Manifest:L37`, `Manifest:L68`). |
| Memories | Remains an optional M2 provider whose activation is deferred behind isolation and A5/A6 qualification (`Spine:L149`, `Spine:L443-L444`). | Matches optional dependency status (`Manifest:L38`). |
| Commons | Supplies mechanisms only and cannot broaden ChatBot roles/permissions; ChatBot owns the application vocabulary (`Spine:L149-L151`). | Matches shared-mechanism compatibility and ChatBot vocabulary ownership (`Manifest:L39`, `Manifest:L69`). |

### Recovery supplement and gate states

- The Epic 12 supplement is accepted only at the manifest-pinned snapshot/hash, remains `activation: pending`, and a
  changed hash triggers recheck (`Spine:L72-L74`; `Manifest:L20`).
- AD-11 preserves the disjoint diagnostic, story-completion, and A10 authority channels; cleanup receipt, fixed
  deadlines, immutable tools, isolated destructive execution, and independent proof remain activation preconditions
  (`Spine:L235-L245`; `Manifest:L75`). A10 still requires a separate fresh controlled-loss/RTO-capable bundle
  (`Spine:L246-L254`; `Manifest:L75`).
- The release ledger is explicitly an open-assumption ledger, not the complete release gate, and records the evidence
  artifact as `evidence-gap` (`Spine:L422-L426`). Its states remain A5 `OPEN`, A6 `OPEN`, A13 `OPEN`, A10
  `OPEN / provisional`, and A11 `OPEN / unsupported` (`Spine:L428-L434`).
- A8 governs allowlist membership; executable producer, authority, audit, concurrency, and fencing acceptance remain
  A13 (`Spine:L65-L68`, `Spine:L183-L191`; `Manifest:L71`).

## Concise remediation history

The following findings were recorded against the earlier 240-line draft. They are retained as history and are no
longer active.

| Initial finding | Disposition in current candidate |
| --- | --- |
| **B-1:** EventStore atomic target presented as current support. | **Resolved.** Diagram says `A13-gated ... target`; AD-2 records conditional current support, owner-unaccepted capabilities, and prohibited outbox/fallback (`Spine:L49`, `Spine:L84-L97`). |
| **B-2:** Projects/Parties/Tenants current-owner authorization boundaries incomplete. | **Resolved.** AD-3 and AD-5 name Project membership/grant acceptance, Parties live gateway authority/external participants, and the Tenants-native-role versus ChatBot-permission split (`Spine:L99-L119`, `Spine:L140-L153`). |
| **B-3:** Conversations producer/consumer acceptance boundary under-specified. | **Resolved.** AD-7 records transport-only status and every A13 prerequisite; AD-15 fixes owner-command choreography (`Spine:L183-L191`, `Spine:L303-L314`). |
| **B-4:** Current reconciliation supersession/acceptance semantics missing. | **Resolved.** `Source & Acceptance Baseline` records the sole current result, supersession scope, H4/H12 incorporation, compatibility boundary, and A8/A13 split (`Spine:L60-L74`). |
| **A-1:** Recovery supplement provenance/activation pin incomplete. | **Resolved.** Manifest-pinned snapshot/hash, pending activation, and changed-hash recheck are explicit (`Spine:L72-L74`). |
| **A-2:** Folders generated-client versus OpenAPI authority imprecise. | **Resolved.** Pinned OpenAPI controls and IDs remain opaque/tenant-scoped (`Spine:L73-L74`). |
| **A-3:** FrontComposer/ChatBot ownership split incomplete. | **Resolved.** FrontComposer owns shell/transport; ChatBot owns lifecycle/authorization (`Spine:L148-L149`). |
| **A-4:** Commons mechanism versus ChatBot vocabulary ownership implicit. | **Resolved.** Both sides are explicit (`Spine:L149-L151`). |

## Final disposition

**Accepted for the source-manifest reconciliation gate.** Remaining blockers: none. Remaining advisories: none. No
new blocker exists. A5, A6, A10, A11, and A13 remain open exactly as recorded; this reconciliation must not be cited
to close them.
