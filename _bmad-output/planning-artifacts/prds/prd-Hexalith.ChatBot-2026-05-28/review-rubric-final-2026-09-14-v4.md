# PRD Quality Review — Hexalith.ChatBot (Final Gate v4)

## Overall verdict

**Conditional pass — two contract closures remain before implementation handoff.** The post-v3 remediation closes the prior FR74/admin-role ambiguity, replaces the historical sibling pointer with one current nine-context/A13 reconciliation, keeps the current architecture and consumed-contract hashes reproducible, and pairs all 15 A11 target rows with candidate-bound qualification rows. A5, A6, A10, A11, and A13 are coherent external stop-ship gates: each has scope, owner, timing, disable behavior, and bounded claims, so their honestly absent external proof is not counted as a document defect.

Two internal gaps remain material. First, M0 consumes tenant-policy values, ChatBot admin authority, and four M0 service clients, but their only grant/mutation contracts are assigned to M1. Second, the data-subject/retention rows promise linked retries without naming the retry operations that FR65 and the declared complete catalog say exist. The remaining medium finding is the absence of the per-workflow retry profiles required by NFR18.

## Decision-readiness — adequate

The product wedge, governed Project boundary, exact AI allowlist, mandatory-approval effects, increment claims, and release gates are explicit. The Authority Map, Current Release Status, increment gate table, assumptions register, and qualification ledger agree on what may be claimed and what disables each increment.

### Findings

- **high** First-use administration is sequenced after its M0 consumers (§Minimum Release Slice; §Shared Workflow Contract; §Service Client Permissions; FR9/FR19/FR75a–g; `addendum.md` §Tenant Policy Schema). M0 association and AI decisions consume versioned policy values, three A6-governed M0 rows have no pre-approval default, and four service-client classes are required in M0. Yet `UpdateTenantPolicy`, `GrantServiceClientPermission`/`RevokeServiceClientPermission`, and all ChatBot admin-role grant commands are M1; the Tenant-Admin Permission Model is also explicitly M1. The sole shared-command invariant leaves no authorized M0 path to create the first policy/admin/client state, while the M0 gate requires a configured tenant and tenant-admin actor tests. *Fix:* define an FR81a-compliant M0 bootstrap path with exact actor/co-approver and immutable first-version semantics, or classify the minimum policy, admin-grant, and service-client grant operations as M0/M1 while retaining the full editor and broader governance surface in M1.

## Substance over theater — strong

The journeys carry real authorization, correction, failure, approval, operations, and compliance consequences. Success measures distinguish release gates from diagnostics, and the qualification ledger uses `unsupported` rather than invented evidence. NFRs contain product-specific observables and stop conditions.

## Strategic coherence — strong

The three increments consistently advance one thesis: turn external email into authorized Project work through deterministic association, governed AI action, one command spine, and reconstructable audit. Counter-metrics prevent adoption or AI-acceptance targets from rewarding unsafe narrowing.

## Done-ness clarity — adequate

The PRD now has an exhaustive-looking stable catalog, authoritative transition families, closed safety-control grammar, exact admin-role mutations, expected-revision rules, fail-closed path inventory, and candidate-bound A11 evidence fields. Most workflow stories can be authored without inventing product semantics.

### Findings

- **high** Data-subject and retention retries are promised but have no stable public operation contract (§Shared Workflow Contract; §Command and Query Contracts; FR65; NFR70). The export, erasure, and retention rows say a retryable failure creates a linked request or attempt, but name no retry command, actor, source/destination transition, retry event, or replay/conflict result. The declared complete catalog contains only `InitiateDataExport`, `InitiateDataErasure`, and `ExecuteRetentionDisposition`, while FR65 explicitly says the section exposes family-specific retry/recovery commands for data-subject/retention work. *Fix:* add exact retry commands and transition/event rules, or explicitly define reuse of the initiating command with a new linked identity and its allowed source states, authorization, equivalence, and conflict behavior.
- **medium** NFR18 still delegates the retry contract that QA must verify (§Reliability and Data Integrity). It requires retryable/terminal classes, maximum attempts, backoff, jitter, dead-letter criteria, manual recovery, and terminal reasons per workflow type, but neither the PRD nor normative appendices provide profiles, an accountable owner, or a pre-release evidence gate for them. The transition tables define successor identity but not exhaustion policy. *Fix:* add a compact per-family retry-profile table, or name the versioned architecture/runbook artifact, owner, approval timing, safe defaults, and gate that makes those profiles authoritative before each workflow ships.

## Scope honesty — strong

M0 is a controlled preview, M1 a governed cross-surface pilot, and M2 a production/release candidate. The PRD does not claim GDPR satisfaction, tamper-evident completeness, owner-contract acceptance, recovery readiness, or SLO support while A5/A6/A10/A11/A13 evidence remains open. Those are properly owned product stop-ship gates, not document defects.

## Downstream usability — adequate

Stable FR/NFR identifiers, workflow contracts, role mappings, UI inventory, traceability, source hashes, qualification evidence, and the 43-entry memlog audit give UX, architecture, engineering, security, compliance, and QA usable extraction anchors. The two high findings above are localized but must be closed because they otherwise force story authors to invent M0 provisioning and retry semantics.

## Shape fit — strong

The main PRD retains outcomes, actors, scope, gates, states, and requirements; the normative addendum holds executable policy and pipeline details; mutable evidence and source lineage remain separate. That shape is proportionate to a chain-top B2B SaaS orchestration product spanning nine bounded contexts and material security/compliance risk.

## Mechanical notes

- The current source manifest points to `reconcile-full-sibling-a13-2026-09-14.md`; its architecture SHA-256 and consumed-contract hashes match the inspected inputs.
- All 15 normative A11 metric names have one candidate-bound qualification row; all are honestly `unsupported` for the not-yet-selected M2 candidate.
- `.memlog.md` and `memlog-audit-2026-09-14.md` both contain 43 entries/rows.
- The stable operation catalog contains 68 named commands; each named catalog command is referenced by a workflow or contract. The missing data-subject/retention retry operations are counted above because the prose promises them without naming them.
- Severity totals: **Critical 0 · High 2 · Medium 1**.
