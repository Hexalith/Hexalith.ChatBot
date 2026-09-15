# Architecture-Surface Reconciliation — 2026-09-15 Change

## Scope and verdict

Reviewed as one atomic architecture unit:

- `../../../architecture.md`
- `../ARCHITECTURE-SPINE.md`

Authority for this focused review is `../../../sprint-change-proposal-2026-09-15.md`, limited to classifier-indeterminate containment, the association correction lifecycle and full impact manifest, and the A5/A6/A9a/A10/A11-M1/A11-M2/A13 release-gate contract.

**Verdict: PASS.** The two surfaces agree on every load-bearing decision introduced by the 2026-09-15 update. No obsolete classifier outcome, association-state literal, unsplit A11 token, gate-closure claim, or cross-surface divergence remains. The sole advisory from the initial pass was corrected and rechecked below.

## Resolved advisory

### A1 — M0 vertical-path summary now includes the applicable A9a first-use prerequisite

**State:** Resolved  
**Artifact:** `architecture.md:806-811`

The target M0 vertical path now states both prerequisites: A5/A6/A13 must close, and the exact A9a M0 detector/classifier artifacts must be `approved-current` before first use. This agrees with the detailed architecture's primary gate ledger and D12 and with spine AD-12 and its release ledger.

## Focused recheck after reviewer fixes

- **A9a flow prerequisite:** PASS. `architecture.md:806-811` now matches `ARCHITECTURE-SPINE.md:297-302`; the formerly incomplete local summary is closed.
- **Classifier idempotency state:** PASS. `architecture.md:751-756,871-874,898` and `ARCHITECTURE-SPINE.md:198-203` all prohibit durable domain and durable idempotency state for `classifier-indeterminate`, while retaining only the separately typed redacted non-mutating auditable attempt. No former `idempotency success` or `successful durable-operation` wording remains.
- **Confidence scope:** PASS. `architecture.md:283-288,881-885` confines numeric confidence/threshold-band evidence to association candidates and task-intent results and keeps ActionRiskClassifier output categorical with class, version, and input tuple. Spine AD-7 independently fixes the association score, task-intent, and categorical risk contracts and contains no numeric-confidence grant for risk classification. The detailed statement is a consistent implementation-facing elaboration, not a competing rule.
- **Regression scan:** PASS. No hyphenated correction state, unsplit A11/A9 token, positive `Proposed` association state, or classifier-availability sibling outcome was introduced.

## Contract parity matrix

| Contract | `architecture.md` evidence | `ARCHITECTURE-SPINE.md` evidence | Result |
| --- | --- | --- | --- |
| Determinate risk classes | Lines 747-756 restrict successful determinate results to `low-risk` or `approval-required`; `denied` and `unsupported` are pre-classification dispositions | Lines 193-204 carry the same class and disposition boundary | PASS |
| Indeterminate inputs | Lines 751-756 map missing, invalid, unqualified, failed, non-contract, missing-tag, unknown-surface, and undeclared-authority cases to `classifier-indeterminate` | Lines 198-203 map the same complete set to the same typed result | PASS |
| Indeterminate containment | Lines 752-756 and 871-874 prohibit proposal/approval identity, durable domain state, durable idempotency state, approval action, and effect; only a redacted non-mutating attempt remains; remediation is a new linked operation and the original is terminal/non-approvable | Lines 199-204 preserve the same containment, attempt, remediation, and non-resumption rules | PASS |
| `classifier-unavailable` status | Lines 754-755 and 871-874 use it only as a subordinate non-canonical safe/availability reason on `classifier-indeterminate` | Lines 201-202 do the same | PASS |
| Exact correction state | Lines 576-580, 668-679, 841-844, and 910-916 use `CorrectionDelayed` | Lines 246-254 use `CorrectionDelayed` | PASS |
| Complete correction manifest | Lines 671-676 enumerate every ChatBot-derived store, affected Conversations/Folders record and index, approved/executed AI action, appended message, task-intent conversion, sent mail, external/tool effect, file disclosure, and required irreversible disposition; lines 910-916 retain the same set as an inclusive summary | Lines 246-251 enumerate the same complete set and acknowledgement/disposition handling | PASS |
| AI-context block | Lines 675-676 and 914-916 block every affected source/destination until all manifest work completes and the correction reaches `Corrected` | Lines 249-253 preserve the same block and completion condition | PASS |
| No `Proposed` association state | Lines 678-679 and 841-844 explicitly exclude `Proposed`; other proposal usages concern AI-action or architecture proposals, not association lifecycle | Line 254 explicitly excludes `Proposed`; other usages are unrelated proposal nouns | PASS |
| Complete open-gate inventory | Frontmatter line 41 and lines 68-78 list A5, A6, A9a, A10, A11-M1, A11-M2, and A13, all open | Frontmatter lines 14-20, AD-12 lines 297-330, and lines 556-562 carry the identical inventory and states | PASS |
| Increment binding | Lines 76-78, 112-113, 292-294, and 789-791 make A11-M1 an M1 prerequisite and A10/A11-M2 additional M2 prerequisites after lower-gate revalidation | Lines 299-330 make the same M0/M1/M2 split and keep A11-M1/A11-M2 independent | PASS |
| Evidence detail | Lines 696-708 freeze the required A11-M1 metric bundle and the exact-candidate A11-M2 SLO bundle; lines 679-695 preserve separate A10 authority | Lines 318-330 and 276-293 preserve the same evidence schemas and independent A10 channel | PASS |

## Stale-token and contradiction scan

- `Correction-delayed`, `correction-delayed`, and other space/hyphen variants: **0 occurrences** in both artifacts.
- Unsplit standalone `A11`: **0 occurrences** in both artifacts.
- Obsolete standalone `A9`: **0 occurrences** in both artifacts.
- `classifier-unavailable`: **3 occurrences total**, all explicitly subordinate to canonical `classifier-indeterminate`.
- Association-state `Proposed`: **0 positive occurrences**; both artifacts explicitly prohibit it.
- All seven named release-gate tokens occur in frontmatter or the primary ledger and remain open; no architecture-completion text claims to close any of them.

## Conclusion

The update is reconciled across the detailed architecture and architecture spine. A1 is resolved, the idempotency-state and confidence-scope fixes are consistent, and no further classifier, correction-manifest, association-state, or release-gate remediation is indicated by this review.
