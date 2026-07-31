# ADR: Replay and simulation isolation — test-tenant adapter, structural replay marker, and a fail-closed isolation probe

## Status

Accepted (realized by Story 9.4, FR95 / FR95a / NFR2 / NFR9a / NFR42 / NFR69, addendum §Replay Isolation). Builds on the
Story 9.1 WORM chain ([worm-audit-backing.md](worm-audit-backing.md)), the Story 9.2 replay marker + v2 hash coverage +
completeness exclusion ([audit-completeness-observable.md](audit-completeness-observable.md)), and the Story 9.3 default
query exclusion ([audit-investigation-surface.md](audit-investigation-surface.md)). Gates the **M2** release.

## Context

A QA/support engineer must be able to replay representative mailbox events so investigation and testing **never touch
production or send external email** (FR95/FR95a, NFR69). The prior Epic-9 stories scaffolded toward this: Story 9.2 added
the `AuditEnvelope.ReplayRunId` marker, folded it into the canonical hash from `chatbot.worm-chain.v2`, and introduced
`AuditReplayExclusion.IsReplayEnvelope`; Story 9.3 made the default-query exclusion real. But **every production audit
record left `ReplayRunId` null** and there was no replay execution path, no test-mode outbound adapter, and no isolation
probe. Story 9.4 owns those pieces — and it does so by **wiring new seams into existing extension points**, never by
re-deriving tenant partitioning, the exclusion predicate, the safe-token rules, or the audit-then-deliver alert path.

## Decision

1. **Test-tenant isolation by construction — one authoritative predicate.** A tenant is a replay/test tenant **iff**
   `ReplayTenantPolicy.IsTestTenant(tenantId)` returns true. The discriminator is a deterministic, configuration-free
   convention: the reserved tenant-id prefix `replay-test:`. The id must be an `AuditMetadata`-safe stable identifier
   first, so an empty/unsafe id is **fail-closed to "not a test tenant"** (treated as production for the probe sweep). The
   same predicate is consumed everywhere a test-tenant decision is made — outbound adapter selection and the nightly
   probe — so the two can never drift. Production tenants never carry the prefix, so they can **never** resolve the
   test-mode adapter ("Production tenants do not have access to the test-mode adapter").

2. **Structurally-un-rewritable replay marker (FR95a).** `ReplayRunId` rides the **same** channel as `SurfaceOrigin`
   (FR85/S7): it is set once at the adapter boundary on the immutable `ChatBotCommandSubmission`, travels unchanged
   through the immutable `ChatBotGatewayContext`, and is emitted from the single `AuditEnvelopeFactory.Create` point as
   `AuditMetadata.SafeOptionalToken(context.Submission.ReplayRunId)`. Because **every** command-path factory method
   (pre-commit, post-commit, duplicate-suppression, rejection, escalation) funnels through `Create`, this one line marks
   all of a replay run's envelopes. A production submission leaves it null **by omission**, not by an active clear step.
   The boundary reads it from the `X-Hexalith-Replay-Run-Id` header (sanitized) so a replay run can be driven through the
   gateway today. The non-`Create` system/operator factories (`AuditChainBroken`, `AuditCompletenessBudgetBreached`,
   `AuditRecordRedacted`, `ReplayIsolationBreach`, the operational-alert factories) deliberately stay production (null) —
   they are out-of-band system envelopes, not command-path records of a replay run.

   This composes with the prior stories with **no new hash work**: the v2 canonical form (Story 9.2) already folds
   `ReplayRunId` into the tamper-evident hash, the default compliance query already excludes replay records (Story 9.3),
   and the completeness measure already drops them from numerator and denominator (Story 9.2). Story 9.4 makes those
   exclusions fire against **real** marked records instead of synthetic ones. The Story 9.2 predicate
   `AuditReplayExclusion.IsReplayEnvelope` is reused verbatim — not re-derived or relocated.

3. **Test-mode outbound adapter + tenant-partitioned outbound-trace store (AC1).** `TestModeOutboundMailboxSender`
   (`IOutboundMailboxSender`) intercepts every send for a test tenant, records the metadata-only would-have-sent envelope
   to the test tenant's `IOutboundTraceStore`, and returns `OutboundMailboxSendResult.Sent("adapter:mailbox-outbound-testmode")`
   **without contacting any external system** (none is reachable by construction). Returning `Sent` is deliberate: the
   aggregate's `AdapterStatus == "sent"` path then runs **identically** to production, so a replay run exercises the real
   success flow end-to-end while no message leaves the boundary. The trace record (`OutboundTraceRecord`) carries only the
   safe identity tokens already on `OutboundMailboxSendRequest` (extended with a nullable `ReplayRunId` populated at the
   dispatcher send seam) plus a server-UTC timestamp — **never** recipient addresses, subject, or body (NFR2/NFR42); every
   field is reduced to a safe bounded token on construction. The store is tenant-partitioned exactly like
   `IWormAuditStore` (NFR9a). Selection is a single decision point: `ReplayAwareOutboundMailboxSender` is the registered
   `IOutboundMailboxSender` the dispatcher resolves and routes by `ReplayTenantPolicy.IsTestTenant(request.TenantId)` — a
   test tenant to the test-mode sender, **every production tenant to the existing production sender unchanged** (today
   `UnavailableOutboundMailboxSender`).

