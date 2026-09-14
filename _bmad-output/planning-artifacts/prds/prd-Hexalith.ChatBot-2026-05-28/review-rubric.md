# PRD Quality Review — Hexalith.ChatBot

## Overall verdict

The product thesis, named journeys, staged scope, measurable outcomes, and non-negotiable trust floor are unusually substantive. The current PRD/addendum pair is not safe to use as the sole implementation or release authority, however: two binding AI-governance rules permit readings that contradict mandatory approval and least privilege, while late chat-surface and recovery changes have not been reconciled through the full requirement set. Treat the product strategy as sound but the requirements contract as blocked until the critical and high findings below are resolved.

## Decision-readiness — broken

The PRD makes real decisions and exposes real trade-offs: email is the wedge, M0/M1/M2 have a fixed dependency order, machine-surface parity is delayed rather than hidden, and the safety floor is explicitly protected when resources tighten (§Executive Summary; §Minimum Release Slice; §Risk Mitigation Strategy). A decision-maker can understand what the product is betting on and what it gave up.

The binding controls do not yet support an unambiguous green-light decision. The approval policy can be read as allowing tenant admins to downgrade the six classes that FR41 declares approval-required, and the M1 AI allowlist can be read as allow-by-default across a catalog containing outbound and governance mutations. M2 also has explicit release obligations whose current evidence and SLO definitions cannot establish completion.

### Findings

- **critical** The approval-fatigue override contradicts the safety invariant (§Task Intent and AI Action Mediation; FR41; NFR16; `addendum.md` §Tenant Policy Schema) — FR41 requires approval for state mutation, file exposure, external send, task creation, tool invocation, and acting on behalf; the PM note then says admins can “downgrade `low-risk-allowed`” for those same classes, and the schema makes those six risky classes boolean opt-ins. That permits an implementation in which policy bypasses the mandatory approval NFR. *Fix:* Restrict `low-risk-allowed` to enumerated read-only action subtypes; make all six boundary-crossing classes structurally non-downgradable, and add a schema invariant/test that rejects any policy snapshot attempting otherwise.
- **critical** M1's AI-command allowlist is denylist-shaped and can include privileged mutations (§Command and Query Contracts; FR19, FR43, FR75a–FR75g; `addendum.md` §Command Allowlist v1) — the addendum defines v1 as the “full catalog ... minus” commands tagged `disallowed-for-AI`, but the referenced catalog includes `SendApprovedProjectEmail`, `GrantServiceClientPermission`, and `RevokeServiceClientPermission`, and the PRD/addendum never supplies the required per-command tags or an exact safe v1 membership list. This conflicts with the AI Actor boundary and least-privilege thesis. *Fix:* Replace the rule with an exact, versioned, deny-by-default AI-invocable set; enumerate metadata and approval behavior for every member; explicitly mark outbound, identity, policy, allowlist, and admin mutations human/service-only.
- **high** M2 cannot receive an evidence-based release decision from the documented recovery gate (§Increment M2; A10; NFR56–NFR59; `addendum.md` §Recovery-validation commitments) — A10 remains provisional, the cited hosted run predates the required controlled-loss job and expired under the document's own eight-day freshness rule on 2026-09-04, no hosted controlled-loss artifact is cited, and a 180-second lane cannot test the four-hour RTO boundary. *Fix:* State the exact M2 stop-ship gate and attach a fresh hosted controlled-loss artifact plus a full-window or separately evidenced pre-production RTO drill before M2 completion can be declared.
- **high** The published M2 SLO catalog does not meet NFR42a's own completion contract (NFR42a; `addendum.md` §Operating Baselines) — NFR42a requires every SLO to have a target, window, error budget, and alert threshold, but the supposedly published table leaves multiple targets and nearly every error budget as `calibration-pending`; it also says only audit projection lag has a live signal. *Fix:* Either make completion of A11 calibration and live signal wiring an explicit M2 entry gate, or supply bounded provisional values and a ratcheting rule for every required field so the release can be tested before tenant-specific overrides exist.

## Substance over theater — adequate

The material is largely earned: the journeys drive concrete surfaces and controls, the NFRs contain product-specific bounds, the innovation claim is explicitly framed as an internal thesis rather than market fact, and the personas represent distinct authority and workflow boundaries. The main drag is repeated restatement, not empty content.

### Findings

- **low** Repeated risk and ownership sections make canonicality harder to trust (§Key Product Risks to Validate Early; §Risk Mitigations; §Innovation Risk Mitigation; §Risk Mitigation Strategy; §Shared Workflow Contract; §Context Ownership) — the same association, leakage, AI-action, mailbox, and ownership material is restated several times, while §Context Ownership calls itself canonical even though an earlier ownership list remains in full. *Fix:* Keep one canonical risk register and one canonical ownership section; make other sections contain only concise links and genuinely new implications.

