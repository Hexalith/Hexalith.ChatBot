# PRD Quality Review — Hexalith.ChatBot

## Overall verdict

**Fair.** The revised PRD has a strong product thesis, explicit safety invariants, honest production-readiness blockers, useful counter-metrics, and a disciplined M0/M1/M2 gate structure. It is not yet launch-grade chain-top input without qualification: four high-severity execution gaps remain around pre-pilot gate evidence, the canonical workflow-state contract, and the identity used to execute the M0 AI command; repeated scope catalogs also retain some stale language that can mislead downstream teams.

The artifact is ready for targeted correction, not wholesale rewriting. Once the high findings are resolved and the duplicate scope summaries are normalized, UX and architecture can source-extract confidently; production approval still remains correctly blocked by A6, A10, and A11 evidence.

## Decision-readiness — adequate

The document states real decisions rather than smoothing them into neutral considerations. The email-first wedge, Project-only MVP boundary, non-downgradable approval effects, deny-by-default allowlist, structural parity invariant, and first production claim at M2 are all explicit. The gate table in §Minimum Release Slice is especially useful because it names audience, evidence, approvers, rollback conditions, and permitted claims.

The main weakness is that two decisions declared to block live pilot behavior are not represented in the gate where that behavior begins. A decision-maker could read the M0 row as complete even though A5 and A6 say otherwise.

### Findings

- **high** Pre-pilot AI-provider and privacy blockers are not part of the M0 gate (§Minimum Release Slice gate table; A5; A6; NFR49a) — M0 includes live AI mediation and a controlled pilot preview, but its mandatory evidence omits A5 provider telemetry/training/retention/region approval and A6's data-class contract. A6 and NFR49a explicitly block pilot onboarding, while the gate table places “A6 privacy decision” only in M2. *Fix:* add A5 and A6 approval evidence, owners, and disable conditions to M0 before any pilot onboarding or live model invocation; keep M2 as the production revalidation/operability gate rather than the first appearance of A6.

## Substance over theater — adequate

The content is overwhelmingly earned. Each named protagonist drives distinct requirements; the innovation claim is modest and explicitly treated as an internal thesis; security, reliability, accessibility, and recovery requirements use product-specific thresholds and failure contracts. The addendum carries mechanism detail that would otherwise overload the PRD.

The remaining weakness is duplication rather than generic boilerplate. The increment scope is restated in §Minimum Release Slice, §Project Scoping, and §Complete Feature Set, and the older summary has already drifted from the stronger canonical wording.

### Findings

- **medium** Repeated increment catalogs have drifted (§Minimum Release Slice versus §Complete Feature Set) — the later M2 must-have summary says recovery “uses an authentic hosted continuity-safety bundle,” reduces replay to a separate tenant plus outbound interception, and says WCAG extends to “M1 surfaces,” while the canonical M2 section correctly says no qualifying hosted bundle exists, requires replay-safe composition/default-deny egress/full invariance, and gates M2 surfaces. *Fix:* make §Minimum Release Slice the only normative increment catalog; convert §Complete Feature Set to a concise pointer or update it mechanically from the canonical gate definitions.

## Strategic coherence — strong

The PRD has a clear bet: email becomes authorized Project work only when association, identity, evidence, approval, command execution, and audit are governed together. M0 proves the vertical trust loop, M1 proves governed human/machine parity, and M2 proves production operation. Feature exclusions follow that thesis rather than convenience.

Success metrics now include stable IDs, explicit pilot windows, association quality, operational adoption, and counter-metrics for unauthorized disclosure, rubber-stamping, and automation-induced reassignment. One meaningful value gap remains in the AI portion of the thesis.

### Findings

- **medium** AI success measures execution, not usefulness (§Measurable Outcomes, SM12-SM13) — review volume and command completion prove governance and reliability, but not whether the AI output actually helps users move work forward. A perfectly executed but repeatedly revised or discarded response can satisfy both measures. *Fix:* add a pilot outcome such as approved-without-revision rate, accepted-result reuse, reviewer-rated usefulness, or request-to-usable-result time, paired with a counter-metric so teams cannot optimize by narrowing supported work excessively.

## Done-ness clarity — thin

The strongest paths are unusually testable: association thresholds and reasons, governed composer outcomes, mandatory approval effects, atomic audit, idempotency identities, recovery evidence boundaries, and many NFRs have measurable consequences. The group-level Functional Acceptance Guidance is also valuable.

However, this is a chain-top PRD with 96 primary FRs plus suffixed requirements. Many FRs still delegate their actual acceptance contract to future scenario decomposition, and the only explicit workflow matrix is internally incomplete. Engineers can infer a plausible implementation, but two teams could implement materially different lifecycle semantics while claiming conformance.

### Findings

