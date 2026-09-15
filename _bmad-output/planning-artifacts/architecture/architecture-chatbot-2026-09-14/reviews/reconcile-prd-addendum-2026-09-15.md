# Targeted PRD/Addendum Reconciliation — 2026-09-15 Architecture Update

## Scope and verdict

This review compares the finalized
[`prd.md`](../../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md) and approved
[`addendum.md`](../../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md) against both maintained architecture
surfaces:

- [`architecture.md`](../../../architecture.md)
- [`ARCHITECTURE-SPINE.md`](../ARCHITECTURE-SPINE.md)

The review is intentionally restricted to:

1. `ActionRiskClassifier` outcomes and fail-closed containment;
2. association lifecycle plus correction manifest/state semantics; and
3. A9a, A11-M1, and A11-M2 increment gating.

**Initial verdict: FAIL — two blocking contract ambiguities remained.** Association lifecycle/correction and split increment
gating pass. The risk-classifier correction is substantially present, but both surfaces weaken the normative ban on
durable idempotency state for `classifier-indeterminate`, and the detailed architecture retains a stale numeric
confidence requirement for every AI proposal. This initial verdict is superseded by the recheck at the end of this
report.

No source artifact was edited, and this review makes no implementation, qualification, pilot, or release-readiness
claim.

## Result by contract and surface

| Contract | `architecture.md` | `ARCHITECTURE-SPINE.md` | Result |
| --- | --- | --- | --- |
| Successful determinate risk classes; pre-classification dispositions; mandatory approval effects; indeterminate outcome and remediation | Correct except for B1 and B2 | Correct except for B1 | **BLOCKED** |
| Exact association state vocabulary and family separation | Exact `CorrectionDelayed`; no `Proposed`; PRD matrix remains sole authority | Exact `CorrectionDelayed`; no `Proposed`; family rows remain authoritative | **PASS** |
| Complete correction manifest, acknowledgement/disposition, AI-context block, delayed state, and terminal completion | Complete and atomically frozen | Complete and atomically frozen | **PASS** |
| A9a first-use/revalidation and disable behavior | Preserved as open and exact-artifact-bound | Preserved as open and exact-artifact-bound | **PASS** |
| A11-M1 as an independent M1 gate | Preserved with mandatory metrics and evidence fields | Preserved with mandatory metrics and evidence fields | **PASS** |
| A11-M2 as an independent exact-candidate M2 gate | Preserved as `unsupported`; A10 and A11-M2 additionally block M2 | Preserved as `unsupported`; A10 and A11-M2 additionally block M2 | **PASS** |

## Blocking findings

### B1 — `classifier-indeterminate` does not prohibit every durable idempotency state

