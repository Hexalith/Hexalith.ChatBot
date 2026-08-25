# Good-Spine Rubric Review

## Gate verdict

**REVISE before finalization.** The current-run/single-writer trust split is lean and substantially ratifies the brownfield system, but the central contract-to-producer path invariant is not actually fixed; one high and two medium findings prevent a clean good-spine pass.

No critical findings were identified. Deterministic lint passed with zero findings:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24
ok: true; total_findings: 0
```

## Findings

### HIGH — H1: The producer/contract result-path ownership is unresolved

**Disposition:** Discuss, then autofix the spine once the choice is made.

The spine says the completion job "writes the declared TRX path" and defines current-run paths only as contract-relative paths below the results root (`ARCHITECTURE-SPINE.md:50-54, 70-74`). The brownfield producer does not consume a contract-declared path: it always writes `machine-results/recovery-primary/live-recovery-validation.trx` (`.github/workflows/ci.yml:232-247`). Policy pins the recovery lane, selector, and `current-run` source, but not that TRX path (`StoryEvidenceValidator.cs:237-313`); locator validation merely requires `file:<whatever-trx-the-contract-declared>` (`TrxEvidenceReader.cs:280-295`). A contract author can therefore make a policy-valid path choice that the producer never writes, causing failure only after the destructive lane runs.

The multiplicity model is also implicit. The CI entry point rejects result/provenance path sharing across active contracts (`Program.cs:167-180`), while the workflow emits only one fixed recovery TRX. The spine does not say whether an event may contain at most one active `recovery-primary` completion or how separate active contracts receive separate producer outputs.

This is exactly a level-below divergence point: the workflow implementer and evidence-contract author can make locally reasonable, incompatible choices. Prefer ratifying the existing brownfield shape by fixing the exact recovery TRX path and allowing at most one active recovery-primary contract per transition, then enforcing both in policy. The alternative is to require the workflow to derive safe per-contract output paths and define whether it executes once or per contract. AD-2 and the Current-run paths convention must state whichever model is selected.

### MEDIUM — M1: Cleanup failure does not have the promised stable TE-2 reason code

**Disposition:** Autofix the wording; add a new reporting mechanism only if stable cleanup diagnostics are an actual requirement.

The Failure convention says cleanup failure fails closed "with existing stable reason codes" (`ARCHITECTURE-SPINE.md:75`). The TE-2 reason-code set has no cleanup code (`GateReason.cs:6-34`). A failed `dapr uninstall --all` step stops the workflow before the `ci` entry point (`.github/workflows/ci.yml:248-250, 293-300`), so the red check has workflow-step diagnostics but no metadata-only gate report reason. The independent recovery/A10 gate's `{job}:cleanup_incomplete` token belongs to the separate trust purpose fixed by AD-3 and is not a TE-2 reason code.

Split the convention into two enforceable outcomes: validator/attestation failures use the existing TE-2 stable reason codes; producer, timeout, and DAPR-cleanup failures leave the required workflow check red with step-level diagnostics. This preserves the correct fail-closed behavior without promising an artifact the implementation does not produce.

### MEDIUM — M2: AD-4 overstates enforcement across the raw evidence surface

**Disposition:** Discuss. Either narrow the rule's surface or bind an enforceable raw-TRX sanitization/producer rule.

AD-4 binds uploaded recovery evidence and says no message body, attachment content, credential, token, secret, prompt, or raw claim may enter the evidence surface (`ARCHITECTURE-SPINE.md:62-66`). Strict JSON grammar and metadata-value checks do protect contracts, provenance, and reports (`EvidenceJson.cs:340-417`). The completion workflow also uploads the raw recovery TRX (`.github/workflows/ci.yml:301-310`), however, and `TrxEvidenceReader` validates its XML shape, counters, times, and selectors without validating or redacting arbitrary test-output content (`TrxEvidenceReader.cs:119-242, 438-456`). No actual payload leak was found in this review, but the adopted rule does not presently prevent one from being introduced by a future live-test change.

If AD-4 is meant only for contract/sidecar/report JSON, say so and separately state the producer's raw-TRX responsibility. If the whole uploaded evidence surface is in scope—as its current wording says—bind the live producer to metadata-only test output and name the enforcement seam (for example, a sanitizer or a structural rejection test covering TRX output nodes).

### LOW — L1: Artifact retention is an operational decision left implicit

**Disposition:** Autofix by ratifying the existing value, or explicitly defer it with a revisit condition.

The flow ends in immutable upload and AD-2 requires upload, but the spine does not bind retention or always-run upload semantics. Brownfield CI uses `if: always()` with 30-day retention (`.github/workflows/ci.yml:301-310`), and the operational recovery artifacts use the same duration. This omission does not weaken same-run validation, but it lets the review/audit availability promised by the flow drift independently across workflows. A small convention such as "upload on success or failure; retain for 30 days" would close the operational envelope without expanding the spine materially.

## Checklist assessment

| Good-spine check | Result | Assessment |
| --- | --- | --- |
| Fixes the real divergence points for the level below and misses none | **Fail** | H1 leaves the central contract/producer path and active-contract multiplicity unresolved. |
| Every AD Rule is enforceable and prevents its stated divergence | **Partial** | AD-1 and AD-3 pass. AD-2 is internally sound except for H1. AD-4 is clear for strict JSON evidence but overbroad for raw TRX (M2). |
| Nothing under Deferred could let two units diverge | **Pass** | Retained completion is excluded with a concrete revisit condition; hosted headroom and authenticity/A10 are outcome questions rather than competing implementation choices. |
| Named technology is verified-current | **Pass** | Pins agree with `global.json` and the workflow: .NET SDK 10.0.302, upload-artifact v7 / download-artifact v8, the checksum-pinned Dapr CLI script (`setup-dapr` was retired), and the 360-minute job budget. (Corrected 2026-08-25: this row still named the pre-remediation v4/v2 stack that the remediation section below had already replaced.) The run memlog records the 2026-08-24 GitHub Actions verification. |
| Ratifies rather than contradicts the brownfield codebase | **Partial** | AD-1, AD-3, attestation ordering, permissions, and failure closure ratify the implementation. H1 and M1 contradict concrete producer/reporting behavior. |
| Covers the driving capability/spec | **Pass within declared scope** | The spine covers Story 12.15's recovery-primary completion provenance and its retained operational-evidence boundary. Recovery authenticity, production equivalence, and A10 recalibration are explicitly outside this slice. |
| Does not weaken or contradict inherited invariants | **Pass** | No formal parent `ARCHITECTURE-SPINE.md` is inherited. The companion architecture's current-run-only recovery-primary rule and scheduled/release separation agree with AD-1 through AD-3. |
| Every owned dimension is decided, deferred, or open | **Partial** | Paradigm, trust boundary, sequencing, state mutation, permissions, failure closure, stack, and environment boundary are covered. Path ownership/multiplicity (H1) and artifact retention (L1) leave two operational decisions implicit. |

## What is already strong

- The named pipes-and-filters/single-writer paradigm carries the trust model economically.
- AD-1 precisely binds source and locator semantics to the stable `primary_path_not_executed` outcome.
- AD-3 cleanly prevents operational/A10 evidence from being promoted into completion evidence.
- The Deferred section has real revisit conditions and does not conceal an alternate implementation path.
- The stack is pinned, the Mermaid flow carries the essential sequence, and lint found no placeholders, duplicate IDs, malformed AD fields, or unpinned stack entries.

## Recommended gate resolution

Resolve H1 before finalization. M1 is a small accuracy edit. Decide whether AD-4 covers raw TRX; if it does, name its enforcement seam. L1 can be ratified in one convention line. After those changes, rerun the deterministic lint and the rubric gate.

## Remediation re-review — 2026-08-24

### Final verdict

**PASS.** H1, M1, M2, and L1 are closed in both the final spine and the relevant brownfield enforcement seams. No new blocking good-spine finding was identified.

### Prior-finding dispositions

#### H1 — CLOSED: producer/contract path ownership and multiplicity are now canonical

AD-1 now fixes the exact recovery TRX, provenance, and locator values and permits at most one active recovery consumer. The pinned policy carries those paths, `StoryEvidenceValidator.ValidatePinnedPolicy` treats them as part of the recovery binding, and `CompletionProductionPlanner` rejects an alternate source/path/selector/declaration or a second recovery consumer before returning `RequiresRecovery`. The workflow runs the `plan` command before any DAPR setup and consumes only that validated plan. The producer's raw output is subsequently projected into the same canonical path the policy and contract require.

Evidence: `ARCHITECTURE-SPINE.md` AD-1/AD-2 and Current-run paths; `story-evidence-policy.json:167-190`; `StoryEvidenceValidator.cs:300-390, 760-766`; `CompletionProductionPlanner.cs:17-146`; `.github/workflows/ci.yml:179-227, 248-254`.

#### M1 — CLOSED: stable-code and workflow-failure diagnostics are accurately separated

The Failure convention now promises stable TE-2 reason codes only for validator/attestation failures. Producer, timeout, and DAPR-cleanup failures are correctly specified as red GitHub checks with bounded step diagnostics and no unchecked raw upload. This matches the executable workflow: cleanup runs with `always()`, downstream validation is not allowed past a failed cleanup, and the final upload path excludes raw recovery results.

Evidence: `ARCHITECTURE-SPINE.md` Failure convention; `.github/workflows/ci.yml:228-254, 297-314`; `GateReason.cs:6-34`.

#### M2 — CLOSED: raw recovery diagnostics are outside the evidence surface

AD-4 now distinguishes raw diagnostics from completion evidence and binds `RecoveryTrxSanitizer` as the sole recovery-TRX publication owner. The live test writes beneath `runner.temp/raw-recovery-results`, outside every artifact upload path. The sanitizer constructs a new allowlisted TRX containing only exact test identity, safe ID, times, reconciled counters, and outcome; it does not copy arbitrary input nodes. The uploaded recovery path contains only that projection and its sidecar. A focused mutation test injects `Output`, bearer/secret, and payload text and proves none survives.

Evidence: `ARCHITECTURE-SPINE.md` AD-4; `.github/workflows/ci.yml:228-254, 305-314`; `RecoveryTrxSanitizer.cs:8-142`; `StoryEvidenceGateTests.cs:2097-2143`.

#### L1 — CLOSED: publication and retention semantics are explicit

The Retention convention fixes always-run upload when artifacts are present and a 30-day deletable/expiring GitHub archive. The completion artifact step uses `if: always()` and `retention-days: 30`, matching the rule. Deferred now also states the exact post-upload identity limitation and revisit condition rather than implying indefinite immutability.

Evidence: `ARCHITECTURE-SPINE.md` Retention convention and Deferred; `.github/workflows/ci.yml:305-314`.

### Re-run evidence

- Deterministic spine lint: **PASS**, zero findings.
- `Hexalith.ChatBot.StoryEvidenceGate.Tests`: **195 passed, 0 failed, 0 skipped**.
- `bash -n .github/scripts/install-dapr-cli.sh`: **PASS**.
- `actionlint .github/workflows/ci.yml`: **PASS**.
- Current platform pins independently checked against the official release pages: `actions/upload-artifact@v7`, `actions/download-artifact@v8`, and Dapr CLI/runtime 1.18.0 are current published majors/releases on 2026-08-24.
- Downloaded Dapr CLI 1.18.0 Linux archive SHA-256: **`2a94739e0aa101289d88418225319562bc6800db273b3d9cf819a0efd1ea1bfe`**, exactly matching the spine and installer pin.

### Non-blocking hygiene note

The workflow comment at `.github/workflows/ci.yml:105-106` still describes a five-hour inner allowance and a 60-minute remainder. Executable bounds and AD-5 now deliberately use a 265-minute in-process deadline, 280-minute producer step, and 30-minute cleanup/publication reserve. Updating that stale comment would reduce reader confusion, but it does not weaken the enforced reserve or the architecture-spine verdict.