- **high** `Proposed` is unreachable in the authoritative association matrix (§Shared Workflow Contract) — the overview says `Received -> Proposed`, and human decisions accept `Proposed` as a source, but `ProposeEmailProjectAssociation` transitions `Received` directly to `Associated` or `NeedsReview`. No row enters `Proposed`, and `MarkEmailAssociationNeedsReview` from the command catalog has no transition row. *Fix:* decide whether `Proposed` is a persisted state. If yes, add the transition into it and the subsequent automatic/human decision transitions; if no, remove it from the state list, graph, command sources, and contracts. Add every mutating association command to the matrix.

- **high** Required non-association state models are missing (§Workflow State, Contracts, and Testability, FR87-FR89) — FR87 requires canonical states for participant resolution, attachment handling, approvals, AI actions, command execution, and audit projection, but the PRD defines a full matrix only for association. Lists such as attachment states and composer admission outcomes do not establish valid transitions, terminality, concurrency, retry, or successor behavior. *Fix:* add compact authoritative matrices for each required workflow family, or narrow FR87 to the state models actually owned here and point to binding versioned contracts for the rest.

- **high** M0 AI command execution has no defined authenticated principal (§Increment M0; §Service Client Permissions; Command Allowlist v0; NFR1/NFR8) — M0 executes `Project.AppendConversationMessage` for an AI action and includes the AI actor in its security matrix, but the only `ai-action-execution-client` identity is introduced in M1. The PRD does not say which M0 principal crosses CommandGateway, how its scope is bound to requester/approval, or how credential expiry/revocation works. *Fix:* define the M0 execution identity and least-privilege grant, or state that the AI actor never authenticates directly and a named M0 mediator principal submits the command with immutable requester, approval, tenant, and Project delegation evidence.

- **medium** Two legacy capability statements weaken absolute safety requirements (§Technical Constraints; FR49) — “Audit-write failure must fail closed for risky actions” is narrower than NFR15a's all-mutations atomicity, and FR49 says the system “can require” outbound approval even though FR41, NFR16, and the risk classifier make external sends mandatory-approval. *Fix:* change both local statements to the canonical absolute behavior, then search for other “can require” or risky-only audit wording that could be implemented permissively.

## Scope honesty — strong

The PRD is candid about what is not proven. It distinguishes pilot preview, governed pilot, and production/release-candidate claims; labels A6, A10, and A11 blockers; exposes unsupported SLO rows; records explicit non-goals; and states that schedule moves before safety controls do. The product-brief deltas for general subjects, user uploads, extra channels, broad automation, and full task lifecycle are visible rather than silently discarded.

The assumptions have named owners and revisit conditions, including the newly explicit A12 external-contract dependency. Open-item density is appropriate for a draft launch-grade PRD because the blockers are phase-gated instead of hidden.

## Downstream usability — adequate

The glossary, source manifest reference, journey-to-FR/NFR table, success trace, high-risk acceptance matrix, UI surface inventory, ownership section, stable requirement IDs, and addendum contracts give UX, architecture, and story creation strong extraction anchors. Named protagonists carry context inline and command/surface terminology is mostly stable.

Downstream use is constrained by the Done-ness findings and by duplicated scope text. The canonical sections are identifiable, but a story author should not have to decide which restatement wins.

## Shape fit — strong

The shape fits a multi-stakeholder, launch-grade B2B SaaS orchestration product. User journeys are load-bearing because contributor, external-party, owner, tenant-admin, developer, reviewer, and system-actor paths impose different authority and recovery requirements. Conditional governance, data-protection, integration, operability, and accessibility sections are justified by the product's actual risks.

The architecture-heavy mechanisms are mostly kept in `addendum.md`, while the PRD retains capability, actor, state, gate, and acceptance intent. The document is long, but its rigor is proportionate to its role as a chain-top artifact spanning UX, architecture, security, operations, and story generation.

## Mechanical notes

- `NFR17a` is defined inside the Functional Requirements section rather than under Non-Functional Requirements, which makes extraction by section unreliable.
- The phrase “25 must-have capabilities” in §Complete Feature Set no longer matches the expanded and grouped bullet inventory.
- The automated parity bullet in §Cross-surface parity outcomes says tests verify “create,” but the singular M1 exit set has no operation named `create`; use the canonical command or capability name.
- Primary FR1-FR96 and NFR1-NFR70 sequences are present, with suffixed additions, but downstream tooling must treat suffixed IDs such as FR28a-FR28f and NFR65a as first-class rather than numeric duplicates.
- Inline `[ASSUMPTION]` markers point to A9a or A11; indexed A1-A12 are also referenced in narrative/gates, though most do not use inline tags.
- Every human user journey has a named protagonist; the System Journey is intentionally actor-system-led.
