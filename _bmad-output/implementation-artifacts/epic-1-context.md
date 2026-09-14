# Epic 1 Context: First Safe Governed Action & Command Spine

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Stand up a deployable `Hexalith.ChatBot` module whose first user-visible UI action proves the complete safety spine end to end: every state-mutating operation is authenticated, tenant-bound, authorized, idempotent, fail-closed, audited before and after commit, lifecycle-validated, redaction-safe, and attributable to its originating surface. This is the architecture-mandated safety floor inherited unchanged by every later epic. It is delivered as a minimal surface over a complete spine — real from day one, never stubbed where safety is concerned. Story 1.9 is the value proof; every foundation story must either unblock that first governed command or add a mechanical guardrail that proves it is safe.

## Stories

- Story 1.1a: Solution scaffold, root config, and build-green baseline
- Story 1.1b: `references/` EventStore submodule and sibling dependency resolution
- Story 1.1c: Aspire/DAPR topology and local run verification
- Story 1.1d: CI/release skeleton and scaffold quality gates
- Story 1.1e: Centralize NuGet package-reference version authority
- Story 1.1f: Standardize reusable domain-module CI/CD and release gates
- Story 1.2: Establish the OpenAPI Contract Spine, typed Client, and `IChatBotCommand`
- Story 1.3: CommandGateway admission spine with tenant binding and authorization
- Story 1.4: Fail-closed audit-commit seam with pre- and post-commit audit emission
- Story 1.5: Two-altitude idempotency
- Story 1.6: Canonical lifecycle state model and transition enforcement
- Story 1.7: Versioned user-safe message catalog and redaction stage
- Story 1.8: Correlation propagation and long-running operation status
- Story 1.9: First governed command end-to-end with surface-origin attribution
- Story 1.10: Architecture dependency fitness tests
- Story 1.11: Differential-conformance harness
- Story 1.12: Cross-tenant isolation harness
- Story 1.13: Tenant-scoped fixture and evaluation scaffold
- Story 1.14: Visual inheritance and semantic token foundation
- Story 1.15: Shared governed component primitives
- Story 1.16: Interaction guardrails and keyboard safety
- Story 1.17: Responsive and touch foundation
- Story 1.18: Accessibility and focus-management floor
- Story 1.19: Live-region and reduced-motion behavior
- Story 1.20: English/French localization infrastructure
- Story 1.21: Redaction-safe off-surface affordances and recovery patterns

## Requirements & Constraints

- Every command and query enforces tenant, actor, role, project, and resource authorization. Tenant identity comes from authenticated claims, never client input; denials must not reveal restricted resource existence or metadata. M0 runs one tenant but must be tenant-partitioned by construction so a second tenant is purely additive.
- State mutations pass through one ordered admission path before the EventStore write path. Audit unavailability, unresolved identity or tenant scope, failed authorization, invalid policy, or failed command validation returns a typed failure and writes no durable state.
- Audit is two-phase: a fail-closed pre-commit gate records intent, risk, approval, evidence, policy, correlation, and idempotency context; a post-commit envelope records transition and outcome and reconciles from the event log. Completeness means reconstructability without relying on sensitive logs.
- Idempotency operates at two altitudes — gateway request dedup and aggregate event dedup — and the two are never conflated. Equivalent retries preserve the same observable end state; conflicting reuse is rejected deterministically.
- Lifecycle states, reason codes, command names, and correlation identifiers are contract data. Invalid transitions are rejected before mutation and audited; terminal items get a linked successor rather than rewritten history.
- User-facing failures use a versioned, localized, redaction-safe catalog with an actionable next step. Raw exception text, payloads, PII, credentials, and restricted evidence must not leak through responses, logs, traces, exports, CLI, or MCP.
- Verification is behavioral, not superficial: integration and end-to-end tiers assert state-store end state rather than HTTP status or exit codes. Cross-tenant isolation (nine actor types, including cursors and error bodies), differential conformance across surfaces, architecture fitness, idempotency, and audit tests are release gates.
- Runtime-topology work completes only against an actually started supported topology: documented prerequisites, every required resource reaching its documented healthy or running state, one tenant-bound smoke path executed, and evidence recording the observed resource states and endpoints. Attempted runs, diagnostic substitutes, self-skipped, zero-test, or all-skipped results do not satisfy it; a genuinely unavailable external dependency requires a separately approved, time-bounded exception naming only the blocked lane and its owner.

