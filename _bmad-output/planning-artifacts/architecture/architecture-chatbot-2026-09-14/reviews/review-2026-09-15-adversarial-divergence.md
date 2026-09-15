---
title: Adversarial Two-Compliant-Units Divergence Review
date: '2026-09-15'
reviewedArtifact: ../ARCHITECTURE-SPINE.md
reviewedSha256: 9155211949746540ec7b8bb76113fe8c49c50b9236705a2e8399ef55b65d4bf3
reviewedLines: 595
reviewedStatus: final
lens: configured-adversarial-divergence
result: pass
criticalFindings: 0
highFindings: 0
mediumFindings: 0
lowFindings: 0
resolvedHighFindings: 4
finalizationBlocked: false
---

# Adversarial Two-Compliant-Units Divergence Review — 2026-09-15

## Verdict

**PASS — zero active findings.** The four high-severity convergence holes found in the initial pass are resolved. The amended spine now fixes classifier-stage applicability, durable indeterminate-attempt identity and successor ownership, correction-manifest ownership/item identity/disposition authority, and one immutable gate-set publication/consumption boundary. Re-running the original two-unit constructions produced no remaining incompatible implementation pair.

The deterministic lint pass succeeded with zero findings. This review does not close A5, A6, A9a, A10, A11-M1, A11-M2, or A13 and does not make an implementation or release-readiness claim.

## Attack method

For each AD, the reviewer constructed two independently delivered units one level below the feature spine—for example Gateway versus workflow handler, correction aggregate versus owner adapter, or qualification producer versus release/runtime consumer. A finding was retained only when both units could quote the spine's rule as support yet exchange incompatible state, ownership, or mutation semantics. The finalized PRD/addendum were consulted to avoid treating their closed row-level contracts as missing architecture.

## Post-remediation recheck

| Original attack | Amended invariant | Recheck result |
| --- | --- | --- |
| H1 — universal classifier invocation versus profile-specific application | AD-2 now makes only authentication, tenant binding, authorization, operation identity, revision/concurrency, and envelope construction universal. `CommandGateway` alone selects exactly one closed PRD/addendum profile; risk and approval run only when that profile declares them, and unknown/absent/duplicate maps reject (`Spine:L94-L106`). | **RESOLVED.** Association/correction/governance/projection units cannot choose their own classifier behavior, and an A9a outage cannot accidentally become a non-AI mutation gate. |
| H2 — incompatible identity/link for the no-idempotency indeterminate attempt | AD-7 assigns the attempt and link to the CommandGateway/auditable-attempt seam, fixes the original operation ID, correlation, classifier tuple, reason/remediation, fresh successor operation ID, and immutable predecessor reference, and makes the attempt ledger the sole durable non-authorizing record (`Spine:L201-L212`). | **RESOLVED.** API, audit, and remediation units share one join identity without creating domain/idempotency state or an approval/retry authority. |
| H3 — competing correction manifest/state owners and acknowledgement keys | AD-9 makes the ChatBot correction aggregate the sole manifest/lifecycle owner, fixes a stable item identity tuple, delegates owner-record/effect authority to sovereign contexts, requires each A13 mapping to name the only acknowledging/dispositioning adapter/actor, and forbids coordinator/projection self-acknowledgement or membership changes (`Spine:L253-L265`). | **RESOLVED.** Concurrent acknowledgement, deduplication, irreversible disposition, and final transition have one canonical owner and identity. |
| H4 — release/runtime evaluate different A9a/A11 record sets | AD-12 assigns Release Governance one immutable conforming registry and one exact-candidate/increment `gate_set_id`; consumers recompute status from its immutable records and current candidate/environment at use time; publication/consumption are all-or-nothing; CI/release and runtime enablement consume the same set; copied/local status cannot grant authority; and drift requires a new immutable set (`Spine:L342-L348`). | **RESOLVED.** A9a runtime disablement and A11-M1/A11-M2 release evaluation cannot mix candidates, records, expiry, reopen state, or stale copied status. |

The deterministic spine linter was rerun against the amended 595-line artifact and again returned zero findings.

## Resolved high findings — initial attack record

### H1 — AD-2 left two incompatible interpretations of classifier and approval-stage applicability — RESOLVED

**Evidence:** `ARCHITECTURE-SPINE.md:94-100,186-218`; source baseline delegation at lines 67-80.

AD-2 says every mutation “performs action-risk classification” and “validates approval,” and prohibits omitting or reordering a stage. AD-7 defines the AI-specific risk result, while the normative addendum's closed admission profiles make risk/approval conditional and expressly exclude them from non-AI profiles. The spine does not say whether a non-AI mutation must invoke the classifier, execute a typed no-op stage, or omit the stage through centrally selected profile applicability.

