# Epic 1 Context: First Safe Governed Action & Command Spine

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver a runnable ChatBot foundation and one real UI action that prove the shared safety spine end to end: an operation is bound to trusted tenant and actor authority, evaluated against immutable policy, admitted through one fail-closed command path, committed with its idempotency and audit evidence atomically, and exposed through safe status and retry behavior. This establishes the non-bypassable contracts and qualification assets that every later workflow and surface must reuse.

## Stories

- Story 1.1: Scaffold the Runnable Canonical Module Foundation
- Story 1.2: Publish the OpenAPI Contract Spine and Typed Client
- Story 1.3: Bind Every Request to Trusted Tenant and Actor Context
- Story 1.4: Return Versioned Safe Outcomes
- Story 1.5: Establish Stable Operation Identity and Concurrency Contracts
- Story 1.6: Persist the First Isolated Policy Snapshot
- Story 1.7: Admit Commands Through the Single CommandGateway
- Story 1.8: Commit a Governed Mutation and Canonical Audit Atomically
- Story 1.9: Handle Duplicate, Conflicting, and Sensitive Non-Mutating Attempts
- Story 1.10: Complete the First Governed UI Action
- Story 1.11: Create Reproducible Safety Evaluation Assets

## Requirements & Constraints

- Derive tenant, actor, role, Project, and resource authority only from trusted server and current owner evidence. Caller input cannot broaden scope, machine identities cannot inherit human authority, and unresolved or stale evidence fails closed.
- Apply existence-neutral authorization and redaction across API, UI, telemetry, diagnostics, exports, and later machine surfaces. Missing and forbidden resources must not be distinguishable; restricted names, evidence, files, audit details, secrets, PII, and raw exceptions must not leak.
- Give each logical operation a lifetime-stable identity, immutable origin, correlation, stable resource identities, and expected revision or accepted equivalent guard. First commit wins; equivalent duplicates return the stored outcome, while non-equivalent reuse returns a typed conflict without another effect.
- Atomically persist each durable mutation with its event, terminal idempotency outcome, policy and approval references, and canonical audit envelope. Any missing element aborts the write. Sensitive denials, restricted reads, and service-client failures use a separate auditable-attempt record.
- Resolve public failure and waiting states through a versioned English/French safe-message catalog: stable code, headline of at most 80 characters, one safe explanation, terminality, and an allowed next action. Unknown codes use a deny-safe fallback.
- Return authoritative long-running status with identity, state, safe reason, origin, attempts, retry eligibility, partial-output marker, prior outcome, terminal reason, correlation, and safe next actions. Projection lag must never appear as completed work.
- Enforce first-use tenant isolation by construction and prove it with native-store and API negative tests; filters alone are insufficient. Propagate correlation through UI, client, API, gateway, EventStore, audit, publication, projection, and status.
- Accept only consented, verified-redacted, or synthetic evaluation data with provenance, version, expected result, fixture identity, and integrity hash. Sandbox resources and credentials must be tenant-scoped and production-isolated.
- A5, A6, A9a, and A13 remain open evidence gates. Tenant-material persistence requires accepted data-protection evidence, and mutation requires an accepted owner-dispatched atomic target. Synthetic work cannot support pilot, compliance, tamper-evidence, or production-readiness claims.

## Technical Decisions

- OpenAPI 3.1 is the sole public HTTP contract source. Generate the typed Client from it; surfaces use only that client and cannot define competing wire models or access Dapr, stores, aggregates, or gateway internals. Failures use metadata-only RFC 9457 problem details.
- Every mutation enters one `CommandGateway` at the EventStore DomainService pre-commit seam. Authentication, tenant binding, authorization, operation identity, concurrency guard, envelope construction, and atomic-commit participation run once in order. The gateway alone selects a closed effect profile from product metadata.
- Keep governance services internal to Server and preserve the Contracts-to-Client-to-Server/surface dependency direction. Use the canonical `.slnx` module shape, local-only AppHost, and independent architecture, conformance, integration, and browser tests.
- Policy snapshots and historical decisions are immutable and superseded by new versions. Each decision retains the exact snapshot identity and version used; unresolved policy blocks admission.
- Publish and project only committed outcomes. SignalR is advisory, so clients re-query typed status. Verify persisted stream/store end state; tests reject alternate write paths and cover partial failure, concurrency, duplicate/conflicting reuse, cross-tenant access, redaction, rebuild, and recovery.

## UX & Interaction Patterns

- The first governed route lets an authorized user inspect failure and retry only when current authority, state, revision, and retry policy permit it. Confirmation shows target and consequence; retry creates a new predecessor-linked operation rather than resuming the original effect.
- Show admission before implying work began, keep status inline, and use banners or toasts only for deduplicated transition feedback. Re-query authoritative status and preserve state and focus through conflicts, denials, degradation, and terminal outcomes.
- Use the single FrontComposer shell, `FcPageLayout`, `FcPageHeader`, and Fluent UI v5 controls for every live state. Meet WCAG 2.2 AA with keyboard/focus safety, non-color status, reduced motion, 320 CSS-pixel and 400% reflow, touch targets, and English/French parity.

## Cross-Story Dependencies

The runnable foundation enables the contract spine. Trusted context, safe outcomes, operation identity, and policy feed the gateway; atomic mutation/audit then enables duplicate, conflict, and sensitive-attempt handling. The UI consumes those contracts without adding a path, and evaluation assets exercise the completed spine. Every later epic must extend workflows through the same gateway and durability boundary.