## Technical Decisions

- ChatBot is an EventStore domain module hosted on the `Hexalith.EventStore.DomainService` SDK. `CommandGateway` mounts as the SDK's pre-commit admission hook and must never become a second command pipeline; governance interfaces stay internal to Server and fitness tests reject replicated admission stages.
- The OpenAPI 3.1 Contract Spine is the single public contract source, with metadata-only problem responses. UI and later CLI/MCP adapters depend only on the typed Client and construct `IChatBotCommand`; they touch no DAPR client, data plane, or gateway internals.
- Solution shape is `.slnx` with strict `Contracts ← Client ← Server` direction, .NET 10 / C# 14, nullable and warnings as errors, and mirrored xUnit v3 test projects plus dedicated architecture and conformance suites. Testing is three-tier: unit, DAPR integration, Aspire end-to-end.
- `Hexalith.Builds` is the sole owner of dependency package versions. Consumer package files are version-free imports; inline versions, overrides, and local package-version properties are governance failures.
- Cross-repository development uses ChatBot root-declared submodules under `references/`, initialized non-recursively; no dependency-owned submodule beneath them is ever initialized. Independent consumer validation uses an isolated standalone checkout at the ChatBot-pinned gitlink, initializing only that root's declared dependencies.
- Local composition uses the retained thin AppHost umbrella (an explicitly recorded exception to full platform composition, never a production hosting bypass). It brings up ChatBot plus DAPR sidecars, the required siblings and Keycloak with health gating, and the UI surface without its own sidecar. DAPR naming is convention-derived and must stay consistent: AppId `chatbot`, EventStore actor/status store `statestore`, ChatBot derived state store `chatbot-statestore`, the dedicated workflow state store, pub/sub `chatbot-pubsub`, topic `chatbot.events`, dead letter `deadletter.chatbot.events`.
- Access control is environment-split by design: production keeps deny-by-default policy under mTLS, while the local self-hosted lane runs mTLS-off against its own separate policy file. Neither posture may be blurred into the other.
- Standalone Aspire and ServiceDefaults projects were retired by host-layer reuse; scaffold references to them are historical. Deployment also requires explicit DataProtection key-ring configuration for the admission marker and query cursor key ring.
- Immutable decision records are superseded, never mutated; live sibling mirrors are version-stamped projections for display only, while authorization gates consult authoritative current state. Correlation travels through commands, events, activities, logs, audit, and status, and long-running work returns an operation identity exposing pending or partial state instead of claiming early completion.

## UX & Interaction Patterns

- Build on FrontComposer and Fluent UI v5 with inherited semantic tokens; do not create a separate ChatBot design system. Status meaning must survive dark mode, forced colors, and non-color presentation.
- Reuse shared project-context, actor, evidence, risk, blocked-state, and status primitives. Risky requests create a reviewable proposal rather than executing from a plain message action; no ungoverned free-text action path may exist.
- Meet WCAG 2.2 AA: full keyboard operation, visible and restored focus, uniquely labelled landmarks, reachable disabled reasons, non-noisy live regions, reduced-motion behavior, and redaction-equivalent exported, copied, downloaded, and read-aloud output.
- Desktop is the full-workflow surface; tablet may stack panels; phone retains safe triage and decision actions. Primary touch targets are at least 44×44 CSS pixels, with the permitted 24×24 floor only for dense controls with adequate spacing.
- Support English and French display text and locale-aware formatting. Stable machine identifiers stay untranslated, and layouts must absorb French expansion without hiding state, risk, next action, or recovery reason.

## Cross-Story Dependencies

Scaffold, dependency-version authority, runtime topology, and CI gates (1.1a–1.1f) enable the Contract Spine; the Contract Spine enables the gateway; authorization, audit, idempotency, lifecycle, redaction, and correlation together complete the path proven by Story 1.9. Within the scaffold split, the solution baseline precedes submodule resolution, which precedes topology wiring, which precedes the CI and release gates that lock the policy in place. Architecture, conformance, isolation, and tenant-fixture stories mechanically verify the same path rather than adding new behavior. UX foundation stories supply inherited behavior for the first governed surface and every later one. All later epics depend on this floor and may extend adapters or workflows only through the same contracts and the same gateway.