4. **Nightly isolation probe — pure verifier + fail-closed coordinator (AC3).** `ReplayIsolationVerifier` is a pure
   evaluator asserting **two complementary invariants** over a production tenant: (a) no outbound-trace record carries a
   non-null `ReplayRunId` (the primary AC3 assertion), and (b) no WORM-chain envelope is a replay envelope (defense in
   depth, reusing `AuditReplayExclusion.IsReplayEnvelope`). `ReplayIsolationProbeCoordinator` mirrors
   `AuditChainVerificationCoordinator` line-for-line: for each production tenant (`!ReplayTenantPolicy.IsTestTenant`) it
   enumerates both stores, verifies, and on any breach does **fail-closed audit-then-deliver** — write the metadata-only
   `AuditEnvelopeFactory.ReplayIsolationBreach` pre-commit envelope, then emit exactly one
   `OperatorAlertKind.ReplayIsolationBreach` alert. An enumeration that throws is `Unknown` — a breach signal, never a
   silent pass. Test tenants are skipped by construction (a test-tenant trace record is expected, not a breach).

5. **M2 release-gate wiring.** `SweepAllProductionTenantsAsync(runCorrelationId, ct)` returns
   `ReplayIsolationProbeOutcome(TenantsSwept, Breaches, Alerted)` — the structured contract a CI/release gate asserts
   against. **Zero breaches ⇒ the M2 release may proceed; any breach (or `Unknown`) is stop-ship.** The gate calls the
   same method a periodic scheduler would; a passing release requires `Breaches == 0`.

## Deferrals (inert-control-floor honesty)

Consistent with Stories 9.1/9.2, both the scheduler and release-gate-consumer deferrals below are now **retired**
(Story 12.14); only the replay-initiation surface remains deferred. This is **enforcement-complete, not
isolation-incomplete**:

- ~~**The periodic scheduler/trigger**~~ — **wired by Story 12.14 (2026-07-21).** The existing
  `PeriodicEnforcementBackgroundService` calls `SweepAllProductionTenantsAsync` on a configurable cadence (default once
  per UTC day, gated by `ChatBot:PeriodicEnforcement:RunM2AuditRecoverySweeps`), preserving the coordinator's
  fail-closed breach path. No parallel `BackgroundService` or Dapr timer was introduced. The scheduler publishes the breach signal on
  `/health/chatbot/periodic-enforcement/m2`, which returns HTTP 503 unless every M2 sweep last completed with zero
  breaches over non-zero coverage and the result is still fresh (disabled, never-completed, failed, stale, or
  zero-coverage sweeps are stop-ship). That endpoint requires a shared-secret header and is not mapped when no token is
  configured. The required `release.yml` topology-acceptance job asserts it before `semantic-release`.
- **The replay-initiation surface** (a QA/support UI or CLI that starts a run). The end-to-end seam — test-tenant adapter
  selection, `ReplayRunId` threading from the boundary into audit + trace, the outbound-trace store, and the isolation
  probe — is the shippable deliverable and is fully built and tested. A replay run can be driven through the gateway today
  via the submission marker; a future driver reaches replay **only** through the gateway/client seam, never the internals.

The enforcement (test-tenant adapter, structural marker, isolation probe) **is** built, tested, scheduled, and consumed
by the release pipeline. Only the initiator UI/CLI remains deferred.

## Consequences

- Replay isolation is achieved **through** tenant partitioning (NFR9a), not beside it: a replay run lands entirely in the
  test tenant's partitions, so no production chain/projection/idempotency/outbound-trace store is mutated. The test-mode
  adapter is an additional belt-and-braces guard so that even if a replay command reached an outbound send, no external
  system is contacted.
- The two-phase WORM audit (D4/NFR49a) is untouched: no new commit-time gate, no chain mutation, no canonical-hash change
  (v2 already covers the marker). The probe is an out-of-band read.
- Boundary: `ReplayTenantPolicy`, `IOutboundTraceStore`/`OutboundTraceRecord`, `TestModeOutboundMailboxSender`,
  `ReplayAwareOutboundMailboxSender`, `ReplayIsolationVerifier`/`ReplayIsolationProbeCoordinator`, and `AuditEnvelope`
  remain `internal` to `.Server` (NetArchTest-enforced) — no `.UI`/`.Cli`/`.Mcp` reference.

## References

- Story files: `_bmad-output/implementation-artifacts/9-4-replay-and-simulation-isolation.md`;
  `9-1-tamper-evident-worm-audit-chain.md`; `9-2-audit-completeness-as-a-production-observable.md`;
  `9-3-audit-query-and-compliance-investigation-surface-s9.md`.
- PRD addendum §Replay Isolation; epics.md Story 9.4; architecture.md (replay/simulation test-tenant isolation; two-phase
  audit / WORM; M2 increment).
- Code: `src/Hexalith.ChatBot.Server/Audit/ReplayTenantPolicy.cs`, `ReplayIsolationVerifier.cs`,
  `ReplayIsolationProbeCoordinator.cs`, `AuditEnvelopeFactory.cs`, `OperatorAlertKind.cs`;
  `src/Hexalith.ChatBot.Server/Adapters/Mailbox/TestModeOutboundMailboxSender.cs`, `IOutboundTraceStore.cs`,
  `IOutboundMailboxSender.cs`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandSubmission.cs`,
  `Gateway/Stages/AcceptedCommandDispatcher.cs`, `Gateway/CommandGatewayServiceCollectionExtensions.cs`, `Program.cs`.