**Normative contract.** The addendum says the result creates no proposal **or durable idempotency state** and cannot
be approved ([addendum.md L64-L70](../../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#L64-L70)). The PRD
repeats that absolute prohibition in the classification table, FR39, and the fail-closed matrix
([prd.md L1054-L1069](../../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#L1054-L1069),
[prd.md L1204-L1209](../../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#L1204-L1209)). Only the separately typed,
redacted, non-mutating auditable-attempt record may be retained.

**Architecture divergence.** Both architecture surfaces instead say the result creates no “durable domain or
idempotency **success**”
([architecture.md L746-L755](../../../architecture.md#L746-L755),
[ARCHITECTURE-SPINE.md L194-L203](../ARCHITECTURE-SPINE.md#L194-L203)). The same weakened phrase is repeated in the
detailed enforcement rule ([architecture.md L892-L898](../../../architecture.md#L892-L898)). This can be implemented
as permission to persist a terminal failure in the idempotency store, which the normative source explicitly forbids.

**Required correction.** In both surfaces, replace the weakened containment phrase with: “creates no proposal,
durable domain state, or durable idempotency state; exposes no approval action; and performs no effect.” Keep the
separate auditable-attempt record and its identity distinct from the idempotency store. Retain the requirement that
remediation creates a new linked operation and never resumes, reinterprets, or approves the former attempt.

### B2 — Detailed architecture still assigns numeric confidence to every AI proposal

**Normative contract.** `ActionRiskClassifier` is categorical and “does not emit a numeric confidence score”
([addendum.md L60-L68](../../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#L60-L68)). Its successful determinate
output is only `low-risk|approval-required`; its proposal evidence uses the classifier version/input tuple and
categorical result, not an association-style threshold band.

**Architecture divergence.** The detailed architecture says every AI proposal must carry “its confidence”
([architecture.md L283-L287](../../../architecture.md#L283-L287)) and then requires every proposal/candidate to carry
`confidenceScore`, `thresholdBand`, and `kernelVersion`
([architecture.md L879-L882](../../../architecture.md#L879-L882)). That stale cross-cutting shape conflicts with the
otherwise-correct categorical risk-classifier section and invites a shared association/risk schema that the addendum
forbids. The concise spine does not repeat this stale numeric-confidence rule.

**Required correction.** Scope `confidenceScore`, `thresholdBand`, and the association kernel version to association
candidates, and retain detector confidence only on task-intent records. Specify that AI action proposals carry the
categorical risk class, classifier version, and input tuple, with no numeric risk-classifier confidence or association
threshold band.

## Passing contract verification

### Action-risk outcomes and containment apart from B1/B2

Both surfaces correctly preserve:

- `low-risk` and `approval-required` as the only successful determinate classifier classes;
- `denied` and `unsupported` as separate pre-classification dispositions;
- mandatory, non-downgradable `approval-required` classification for state mutation, file disclosure, external send,
  task creation/assignment, external-tool invocation, and acting on behalf of a participant;
- `classifier-indeterminate` for missing, invalid, unqualified, failed, or non-contract artifacts/outputs, missing
  tags, unknown effect surfaces, and undeclared authority classes;
- no proposal, approval action, or effect; a separate redacted non-mutating auditable attempt; and
- `classifier-unavailable` only as a non-canonical safe availability reason, followed by a new linked operation after
  remediation rather than approval or resumption of the failed attempt.

Evidence:
[`architecture.md` L739-L773](../../../architecture.md#L739-L773),
[`architecture.md` L864-L872](../../../architecture.md#L864-L872), and
[`ARCHITECTURE-SPINE.md` L186-L218](../ARCHITECTURE-SPINE.md#L186-L218).

### Association lifecycle and correction

Both surfaces use the exact association family states `Received`, `Associated`, `Rejected`, `Deferred`,
`NeedsReview`, `Failed`, `Skipped`, `Correcting`, `CorrectionDelayed`, and `Corrected`; neither reintroduces `Proposed`.
They leave transition, terminal, reprocessing, and successor semantics with the PRD's authoritative Shared Workflow
Contract.

Both surfaces also require correction to enter `Correcting` only after the complete impact manifest is atomically
frozen. The enumerated scope includes every ChatBot-derived store, affected Conversations/Folders records and indexes,
approved or executed actions, appended messages, task-intent conversions, sent mail, external/tool effects, file
disclosures, and required irreversible-effect dispositions. Every item requires an authenticated owner
repair/rebuild acknowledgement or explicit `contained|compensation-required|cannot-repair` disposition. All affected
source/destination AI context stays blocked until every item completes and the workflow reaches `Corrected`; a missed
SLO transitions to `CorrectionDelayed`, exposes owner/next safe action, and triggers P2.

Evidence:
[`architecture.md` L574-L583](../../../architecture.md#L574-L583),
[`architecture.md` L667-L678](../../../architecture.md#L667-L678),
[`architecture.md` L839-L842](../../../architecture.md#L839-L842),
[`architecture.md` L907-L913](../../../architecture.md#L907-L913), and
[`ARCHITECTURE-SPINE.md` L233-L254](../ARCHITECTURE-SPINE.md#L233-L254).

### A9a / A11-M1 / A11-M2 increment gating

Both surfaces preserve the normative gate sequence and current open posture:

- M0 requires current A5/A6/A13, while the exact A9a M0 detector/classifier records gate each artifact's first use;
- M1 revalidates A5/A6/A13 and exact deployed A9a records and additionally requires an independently machine-readable
  `approved-current` A11-M1 record;
- A11-M1 freezes and evidences definitions, denominators, supported-request mix, provisional targets, minimum
  samples/windows, sources, owners, and pass/fail rules for SM8, SM16, SM-C3, and SM-C5, with SM12 and SM15 in the same
  bundle;
- M2 revalidates A5/A6/A13/A9a/A11-M1 against the changed exact candidate and additionally requires A10 and A11-M2;
- A11-M2 remains independently machine-readable and `unsupported` until every row has its exact-candidate target,
  unit, window, error budget, signal/provenance, route, calibration, and burn evidence; and
- neither architecture completion nor one passing gate substitutes for another gate or for the PRD's sole increment
  table.

Evidence:
[`architecture.md` L66-L80](../../../architecture.md#L66-L80),
[`architecture.md` L696-L708](../../../architecture.md#L696-L708),
[`ARCHITECTURE-SPINE.md` L295-L330](../ARCHITECTURE-SPINE.md#L295-L330), and
[`ARCHITECTURE-SPINE.md` L548-L562](../ARCHITECTURE-SPINE.md#L548-L562).

## Acceptance condition

This reconciliation becomes **PASS** when B1 is corrected in both architecture surfaces and B2 is corrected in
`architecture.md`, followed by a targeted rescan confirming:

- no architecture text permits any durable idempotency state for `classifier-indeterminate`;
- no architecture text assigns numeric confidence or association threshold bands to `ActionRiskClassifier` or its AI
  action proposals; and
- the already-passing association/correction and A9a/A11-M1/A11-M2 contracts remain unchanged.

## Recheck — 2026-09-15

**Final verdict: PASS — zero blocking findings and zero advisories remain within the targeted scope.**

- **B1 resolved in both surfaces:** `classifier-indeterminate` now creates no proposal, durable domain state, durable
  idempotency state, approval action, or effect; the auditable-attempt record remains separate, and remediation still
  creates a new linked operation
  ([architecture.md L747-L756](../../../architecture.md#L747-L756),
  [architecture.md L871-L874](../../../architecture.md#L871-L874),
  [architecture.md L895-L900](../../../architecture.md#L895-L900),
  [ARCHITECTURE-SPINE.md L194-L203](../ARCHITECTURE-SPINE.md#L194-L203)).
- **B2 resolved in the detailed surface:** confidence fields are now scoped to association candidates and task-intent
  results. AI-action proposals and `ActionRiskClassifier` results carry categorical class, classifier version, and
  input tuple; numeric confidence cannot authorize, downgrade, or recover a result
  ([architecture.md L283-L288](../../../architecture.md#L283-L288),
  [architecture.md L881-L885](../../../architecture.md#L881-L885)).
- The rescan found no stale `Correction-delayed` state, no reintroduced `Proposed` association state, and no regression
  in the already-passing correction-manifest or A9a/A11-M1/A11-M2 increment-gate contracts.