**Two compliant readings:**

- Gateway unit A invokes `ActionRiskClassifier` for every association, correction acknowledgement, policy, safety-control, projection, and data-rights mutation, treating AD-2 literally. Missing A9a/runtime classification then returns `classifier-indeterminate` and blocks those operations.
- Gateway unit B centrally selects the normative profile and does not invoke risk/approval for non-AI commands, treating the stage as present-but-not-applicable and AD-7 as the only classifier contract.

The units disagree on whether an A9a outage blocks correction, safety, projection, and governance commands. That is a conflicting mutation path, not harmless implementation freedom.

**Required closure:** Tighten AD-2 so the universal stages are authentication, tenant binding, the operation-specific authorization row, stable identity, revision/concurrency, and canonical-envelope construction. Require the gateway—not adapters or handlers—to select exactly one closed addendum admission profile, and apply risk/approval only when that profile declares them. Define non-applicability as profile selection, never a classifier call or adapter omission.

### H2 — The no-idempotency indeterminate attempt had no single durable identity/link owner — RESOLVED

**Evidence:** `ARCHITECTURE-SPINE.md:121-128,198-204,440-449`.

AD-7 correctly says `classifier-indeterminate` creates no durable domain or idempotency state and retains only a separately typed redacted non-mutating auditable attempt. It also requires remediation to create a new linked operation. The spine does not bind the attempt's stable identity, who owns that identity, which identity the successor links to, or whether the attempt must be durably accepted before the response can expose remediation. AD-3 defines audit-outage failure, but not this cross-unit identity contract.

**Two compliant implementations:**

- Gateway unit A makes the rejected request's `operation_id` the immutable attempt identity, commits the auditable attempt before returning, and puts `predecessor_operation_id` on the remediated request.
- Audit unit B creates its own `attempt_id`, keys by correlation, records asynchronously through its durable path, and links the remediated operation to the attempt ID rather than the original operation ID.

Both create no idempotency state, retain only an attempt, and create a “linked” new operation, but the API, audit query, and successor workflow cannot join the records reliably.

**Required closure:** Add an AD rule assigning the auditable-attempt record and its immutable identity to one owner. Bind the predecessor/successor identity relation used by the result contract and audit query, require durable attempt acceptance before returning the remediable result, and keep that identity explicitly outside every domain/idempotency store. The exact DTO can remain OpenAPI-owned; the ownership and identity relation cannot.

### H3 — The manifest aggregate, item identity, and disposition authority were not fixed — RESOLVED

**Evidence:** `ARCHITECTURE-SPINE.md:233-254,358-372,443-449`.

AD-9 fixes the complete manifest population and final states. AD-15 says a ChatBot aggregate owns orchestration intent/status, but it does not select the sole correction aggregate or make that aggregate the exclusive owner of the frozen manifest and `Correcting | CorrectionDelayed | Corrected` transition. No stable manifest-item identity/equivalence rule is fixed, and no rule identifies who may record the security-significant `contained`, `compensation-required`, or `cannot-repair` disposition.

**Two compliant implementations:**

- Association unit A stores one immutable manifest in the association-correction aggregate, keys items by owner context + resource/effect identity + source version, and accepts only owner-issued acknowledgements through expected-revision commands.
- Workflow unit B stores manifest rows in coordinator state, keys them by store/type labels, lets a ChatBot worker record irreversible dispositions, and drives the association aggregate from a completion projection.

Both freeze the enumerated population, record acknowledgements/dispositions, keep AI context blocked, and eventually emit `Corrected`; they disagree on deduplication, concurrent final-item handling, disposition authority, and which state is canonical. The second path can also make a projection a de facto mutation owner despite AD-9's display-only projection rule.

**Required closure:** Name one ChatBot association-correction aggregate as sole owner of the immutable manifest and lifecycle. Fix a stable versioned manifest-item identity/equivalence tuple, require every acknowledgement/disposition to enter through `CommandGateway` with expected revision and authenticated named authority, and allow only that aggregate to decide the final-item transition. Owner repair execution remains in the sovereign context under AD-15; only acknowledgement/disposition truth returns to the correction aggregate.

### H4 — No single gate-decision publication and consumption boundary existed — RESOLVED

**Evidence:** `ARCHITECTURE-SPINE.md:295-330,423-436,548-562`; normative gate-record delegation at lines 65-80.