## Strategic coherence — strong

The PRD has a clear thesis: external email becomes valuable when it can be converted into authorized, evidence-backed project work rather than merely summarized (§What Makes This Special). The M0/M1/M2 sequence follows that thesis—prove the governed vertical loop, extend structural parity and policy breadth, then establish production operations—and the success measures test association quality, user effort, adoption, AI-action execution, attachment handling, governance friction, and safety counter-signals such as reassignment, rubber-stamping, and unauthorized exposure. The post-MVP deferrals consistently protect this arc.

### Findings

No substantive findings.

## Done-ness clarity — thin

Several high-risk surfaces are specified with exemplary precision, including FR23, FR42, FR67, FR76, FR77, NFR15a, and NFR50a. The quality is uneven across the catalog: generic “can” requirements rely on future scenario decomposition, and core association, risk, policy, and SLO contracts still contain incompatible or placeholder definitions.

An engineer can build many individual pieces, but cannot derive one deterministic acceptance oracle for the whole product without choosing among conflicting source statements or inventing missing policy/SLO data.

### Findings

- **high** Three different classifiers are conflated into one undefined kernel (`addendum.md` §Confidence Thresholds; `addendum.md` §Risk Classifier; FR35; A9a) — association expects a numeric `[0.0, 1.0]` scorer, action risk produces categorical `low-risk`/`approval-required`, and task intent introduces another numeric confidence while pointing to the categorical classifier's “same domain.” FR35 also requires an `actionable` evaluation label that A9a does not define. *Fix:* Define separate AssociationScorer, TaskIntentDetector, and ActionRiskClassifier contracts with their own inputs, outputs, versions, datasets, label taxonomies, calibration targets, and failure states; remove every “same kernel/domain” shortcut unless a shared primitive is explicitly specified.
- **high** Below-`T_low` association has incompatible lifecycle outcomes (§Technical Success; §Shared Workflow Contract; `addendum.md` §Confidence Thresholds) — the PRD says messages below `T_low` are “deferred or rejected,” while the addendum says they enter `NeedsReview`; scorer failure also enters `NeedsReview` but with an empty candidate list. These states have different terminality, ownership, and reprocessing semantics. *Fix:* Add one canonical score/outcome/state table covering success bands, deterministic-signal conflict, no candidate, scorer error, unauthorized evidence, and user decisions; reference that table everywhere.
- **high** Acceptance readiness is promised for every FR group but specified for only four (§Functional Acceptance Guidance; §Functional Requirements) — the PRD says each group must have happy-path, authorization, ambiguity, idempotency, audit, and redaction scenarios, then gives matrices only for FR1–FR12, FR39–FR46, FR55–FR63, and FR81–FR89. Participant/identity, conversation/context, files, outbound, admin/operations, and parts of workflow/recovery remain dependent on downstream invention. *Fix:* Complete the group-level acceptance matrix before story readiness, or explicitly mark each uncovered group blocked with an owner and prerequisite artifact.
- **high** The binding tenant-policy schema is complete only for M0 (`addendum.md` §Tenant Policy Schema; FR52; FR73–FR75g; NFR35) — the addendum requires every knob to declare type, allowed values, safe default, sensitivity, increment, and validation rule, but M1 and M2 are only prose categories. Required controls such as approval routing, allowlist pinning, authenticity strictness, retention, replay, and dashboard visibility therefore have no testable schema contract. *Fix:* Enumerate every M1/M2 knob using the declared schema fields and include cross-knob rejection rules, migration/version behavior, and safe defaults.

## Scope honesty — thin

The May 28 scope is mostly candid: the PRD explicitly narrows the brief to controlled mail, moves scheduled/file/standing automation post-MVP, sequences CLI/MCP behind M0, lists non-goals, and indexes assumptions with owners and revisit conditions. Two later or inherited scope edges are not treated with the same discipline.

### Findings

