---
title: Contract Verification — Recommended PRD Update — 2026-09-14
status: complete
created: "2026-09-14"
reviewer: adversarial contract verifier
inputs:
  - reconcile-recommended-update-2026-09-14.md
  - reconcile-recommended-edit-map-2026-09-14.md
targets:
  - prd.md
  - addendum.md
---

# Contract Verification — Recommended PRD Update

## Gate verdict

**STOP.** The current draft closes 12 of the 15 recommended Critical/High contracts, preserves the stable FR/NFR identifiers, and materially improves the authority and gate model. One Critical correction-contract gap and two High governance/authenticity gaps remain. The document should not return to `final` until those three items are reconciled and re-reviewed.

The open evidence gates A5, A6, A10, A11-M1, A11-M2, and A13 are correctly left open; this review does not treat specification completeness as evidence closure.

## Fifteen-item verdict matrix

| ID | Verdict | Verification evidence | Residual or rationale |
| --- | --- | --- | --- |
| C1 — Human-only MCP approval | **Resolved** | The parity outcome limits human decisions to current human-delegated MCP sessions (`prd.md:213-220`); RBAC and service-client scopes split human-delegated from AI/tool clients (`prd.md:786-839`); approval transitions require current human presence and principal independence (`prd.md:542-548`); FR41/FR42/FR83 and NFR67 preserve the same rule (`prd.md:1309-1315`, `prd.md:1388`, `prd.md:1570`). | The AI/tool MCP class uses a closed positive allowlist, so `CancelAIAction` is also unavailable even though it is not repeated in the explicit denial examples. The old `mcp-tool-client` label is retained, but its semantics are unambiguously AI/tool-bound. |
| C2 — Resume-only exit from `Deferred` | **Resolved** | The authoritative lifecycle has only `Deferred -> NeedsReview`; confirm/reject source only from `NeedsReview`, direct attempts from `Deferred` return `state-not-permitted`, and resume requires refreshed evidence (`prd.md:492-527`). FR6 removes the worker-only command from human capability (`prd.md:1239`). | S2 and FR76 are generic surface descriptions, but the Authority Map makes the Shared Workflow Contract sole transition authority, so they cannot authorize another exit. |
| C3 — Full correction impact completion | **Unresolved — Critical** | The main lifecycle, UJ4, FR7, FR91a, NFR17a, A13, and the M0 gate now cover dual-Project authority, Conversations/Folders ownership, effectful/irreversible consequences, owner acknowledgements, incident delay, and terminal completion (`prd.md:422-430`, `prd.md:498-532`, `prd.md:1240`, `prd.md:1400`, `prd.md:1483`). | The declared exhaustive ChatBot durable-record/ownership surface omits the correction impact manifest (`prd.md:591-610`, `prd.md:745-757`). NFR15a's correction row still describes only a generic correction record plus derived-context invalidation and fails closed only on singular Project ownership/invalidation-queue availability (`prd.md:1466`), not both Project authorities, manifest durability, owner-contract availability, and acknowledgement/disposition integrity. This leaves the critical record without an explicit A6 retention/isolation owner contract and admits a weaker fail-closed implementation than FR91a. |
| C4 — A11 split across M1/M2 | **Resolved** | Current Release Status, increment gates, A11, and the addendum gate/SLO contracts consistently make A11-M1 block the M1 observation/exit and A11-M2 block M2 (`prd.md:100-108`, `prd.md:289-291`, `prd.md:1425`; `addendum.md:143-149`, `addendum.md:263-307`). | `.memlog.md:52` records the required override of the prior M2-only A11 decision. |
| H1 — Machine-evaluable increment gate record | **Resolved** | `addendum.md` §Increment Gate Record Contract defines immutable fields, signer independence, expiry, reopen/invalidation, supersession, and the closed computed-status set; Current Release Status and NFR65 consume the gate model (`addendum.md:143-149`, `prd.md:100-108`, `prd.md:1566`). | No prose or file-presence shortcut closes a gate. |
| H2 — Policy snapshot owned in M0 | **Resolved** | The Data Governance Surface assigns Policy snapshot to M0 and spells out bootstrap/M1 ownership, A6 treatment, and native-store isolation (`prd.md:602`). M0 bootstrap and FR61 use that record (`prd.md:301-303`, `prd.md:1343`). | Owner boundary remains with ChatBot-derived policy state while Tenants remains authoritative for tenant facts/policy authority. |
| H3 — Indeterminate classifier is no-write denial | **Resolved** | The workflow, FR39, Risk Classifier appendix, retry profile, and NFR15a all return `classifier-indeterminate`, create no proposal/idempotency state, allow only a typed non-mutating attempt record, and require a new linked request after remediation (`prd.md:540`, `prd.md:1293-1308`, `prd.md:1472`; `addendum.md:49-59`, `addendum.md:252`). | No approval path promotes the indeterminate attempt. |
| H4 — Approval expiry and material-drift invalidation | **Resolved** | `addendum.md` §Approval Freshness selects exact default/maximum TTLs, immutable timestamps/digests/evidence, `Approved -> Expired`, drift invalidation, and new-proposal-only renewal (`addendum.md:61-68`). The workflow, FR42/FR50, NFR16/NFR36/NFR48, and service clients consume it. | `.memlog.md:53` records the TTL decision; it is not borrowed from credential-cache lifetime. |
| H5 — Non-gameable association evaluation | **Resolved** | SM1/SM7/SM-C1 and `addendum.md` §Association Evaluation Protocol separate final correctness, auto precision/recall, safe abstention, and wrong association; define mutually exclusive populations, minima, prevalence, adjudication, disagreements, formulas, raw counts, and Wilson bounds (`prd.md:178-199`; `addendum.md:29-38`). | Medium cleanup: A9a still calls the first/last partitions `deterministic-match` / `inbound-authenticity-anomaly`, while the protocol uses `one-authorized-project-unambiguous` / `authenticity-anomaly` (`prd.md:1423`; `addendum.md:33`). Add an explicit alias/migration mapping so dataset implementations cannot interpret two taxonomies. |
| H6 — Operable pre-pilot data-rights workflows | **Unresolved — High** | O1, RBAC, owner-driven workflow states, retry rules, FR58, and the five requested status/result queries now cover request, approval, partial completion, hold precedence, result expiry/redaction, retry, appeal, and owner acknowledgements (`prd.md:623-625`, `prd.md:786-804`, `prd.md:574-580`, `prd.md:948-953`, `prd.md:1340`; `addendum.md:259`). | FR75f still says a `compliance-admin` “Cannot operate on workflow items” (`prd.md:1373`) without the required exception for the named data-protection workflow family. That contradicts FR58 and the Shared Workflow Contract, permitting implementations either to admit or reject the same data-rights command. Clarify that the prohibition covers collaboration/Project workflow items while export, erasure, hold, and retention operations remain the only allowed workflow family. |
| H7 — Unknown outbound-send reconciliation | **Resolved** | The authoritative workflow defines `SendOutcomeUnknown -> Reconciling -> Sent | NotSent | Unresolved`, a four-hour deadline/P2 escalation, no blind retry, and new-draft-only-after-`NotSent` (`prd.md:551`; `addendum.md:238-240`, `addendum.md:256`). `ReconcileOutboundSendOutcome` and the stable equivalent query `GetOutboundSendStatus` are in the public catalogs (`prd.md:884`, `prd.md:947`). | The existing status naming is acceptable because FR80 requires current state, partial output, terminal reason, and safe next action; it does not weaken the requested outcome-query semantics. |
| H8 — Untrusted external/retrieved AI content | **Resolved** | The product invariant, system journey, FR27/FR33, NFR8/NFR9, A5, and the addendum preserve immutable origins, separate data from instructions/authority, block suspicious instruction patterns, and require adversarial fixtures across every named source class (`prd.md:90-98`, `prd.md:473-482`, `prd.md:1268`, `prd.md:1284`, `prd.md:1435-1444`, `prd.md:1419`; `addendum.md:70-74`). | Medium cleanup: repeat prompt/tool-injection coverage explicitly in the FR39-FR46 acceptance row and NFR67/NFR68 so story-readiness and release-security matrices point to the same mandatory fixtures. NFR9/A5 already makes the tests normative, so this is traceability rather than an alternate conforming security behavior. |
| H9 — Closed admission-stage profiles | **Resolved** | FR81a makes the universal stages explicit and delegates class-specific stages to one centrally selected closed profile; the addendum supplies seven exhaustive profiles and fails unknown classes closed (`prd.md:1386`; `addendum.md:151-173`). | Adapters/handlers cannot select, omit, replicate, or reorder admission stages. |
| H10 — Complete sender-authority evidence tuple | **Resolved** | The Microsoft 365 constraint, outbound guard, and addendum define the provider-neutral tuple, map all five FR48 classes, require every element to be current/consistent, and provide typed fail-closed outcomes (`prd.md:994-1004`, `prd.md:551`; `addendum.md:220-236`). | Owner boundaries remain intact: M365 capability never creates ChatBot Project/outbound authority. |
| H11 — Separate inbound-authenticity workflow | **Unresolved — High** | The lifecycle/state family, strict/paranoid terminality, reprocessing-by-successor, S2a, FR48d, policy schema, and retry profile are present and kept separate from association/participant states (`prd.md:488`, `prd.md:619`, `prd.md:1326`; `addendum.md:124`, `addendum.md:209-218`, `addendum.md:248`). | Three required closures are absent: (1) no inbound-authenticity durable record appears in the exhaustive Data Governance Surface; (2) no `GetInboundAuthenticityStatus` or semantic equivalent appears in the public query catalog; and (3) review/reprocess authority is a mailbox admin alone rather than `mailbox-admin` plus an independent `policy-admin` (`prd.md:488`, `prd.md:929-954`; `addendum.md:217-218`). A single mailbox principal can therefore release anomalous intake without the specified separation of duty, and machine consumers lack a stable per-item status query. |