AD-12 correctly imports immutable gate records and fixes the A9a/A11-M1/A11-M2 sequence, current states, and non-substitutability. The normative addendum fixes the record fields and status algorithm. The spine does not assign one component as the gate-status evaluator/publisher, define one exact-candidate gate-set snapshot consumed by release logic, or state how the runtime A9a disable decision observes the same immutable record/successor/revocation ordering. AD-19's durable runtime-control view covers safety controls and rate limits but does not bind qualification-gate currentness.

**Two compliant implementations:**

- Qualification unit A selects the latest individually valid record per gate at release-job start, publishes no combined snapshot, and treats A9a as a deployment-time check.
- Runtime/release unit B recomputes each status just in time from evidence and expiry, dynamically disables A9a artifacts, and evaluates A11-M1/A11-M2 later against a potentially different candidate/dependency set.

Both consume immutable conforming records and require every named status to be `approved-current`, yet a successor, revocation, expiry, or candidate drift between evaluations can yield a mixed gate set: runtime A9a enabled against one record while release A11 is judged against another candidate, or M2 revalidation using a different A11-M1 baseline.

**Required closure:** Bind a single gate-decision authority that deterministically orders immutable records and publishes an immutable, signed/versioned gate-set decision for one exact candidate, dependency set, environment/profile, and evaluation instant. Release consumes that set atomically. Runtime detector/classifier admission consumes a fail-closed A9a currentness projection derived from the same authoritative decision stream, with bounded freshness and revocation/expiry behavior. Keep A11-M1 and A11-M2 as separate records in the set; neither may be synthesized from the other's rows.

## Requested-focus confirmation

| Focus | Confirmed invariant | Residual divergence |
| --- | --- | --- |
| Classifier containment | Only determinate `low-risk|approval-required` continues; all named indeterminate conditions return `classifier-indeterminate`; no proposal, domain/idempotency state, approval, or effect; remediation is a new linked operation | None; profile applicability and attempt/link ownership are now fixed. |
| Correction state/population | Exact `CorrectionDelayed`; full ChatBot, Conversations/Folders, action, message, conversion, mail, tool/effect, disclosure, and irreversible-disposition manifest; AI context blocked to `Corrected`; no `Proposed` association state | None; aggregate ownership, item identity, and disposition authority are now fixed. |
| A9a | Exact artifact qualification gates first use and is revalidated at M1/M2; status remains open | None; runtime and release consume the same immutable gate set. |
| A11-M1 | Independent approved-current M1 record freezes the mandatory SM8/SM16/SM-C3/SM-C5 contract and records SM12/SM15 | None; exact-candidate gate-set consumption is atomic. |
| A11-M2 | Independent exact-candidate SLO qualification; unsupported rows block M2 and narrower claims | None; it remains independent inside the same atomic gate set. |

## Other AD attack results

| AD group | Result |
| --- | --- |
| AD-1, AD-17, AD-18 — dependencies, host, and wire authority | Convergent: one inward dependency direction, one DomainService admission hook, local-only AppHost, and OpenAPI-only HTTP wire authority prevent alternative surface pipelines. |
| AD-3, AD-5, AD-6 — authorization, context ownership, bootstrap | Convergent: source contexts remain sovereign, mirrors do not authorize, and bootstrap has one command path. |
| AD-4, AD-8 — family lifecycle/retry and mailbox authority | Convergent: normative family/profile tables, stable identities, retry registry, authenticity evidence, and execution-time outbound revalidation are binding. |
| AD-10, AD-13 — canonical audit and replay | Convergent: mutation audit remains atomic; indeterminate attempts have one separate non-authorizing identity/owner; replay and its gate-owned verifier have disjoint credentials and authority. |
| AD-11 — recovery evidence | Convergent: diagnostics, completion, and A10 operational evidence are disjoint and activation remains pending. |
| AD-14, AD-16, AD-19 — dependency drift, parity/streaming, runtime control | Convergent: material changes recheck, surface origin remains the only parity difference, streaming state is query-authoritative, operational controls fail closed, and runtime enablement consumes the Release Governance gate set. |

## Open gates are not findings

The current `OPEN`/`OPEN / provisional`/`OPEN / unsupported` statuses are correct. A5/A6/A13 still block M0, A9a blocks affected detector/classifier first use and later revalidation, A11-M1 blocks M1, and A10/A11-M2 block M2 after lower-gate revalidation. The four findings concern how independently built units converge on those contracts; they do not assert that any evidence gate can currently close.

## Final disposition

**PASS.** AD-2, AD-7, AD-9/AD-15, and AD-12/AD-19 now bind stage applicability, attempt linkage, correction-manifest authority, and atomic gate-set consumption without renumbering. The amended spine remains mechanically valid and now passes the configured adversarial convergence gate. All named evidence/approval gates remain open exactly as documented.