- **high** The approved interactive-chat scope exists only as a PM note (§Vision (Future), 2026-06-09 note; §UI Surface Inventory; §Functional Requirements) — the note pulls a governed chat composer and FrontComposer shell into Epic 10, but the required UI inventory still has no chat/composer surface, the increment must-haves do not place it, and no FR defines message submission, admission outcome, proposal conversion, failure/retry, or accessibility acceptance. Downstream teams must guess whether S1 includes it and which increment gates it. *Fix:* Reconcile the change into Product Scope, the appropriate increment, a named UI surface, journeys, FRs, NFR60 accessibility scope, traceability, and explicit non-goals; remove the historical “no chat surface” framing once superseded.
- **medium** General user-upload file ingestion disappeared without an explicit scope disposition (§MVP; §Files and Attachments; §Out of scope for MVP) — the source product brief included files added by user upload as well as mailbox attachments, while the PRD consistently specifies mailbox attachment capture and neither commits to nor explicitly defers user upload. *Fix:* Add user upload to a named increment with governed-file acceptance requirements, or name it as a deliberate non-goal/post-MVP item with the same rationale used for the other brief reductions.

## Downstream usability — thin

The glossary, named protagonists, FR/NFR catalogs, lifecycle table, UI inventory, and journey-to-requirement overview give UX, architecture, and story workflows useful extraction anchors. Those strengths are weakened by unpinned brownfield lineage, missing success-metric identifiers, and late decisions that do not round-trip through the declared artifact metadata.

### Findings

- **high** The brownfield source baseline cannot be reproduced from the PRD (§frontmatter `inputDocuments`/`documentCounts`; §Project Classification; §Material-change re-check protocol) — the only direct input is a machine-specific `D:/...` path, `projectContext` is counted as zero, and the later claim to use multiple sibling-module artifacts gives neither exact paths nor revisions. A downstream architect cannot tell which contracts were validated or reproduce the material-change comparison. *Fix:* Replace the machine-specific path, enumerate every source artifact with repository-relative path and revision/date, and record the compatibility/re-check result in the PRD metadata or a versioned companion manifest.
- **medium** Success outcomes have no stable IDs (§Measurable Outcomes; §Traceability Overview) — the PRD supplies many strong numeric measures but never assigns SM IDs, and the trace table maps only UJs, FRs, and NFRs. Stories and release evidence therefore cannot cite a stable metric when wording or ordering changes. *Fix:* Assign stable `SM1...` identifiers, identify primary/counter-metric pairs, and add SM references to the journey/requirement trace overview and increment gates.

## Shape fit — adequate

A multi-stakeholder, security-sensitive, chain-top B2B product warrants named journeys, governance sections, and a deep FR/NFR catalog; that overall shape fits. The artifact has nonetheless accumulated implementation status, story references, commit/run evidence, and code-as-authority statements inside the product contract, blurring the boundary between requirements, architecture, and qualification evidence.

### Findings

- **medium** The PRD/addendum now mix product requirements with mutable implementation evidence and invert requirement authority (`prd.md` A10 and the 2026-06-09 PM note; `addendum.md` §Operating Baselines and §Recovery-validation commitments) — the addendum calls a code catalog authoritative, while long commit/run narratives and story status sit inside documents that downstream workflows expect to source-extract as requirements. This makes a product decision appear stale when an implementation run ages and makes code, rather than the approved requirement, the source of truth. *Fix:* Keep targets, invariants, owners, and release gates in the PRD/addendum; move run locators, freshness, measured results, story completion, and code-catalog status to a versioned qualification-evidence artifact referenced by a short current gate-status field.

## Mechanical notes

- Frontmatter freshness is wrong: `prd.md` says `updated`/`lastEdited: 2026-05-28` although its body includes approved 2026-06-09 and August 2026 changes; `addendum.md` says `updated: 2026-05-28` while §Recovery-validation commitments contains evidence through 2026-08-28.
- `.memlog.md` is absent. The legacy `.decision-log.md` contains important post-final decisions, so the current workflow's canonical memory/audit round-trip is not available.
- The current PRD places `Skipped` in M0 (§Increment M0 and M0 must-haves), while `.decision-log.md`'s 2026-05-28 tightening entry says “only `Skipped` ... is M1.” No later decision-log entry records the reversal.
- FR definitions are unique and base-number-contiguous from FR1 through FR96; NFR definitions are unique and base-number-contiguous from NFR1 through NFR70. Letter-suffixed additions are unique, but range references such as `FR39–FR50` and `NFR49–NFR55` should state whether suffixed IDs are included.
- Every inline `[ASSUMPTION A9a]` and `[ASSUMPTION A11...]` marker resolves to the Assumptions Index. The A9a taxonomy does not round-trip cleanly because FR35 uses the unindexed label `actionable`.
- Every human user journey has a named protagonist (Amira, Marc, Elena, Priya, Nora, Leo, or Sofia); the separate System Journey is appropriately actor-system-focused.
- `addendum.md` §Risk Classifier sends classifier error-rate measurement to NFR50a, but NFR50a defines audit-chain completeness rather than classifier accuracy.
