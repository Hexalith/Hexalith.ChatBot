---
status: active-ledger
updated: 2026-08-03
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md
productEpicCountImpact: 0
---

# Hexalith.ChatBot Technical Enablers

This ledger tracks architecture and platform work that preserves product behavior but does not provide a standalone user outcome. Technical enablers are excluded from the product epic and assignable-story counts. Product-facing behavior remains accepted in the owning product stories.

## TE-1 — DomainService SDK Host Adoption

- **Status:** complete; evidence reconciled 2026-07-17
- **Decision:** `docs/adrs/domainservice-sdk-host-adoption.md`
- **Product invariants preserved:** FR81/FR81a shared command admission; UI/API/CLI/MCP compatibility; tenant isolation; authorization; audit; idempotency; project-conversation queries; SignalR authorization and re-query behavior.
- **Platform prerequisite:** TE-1.2 is owned by the Hexalith.EventStore SDK plan and implemented in the root-declared `references/Hexalith.EventStore` submodule. ChatBot consumes the capability; it does not treat the platform change as product value.
- **Selected host outcome:** module-owned `.Aspire` and `.ServiceDefaults` projects are retired. The ADR-scoped local `Hexalith.ChatBot.AppHost` shim remains because the platform composition API cannot yet express ChatBot's dedicated Dapr state, workflow, and pub/sub resources. There is no open remove-or-retain alternative.

| Task | Outcome | Historical implementation evidence | Status |
| --- | --- | --- | --- |
| TE-1.1 | Accept the host-reuse ADR and preservation matrix. | `../implementation-artifacts/11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md` | complete |
| TE-1.2 | Add the EventStore SDK pre-commit admission hook. | `../implementation-artifacts/11-2-platform-pre-commit-admission-hook-in-the-domainservice-sdk.md` | complete |
| TE-1.3 | Migrate ChatBot queries and cursor behavior to SDK contracts without changing public results. | `../implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md` | complete |
| TE-1.4 | Migrate projections, telemetry, and health to SDK contracts without changing operator behavior. | `../implementation-artifacts/11-4-migrate-projections-telemetry-and-health-to-sdk-contracts.md` | complete |
| TE-1.5 | Reduce the Server host and mount CommandGateway at the SDK admission seam without introducing a second pipeline. | `../implementation-artifacts/11-5-reduce-the-server-host-to-the-sdk-shape-with-the-commandgateway-admission-hook.md` | complete |
| TE-1.6 | Retire module-owned Aspire/ServiceDefaults and retain the ADR-scoped AppHost shim for dedicated ChatBot Dapr resources. | `../implementation-artifacts/11-6-retire-apphost-aspire-servicedefaults-and-compose-via-addeventstoredomainmodule.md` | complete |
| TE-1.7 | Initialize AppHost security services through EventStore Aspire helpers. | `../implementation-artifacts/11-7-apphost-security-service-initialization-via-eventstore-aspire.md` | complete |

### Completion evidence rule

TE-1 remains complete only while the linked architecture decision and implementation records continue to prove the product invariants above. A future platform-composition enhancement may remove the retained AppHost shim, but that is a new platform task and does not reopen a ChatBot product epic unless it changes user- or operator-visible behavior.

## TE-2 — Mechanical Story-Evidence Integrity Gate

- **Status:** review; implementation self-validates prospectively, but completion remains blocked on the required protected check.
- **Source:** `sprint-change-proposal-2026-08-03.md`
- **Owners:** Amelia (implementation) with Murat (evidence/primary-path policy); Winston reviews architecture and CI boundaries.
- **Product impact:** none; delivery-integrity control only.
- **Invariant:** a story or technical-enabler item may move to `done` only when the repository-owned gate passes for the proposed status, exact scoped diff, exact machine results, required primary paths, File List, and checked-task/acceptance mappings.

| Task | Outcome | Status |
| --- | --- | --- |
| TE-2.1 | Define the versioned JSON policy and per-story evidence-contract schema, including stable reason codes and metadata-only output. | complete |
| TE-2.2 | Reconcile story/sprint status, File List, explicit out-of-scope disclosures, root and root-declared-submodule diffs/gitlinks, and mandatory checkbox state. | complete |
| TE-2.3 | Parse policy-approved machine results, bind them to the tested implementation digest, and enforce required primary-path execution with zero-test/all-skipped/fallback-only failure. | complete |
| TE-2.4 | Require every checked task and acceptance criterion to map to current diff and/or passing machine assertions; detect proposed `done` transitions in CI and emit a fail-closed report. | complete |
| TE-2.5 | Prove the gate with positive, per-reason negative, mutation, multi-repository, and bootstrap self-validation fixtures; publish the developer runbook and activate the protected check. | in-progress — repository work complete; external required-check activation remains open |

### Completion evidence rule

TE-2 cannot waive its own gate. While its record is still `review`, CI evaluates it with `targetStatus=done`; after all negative mutations are proven to fail and the positive exact-scope run passes, its status may change to `complete`. The Epic 13 action stays open until that self-validation and required branch check are both active.
