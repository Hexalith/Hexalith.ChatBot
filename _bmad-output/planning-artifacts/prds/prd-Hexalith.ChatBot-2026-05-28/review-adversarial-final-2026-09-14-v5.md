---
title: Adversarial Final Review v5 - Hexalith.ChatBot PRD
status: complete
created: "2026-09-14"
reviewedArtifacts:
  - "prd.md"
  - "addendum.md"
  - "source-manifest.md"
  - "qualification-evidence.md"
  - "reconcile-full-sibling-a13-2026-09-14.md"
reviewLens: "M0 governance-bootstrap and retry-profile regression check"
---

# Adversarial Final Review v5 — Hexalith.ChatBot PRD

## Verdict

**DOCUMENT-CONTRACT PASS — Critical 0, High 0, Medium 0.** The M0 governance-bootstrap and retry-profile additions introduce no Critical, High, or Medium document regression. The v4 closures remain intact.

This is a document-contract verdict, not a launch approval. A5, A6, and A13 remain explicit M0/M1 external implementation/evidence stop-ship gates; A10 and A11 remain explicit M2 stop-ship gates.

## M0 governance-bootstrap regression check

**Pass.** The first governance state is no longer left to an implicit seed or unaudited setup path:

- M0 names two distinct, current Tenants `TenantOwner` principals for bootstrap initiation and approval.
- The first ChatBot admin grant uses `GrantChatBotAdminRole`, validates the closed role/scope, and inherits expected revision, stable operation identity, replay/conflict behavior, and atomic canonical audit.
- The first tenant-policy snapshot uses `UpdateTenantPolicy` with the explicit first-version TenantOwner bootstrap rule, schema validation, independent approval, and all applicable A5/A6/schema co-approvals.
- The four M0 service-client classes are exhaustively enumerated as mailbox ingestion, audit projection, background retry, and AI action mediation. Their grants use `GrantServiceClientPermission`, retain bounded tenant/operation scopes and expiry, and do not inherit UI authority.
- The first service-client grant has the two-TenantOwner bootstrap rule; subsequent bootstrap grants require the now-established authorized admin and approved policy. Broader client grants and editor surfaces remain M1-only.
- No first version may be created by direct data seeding. All bootstrap mutations remain subject to the shared command spine and atomic canonical audit contract.

The bootstrap does not weaken the M0 gate. Required Security, Compliance/Data Protection, Architecture, and bounded-context owner approvals remain additive, and missing A5/A6/A13 evidence still disables live AI and pilot onboarding.

## Retry-profile regression check

**Pass.** The new `addendum.md` retry catalog supplies a versioned v1 default for each retained workflow family, with testable values for retryable reasons, terminal/no-blind-retry reasons, maximum automatic attempts, jittered exponential backoff, exhaustion/dead-letter state and owner, and manual recovery.

The retry contract remains fail-closed:

- A tenant/deployment profile may only reduce retries or make a reason terminal. Expanding retryability or a maximum requires a new version approved by the System Architect and Test Architect.
- The applied profile version is recorded in retry audit envelopes; retry state and canonical audit commit atomically.
- Approval, policy, admin-role, service-client, safety-control, and queue decisions have zero automatic retries. Transport uncertainty is resolved by replaying the same `operation_id`, not by creating a second decision.
- Approved actions and commands retry only typed pre-effect failures. Committed, rejected, stale, unauthorized, non-idempotent, and uncertain external effects never retry blindly.
- Outbound unknown-send outcomes enter investigation and block retry; sent results cannot resend.
- Governed-chat retry remains linked-attempt based, and the previously accepted stop/completion and cancel/streaming first-commit-wins contracts are unchanged.
- Export, erasure, and retention retry commands cover failed or partial work. Hold-blocked erasure/retention work can retry only after the exact hold is released and A6/policy evidence is revalidated.
- Exhausted items expose the terminal reason, attempts, next safe action, owner, and predecessor/successor identities. Dead-letter routing remains a projection and cannot authorize another attempt.

## Mechanical catalog and lifecycle check

- The current stable catalog contains **71 unique command IDs** with no duplicates.
- All 71 catalog commands appear in an authoritative association, owned-workflow, or governance/admin transition contract.
- The three new public recovery commands—`RetryDataExport`, `RetryDataErasure`, and `RetryRetentionDisposition`—are present in the catalog and have explicit source states, guards, successor states, events, actor rules, idempotent replay, and no-retry terminal conditions.
- The retry profile references the existing family-specific commands without creating an uncataloged durable mutation path.
- FR65 and NFR18 point to the same family-specific lifecycle and versioned-profile contracts; NFR18 makes profile conformance first-increment gate evidence.

## Critical/High/Medium regression sweep

No regression was found in tenant isolation, authority separation, policy bypass prevention, hidden mutation, audit atomicity, command/lifecycle coverage, chat races, increment timing, or A13 owner-contract gating. In particular:

- `safety.controls` remains writable only through `ApplySafetyControl` and `ReleaseSafetyControl`; `UpdateTenantPolicy` cannot mutate it.
- FR74 retains exactly four exhaustively governed subject classes, and FR75a retains explicit two-person admin grant/change/revoke commands.
- Every durable mutation remains routed through the shared command spine and must co-commit domain event, durable idempotency state, applied policy/approval references, and canonical audit envelope or commit nothing.
- The single current full-sibling/A13 chain remains `prd.md` A13 → `reconcile-full-sibling-a13-2026-09-14.md` plus `source-manifest.md`; the manifest retains the hash-bound Projects row.
- The sole increment gate table remains authoritative and now requires M0 retry-profile conformance without changing the inherited A5/A6/A13 or M2 A10/A11 timing.

## Gate recommendation

Accept the updated PRD/addendum set at the document-contract review gate. Preserve its explicit stop-ship status until the applicable external implementation and evidence gates close against an exact candidate.
