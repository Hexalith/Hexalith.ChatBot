# Epic 1 Context: First Safe Governed Action & Command Spine

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver a deployable `Hexalith.ChatBot` module whose first visible UI action proves the complete safety spine: every durable mutation is authenticated, tenant-bound, authorized, idempotent, fail-closed, audited before and after commit, and attributable to its originating surface. The epic establishes the non-negotiable security, contract, test, and UX foundations inherited by all later product work; every foundation story must either unblock the first governed command or mechanically prove that command is safe.

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

- Every command and query enforces tenant, actor, role, project, and resource authorization. Tenant identity comes from authenticated claims, never client input; denials must not reveal restricted resource existence or metadata.
- State mutations pass through one ordered admission path before EventStore execution. Audit unavailability, unresolved identity or tenant scope, failed authorization, invalid policy, or failed command validation must return a typed failure and write no durable state.
- Audit is two-phase: a pre-commit gate records intent, risk, approval, evidence, policy, correlation, and idempotency context; a post-commit envelope records transition and outcome. Security-sensitive operations must be reconstructable without relying on sensitive logs.
- Idempotency operates at gateway request and aggregate event levels. Equivalent retries preserve the same observable end state; conflicting reuse is rejected deterministically.
- Stable lifecycle states, reason codes, command names, and correlation identifiers are contract data. Invalid transitions are rejected before mutation and audited; terminal-item reprocessing creates a linked successor rather than rewriting history.
- User-facing failures use a versioned, localized, redaction-safe message catalog with an actionable next step. Raw exception text, payloads, PII, credentials, and restricted evidence must not leak through responses, logs, traces, exports, CLI, or MCP.
- The value proof is one trivial allowlisted UI command exercised through the complete path and verified against state-store end state, not merely an accepted HTTP response. Cross-tenant, differential-conformance, architecture, idempotency, and audit tests are release gates.

## Technical Decisions

- Use the Hexalith EventStore domain-service SDK and its write path; `CommandGateway` mounts as the pre-commit admission hook and must not become a second command pipeline.
- The OpenAPI 3.1 Contract Spine is the single public contract source. UI and later CLI/MCP adapters depend only on the typed Client and construct `IChatBotCommand`; governance interfaces remain internal to Server and architecture tests reject replicated gateway stages.
- Follow the domain-module solution shape with `.slnx`, .NET 10/C# 14, warnings as errors, and individually executed xUnit v3 test projects. Cross-repository development uses root-declared sibling submodules only, initialized non-recursively.
- `Hexalith.Builds` owns all dependency package versions. Consumer package files are version-free imports; inline versions, overrides, and local package-version properties are governance failures.
- Local composition uses the retained thin AppHost with DAPR resources `statestore`, `chatbot-statestore`, `chatbot-pubsub`, topic `chatbot.events`, and dead letter `deadletter.chatbot.events`. Production access control is deny-by-default; the local mTLS-off policy is explicitly separate.
- Immutable decision records are superseded, never mutated. Live sibling mirrors are idempotent, version-aware projections used for display; authorization gates consult authoritative current state.
- Correlation travels through commands, events, activities, logs, audit, and status. Long-running work returns an operation identity and exposes pending or partial success instead of claiming completion early.

## UX & Interaction Patterns

- Build with FrontComposer and Fluent UI v5 using inherited semantic tokens; do not create a separate chatbot design system. Status meaning must survive dark mode, forced colors, and non-color presentation.
- Reuse shared project-context, conversation, actor, evidence, risk, blocked-state, and status primitives. Risky requests create a reviewable proposal rather than executing from a plain message action.
- Meet WCAG 2.2 AA: complete keyboard operation, visible and restored focus, uniquely labelled landmarks, reachable disabled reasons, non-noisy live regions, reduced-motion behavior, and redaction-equivalent off-surface output.
- Desktop is the full-workflow surface; tablet may stack panels, and phone retains safe triage and decision actions. Primary touch targets are at least 44×44 CSS pixels; dense controls meet the permitted 24×24 floor with adequate spacing.
- Support English and French display text and locale-aware formatting. Stable machine identifiers remain untranslated, and layouts must accommodate French expansion without hiding state, risk, next action, or recovery reason.

## Cross-Story Dependencies

The scaffold, dependency authority, topology, and CI gates enable the Contract Spine; the Contract Spine enables the gateway; authorization, audit, idempotency, lifecycle, redaction, and correlation complete the path proven by Story 1.9. Architecture, conformance, isolation, and tenant-scoped fixture stories mechanically verify that path. UX foundation stories provide inherited behavior for the first governed surface and every later surface. All later epics depend on this safety floor and may extend adapters or workflows only through the same contracts and gateway.