## Critical findings

### C3-R1 — Correction manifest is outside the declared durable-record contract

`FR91a` requires a durable, versioned manifest, but §Data Governance Surface and §Context Ownership do not list it. Because those sections define the owned-record and A6/isolation surfaces, an implementation could preserve the workflow state while failing to apply explicit retention, redaction, native-store isolation, export/erasure, and ownership rules to the manifest itself.

**Required fix:** add `Correction impact manifest` as an M0 ChatBot-owned durable record with source, A6 retention/export/erasure/hold treatment, high sensitivity, tenant partition/isolation proof, and provenance. Include it in the ChatBot ownership list. Extend the NFR15a Correction row to freeze/commit the complete manifest atomically and fail closed when either Project authority, owner contract, manifest persistence, canonical audit, or required acknowledgement/disposition validation is unavailable.

## High findings

### H6-R1 — Compliance role denies the workflow it must operate

FR58 and the Shared Workflow Contract authorize `compliance-admin` initiation/tracking of export, erasure, hold, and retention workflows, while FR75f unqualifiedly forbids the same role from operating workflow items.

**Required fix:** state that `compliance-admin` may operate only the enumerated data-protection workflow family under its independent approval and owner-authority guards; retain the prohibition for association, conversation, Project, AI-action, and other collaboration workflow items.

