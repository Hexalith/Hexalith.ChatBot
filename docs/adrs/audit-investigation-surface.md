# ADR: Audit query and compliance investigation surface (S9) — read/escalate-only over the WORM chain

## Status

Accepted (realized by Story 9.3, FR54 / FR56 / FR75f / FR95a / NFR2 / NFR9a). Builds on the Story 7.4 admin-scope
and compliance-contract forward-scaffold, the Story 9.1 WORM chain ([worm-audit-backing.md](worm-audit-backing.md)),
and the Story 9.2 completeness/replay work ([audit-completeness-observable.md](audit-completeness-observable.md)).

## Context

FR54/FR56 require compliance/support reviewers to **search and reconstruct** what happened — association decisions,
approvals, command outcomes, corrections, retries, risky AI actions — querying by tenant, actor, command, resource,
decision, reason, correlation, message id, surface, and time. FR75f scopes the authority precisely: `compliance-admin`
reads audit **across** the tenant (with per-project redaction, NFR2) and triggers investigations, but **cannot operate
on workflow items**.

Story 7.4 forward-scaffolded the contracts (`ComplianceAuditQueryFilters`, `ComplianceAuditResultRow`,
`ComplianceAuditDetail`, the `SearchComplianceAuditRecords`/`GetComplianceAuditDetail` queries, the
`RequestComplianceInvestigation`/`RequestComplianceEscalation` commands), the read policy
(`ComplianceAuditReadPolicy.Search/Detail`), and the binding S9 DOM contract (the `ComplianceAdministrationE2ETests`
fixture). Until this story those read-policy methods were **called by nothing**. Story 9.3 is the wiring: a real chain
source, a tenant-scoped endpoint pair, and a Blazor surface — plus two small read-policy extensions.

## Decision

1. **Two filter dimensions added in lock-step (FR56).** `message-id` and `surface` join
   `ComplianceAdministrationSchema.AuditFilterKeys` **and** `ComplianceAuditReadPolicy.MatchesFilter` together. `surface`
   matches the envelope's `SurfaceOrigin`; `message-id` matches a `source-message:` / `provider-message:` token in
   `SourceEvidenceRefs` (the value treated as an opaque safe token, never raw content). `FilterKey` is a free string
   validated against the key set, so widening it is a backward-compatible **v1** change — no `ComplianceAuditFilterRef`
   shape change and no OpenAPI/client regeneration.

2. **Replay exclusion from default production queries (FR95a).** `ComplianceAuditReadPolicy.Search` filters out
   replay-marked envelopes by default using the Story 9.2 `AuditReplayExclusion.IsReplayEnvelope` predicate, composed
   into the existing safe-identifier/time-window/filter `Where` chain. Production holds zero replay records today (Story
   9.4 owns populating `ReplayRunId`), so the exclusion holds by construction — but it is **real and testable now**
   (inject a replay-marked envelope; assert it is absent from a default query). A replay-scoped investigation mode is
   explicitly out of scope.

3. **Tenant-scoped, Compliance-gated read endpoints wired to the chain.** `POST /api/v1/compliance/audit/search` and
   `GET /api/v1/compliance/audit/{auditRecordRef}` mirror the `audit-history` read-endpoint pattern: resolve the
   correlation context, `TryResolveTenant` from the authenticated principal, enumerate exactly one tenant's chain via
   `IWormAuditStore.EnumerateChain(tenantId)` (tenant-partitioned — NFR9a), and run `Search`/`Detail`. Authority is
   `ComplianceAuditReadPolicy.CanSearchTenantAudit` (`AdminScope.Compliance` via `AdminAuthorityEvaluator`). A
   non-Compliance principal, a non-human actor, an unresolved/cross-tenant tenant, an invalid query, and an unknown or
   replay-marked record **all collapse to the identical safe-not-found**, so the read never confirms whether a
   restricted resource exists (NFR2). The endpoints return string-token wire models (`ComplianceAuditHttpResults`) so
   redaction/escalation states travel as their kebab tokens, never enum ordinals, and every field stays an
   `AuditMetadata`-safe bounded token.

4. **Per-project authority drives detail visibility, never assumed (AC2, Flow 7).** `Detail(envelope,
   hasPerProjectAuthority)` returns `DetailAvailable` / `view-metadata` / visible evidence refs with authority, and
   `EscalationRequired` / `request-access` / empty refs without. Authority is evaluated against the reviewer's actual
   grants: `ComplianceAuditReadPolicy.HasPerProjectAuthority` matches a `project-owner` claim against a `project:`
   evidence token on the record. A tenant-wide compliance reviewer therefore sees redacted rows but must escalate for a
   project's detail unless explicitly granted.

5. **Read/escalate-only surface (FR75f, NFR2).** The S9 Blazor surface (`ComplianceAuditInvestigation.razor` +
   `ComplianceAuditService`) reads metadata-only timeline rows through `IChatBotClient` and dispatches only the
   already-allowlisted `RequestComplianceInvestigation` / `RequestComplianceEscalation` commands (which record intent —
   they are not workflow-item mutations) with an **opaque** escalation target. Any operate-style control (e.g. "Retry
   queue item") is rendered inert (`aria-disabled="true"`, reachable explanation via `aria-describedby`) and dispatches
   no workflow mutation. This is enforced structurally by authority (`AdminScope.Compliance` grants
   `{SeeOnly, Compliance, AuditObligation}` — never `Operate`/`Policy`/`Mailbox`), not merely by hiding buttons. The UI
   reaches audit data only through the client/HTTP seam — never `IWormAuditStore`, the read policy, or the audit-record
   types (NetArchTest-enforced).

## Consequences

- The investigation surface is a **read** over the WORM chain plus intent-recording commands. It appends nothing to the
  chain, adds no commit-time gate, and mutates no project/workflow state (D4 two-phase audit / NFR49a WORM). An endpoint
  test asserts the chain length is unchanged after a search.
- Because the new endpoints post-date the generated OpenAPI client (and the generated `FilterKey` enum predates
  `message-id`/`surface`), the UI reaches them through a small hand-written transport seam over the existing
  `HttpClient` rather than a regenerated client. Regeneration remains a future option if the contract is versioned.
- The binding S9 DOM contract (`ComplianceAdministrationE2ETests`) remains fixture-based; the real surface reproduces
  the same structure, now covered by a component-composition contract test. Repointing the Playwright scenarios to
  render the live Blazor component requires a browser-hosted render harness that does not yet exist in the repo (the
  current E2E uses `SetContentAsync` with inline HTML) and is deferred.

## References

- Story 9.3 (`_bmad-output/implementation-artifacts/9-3-audit-query-and-compliance-investigation-surface-s9.md`)
- `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`, `ComplianceAuditHttpResults.cs`,
  `IWormAuditStore.cs`, `AuditReplayExclusion.cs`; `src/Hexalith.ChatBot.Server/Program.cs` (compliance audit endpoints)
- `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs` (filter keys)
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`, `Services/ComplianceAuditService.cs`
- Related ADRs: [worm-audit-backing.md](worm-audit-backing.md), [audit-completeness-observable.md](audit-completeness-observable.md), [audit-two-phase.md](audit-two-phase.md)