### H11-R1 — Authenticity admission still lacks complete record, query, and separation-of-duty contracts

The state machine exists, but its record is absent from the exhaustive governance table, its per-item status query is absent from the stable query catalog, and a mailbox admin alone may accept anomalous intake or create a successor. Those gaps weaken the pre-association boundary that H11 was intended to make operable.

**Required fix:** add the M0 inbound-authenticity record to §Data Governance Surface; add `GetInboundAuthenticityStatus` (or explicitly norm the existing equivalent, if one is deliberately selected); require a current `mailbox-admin` initiator plus independent `policy-admin` approval for accept/reject and blocked/rejected successor creation, while preserving mailbox-only evidence visibility and no Project-data access.

## Medium/low tail

- **Medium — evaluation taxonomy aliases:** normalize or explicitly map A9a's `deterministic-match` / `inbound-authenticity-anomaly` labels to the authoritative protocol labels.
- **Medium — injection-fixture traceability:** repeat NFR9/A5 prompt/tool-injection fixtures in the FR39-FR46 readiness row and NFR67/NFR68.
- **Low — MCP principal naming:** `mcp-tool-client` is semantically closed as AI/tool-bound, but renaming it to `mcp-ai-tool-client` would match the remediation vocabulary. If retained for compatibility, state that it is the stable legacy name.

## Stable-ID and boundary audit

- FR1–FR96 remain present exactly once; existing letter-suffixed FR definitions remain unique.
- NFR1–NFR70 remain present exactly once; existing letter-suffixed NFR definitions remain unique.
- No FR/NFR definition ID was removed or added relative to `HEAD`.
- Existing correction operation `AcknowledgeAssociationCorrectionStore` remains stable and is explicitly treated as a legacy name for manifest-item acknowledgement.
- Newly required workflow capabilities were added as operation/query catalog entries without renumbering FR/NFR IDs. `GetOutboundSendStatus` is accepted as the named stable semantic equivalent of the proposed `GetOutboundSendOutcome`.
- Conversations and Folders retain mutation authority under A13; ChatBot orchestrates and records acknowledgements. The Critical C3 residual is that the manifest itself has not yet been placed in ChatBot's declared owned-record surface.
- Gate semantics remain fail-closed and evidence-bound. The required A11 override and H4 TTL decision are present in `.memlog.md:52-53`; no other memlog content was used for this verification.

## Re-review acceptance

The gate can move from **STOP** to **PASS for specification consistency** after C3-R1, H6-R1, and H11-R1 are fixed without introducing a new transition, authority, operation-ID, or gate-status contradiction, followed by another FR/NFR uniqueness and owner-boundary check. This would not close the external evidence gates recorded in Current Release Status.
