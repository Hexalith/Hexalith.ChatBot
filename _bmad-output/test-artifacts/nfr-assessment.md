---
stepsCompleted:
  - step-01-load-context
  - step-02-define-thresholds
  - step-03-gather-evidence
  - step-04e-aggregate-nfr
  - step-05-generate-report
lastStep: step-05-generate-report
lastSaved: '2026-08-24'
workflowType: testarch-nfr-assess
inputDocuments:
  - '_bmad-output/implementation-artifacts/12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10.md'
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md'
  - '_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/.decision-log.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md'
  - 'docs/adrs/live-recovery-validation-drivers.md'
  - 'docs/story-evidence-integrity.md'
  - 'story-evidence-policy.json'
  - '.github/workflows/ci.yml'
  - '.github/workflows/release.yml'
---

# NFR Evidence Audit — Story 12.15 recovery evidence and A10

**Date:** 2026-08-24 (see the 2026-08-27 addendum below for the current state)
**Story:** 12.15  
**Overall Status:** **FAIL — release/story-closure blocker, as of 2026-08-24.** *(Superseded in part — see the "Addendum (2026-08-27)" section at the end of this document. The specific hosted-authenticity blocker described below is resolved; A10/NFR56/NFR57/NFR41 numeric ratification remains open, precisely as this audit already anticipated, and is not a bare "PASS.")*

This audit reviewed repository evidence, ran focused local verification, inspected current hosted workflow results, and used four independent security/performance/reliability/scalability evidence audits. It did not start a live fault-injection run.

## Executive Summary

The current-run `recovery-primary` completion path is materially authentic and fail-closed. The pre-Dapr planner now validates every active lane, safe path, locator, selector, skip flag, primary declaration and policy binding before it can authorize Dapr. Direct planner tests cover valid recovery, malformed secondary lanes, recovery multiplicity, result-path collisions, wrong primary bindings and retained locators; sanitizer tests reject DTDs, foreign structural namespaces, duplicate results, failed outcomes, wrong classes, counter mismatches and reversed timestamps.

Hosted authenticity is a FAIL, **as of this audit's 2026-08-24 date** *(resolved 2026-08-27 — see the addendum at the end of this document)*. Hosted recovery attempts have run, but none produced a complete passing recovery artifact. The latest required release run, [32705581758](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/32705581758), failed its live recovery test and independent gate. Its only retained artifact is topology evidence. Therefore A10/NFR56 and NFR41/NFR57/NFR58/NFR59 are not ratifiable, and Murat approval is withheld.

| Result type | Verdict (2026-08-24) | Verdict (2026-08-27, see addendum) |
| --- | --- | --- |
| Static completion lifecycle | PASS | PASS (unchanged) |
| Final evidence acceptance | PASS | PASS (unchanged) |
| Hosted recovery authenticity | FAIL | **PASS** — run `33066358280`, evidence `01M11EYSDMP1ZF38B7KZA1A6FA`, exact commit `17aa94d` |
| A10/NFR56 ratification | FAIL | **PROVISIONAL** — genuine hosted evidence now exists but does not meet the exit condition (180s ceiling vs. 4h RTO target; RPO still a no-loss-path constant). Not a FAIL (evidence is real and authentic) and not a PASS (target not ratified). |
| NFR41/NFR57/NFR58/NFR59 ratification | FAIL | **PROVISIONAL** — same reasoning; see addendum |
| Production-scale equivalence | FAIL / unproven | FAIL / unproven (unchanged) |

## Threshold Matrix

No test-design NFR plan exists under `_bmad-output/test-artifacts/`; thresholds are sourced from the PRD, Story 12.15, the live-recovery ADR, architecture spine, and pinned evidence policy.

| Category | Threshold |
| --- | --- |
| Testability & Automation | All mandatory mappings present; non-zero passing required lanes; no primary skip; policy/planner/attestor/validator/sanitizer and workflow contracts pass. |
| Test Data Strategy | Dedicated `replay-test:` tenant; dataset version `v1`; configured volume >= 6; synthetic/redacted/consented, reproducible fixtures. |
| Scalability & Availability | Every unrelated tenant/mailbox control remains available for Graph, identity, AI provider, command, audit, and attachment outages. Production-scale threshold: **UNKNOWN**. |
| Disaster Recovery | RPO <= 15 minutes; RTO <= 4 hours; projection rebuild <= 4 hours without mailbox re-ingestion. |
| Security | Zero cross-tenant leakage, unauthorized mutation, or silent data loss; fail closed; metadata-only completion evidence; read-only workflow permissions. |
| Monitorability | Scope/dependency recorded <= 5 minutes when monitoring exists; evidence <= 8 days old; complete replayable provenance/assertions/cleanup/locators; missing observation is `unmeasurable`. |
| QoS/QoE | **UNKNOWN** for this story; no recovery-specific end-user QoS workload or threshold is defined. |
| Deployability | One validated current-run producer; exact head; pinned .NET/Dapr/actions; 360-minute outer budget; fixed cleanup/publication reserve; any stage failure keeps the check red. |

## Evidence Summary

### Fresh local checks

| Check | Result |
| --- | --- |
| StoryEvidenceGate tests | 195 passed; 0 failed; 0 skipped |
| Live completion timeout/startup contracts | 16 passed; 0 failed; 0 skipped |
| Recovery evidence-gate/coordinator tests | 31 passed; 0 failed; 0 skipped |
| Focused recovery/StoryEvidence architecture tests | 10 passed; 0 failed; 0 skipped |
| Workflow `actionlint` | PASS for CI and release |
| Evidence policy JSON parse | PASS |
| Dapr installer shell syntax | PASS |

Build/test output still reports pre-existing `Microsoft.IdentityModel.*` `8.19.2` versus `8.22.0` resolution warnings. They did not fail these checks but remain an unrelated maintainability/deployability concern.

### Current hosted evidence

- Release runs from 2026-08-02 through 2026-08-24 are red.
- Run `32705581758` at SHA `865e5b3ced18c1e10df7a83d85d6a93b0bda7fa0` executed the recovery producer; the live test failed, cleanup completed, and the nominal upload step passed with no recovery files.
- The run retained only `release-topology-acceptance-evidence`; the independent recovery gate failed downloading the missing recovery artifact.
- Scheduled CI runs on 2026-08-09, 2026-08-16, and 2026-08-23 failed their build jobs before recovery production.
- The current worktree includes later uncommitted remediation, so no hosted run binds this exact implementation.
- Ignored local `TestResults/` files are diagnostics, not citable retained evidence.

The canonical decision log now records the failed hosted attempts and latest run link. No passing hosted recovery bundle or locator exists.

### Platform cross-check

- GitHub documents a 360-minute maximum step timeout/default job timeout and a six-hour GitHub-hosted job limit.
- Official artifact actions show current upload `v7`/download `v8`, immutable current-major uploads, SHA-256 artifact digests, and expiry/deletion limits.
- Dapr lists CLI/runtime `1.18.0` as a supported tested pair and documents `dapr init --runtime-version`.
- GNU Coreutils documents the selected-signal deadline followed by `--kill-after` grace and SIGKILL.

## Category Assessments

### 1. Testability & Automation — PASS

Final validation, provenance binding, strict TRX parsing, recovery binding and the side-effect authorization boundary have direct automated evidence. `PreflightProductionContract` validates all active lane fields and primary-path bindings before `CompletionProductionPlanner` can return `requiresRecovery: true`. The focused gate suite includes positive and negative planner cases plus a hostile sanitizer matrix and passes 195/195 locally.

### 2. Test Data Strategy — CONCERNS

The implementation has a dedicated replay tenant, fixed versioned corpus, configured minimum volume, isolated partitions, control sentinels, and cleanup. No current hosted bundle proves those inputs were used and retained for the exact implementation. Four of six categories remain provenance-only and WORM remains process-local.

### 3. Scalability & Availability — CONCERNS

The sandbox provides credible single-run isolation mechanisms. It does not establish horizontal, vertical, data, or traffic scaling. AKS, multi-replica coordination, autoscaling, regional failure, production volume, external M365, durable WORM/KMS, NFR22 continuity, and NFR30 backlog isolation remain unproven.

### 4. Disaster Recovery — FAIL

| Requirement | Threshold | Actual evidence | Verdict |
| --- | --- | --- | --- |
| A10/NFR56 RPO | <= 15 minutes | No citable non-constant hosted loss-path measurement | FAIL |
| A10/NFR56 RTO | <= 4 hours | No passing hosted measurement; lane ceiling is 180 seconds | FAIL |
| NFR57 rebuild | <= 4 hours | No hosted duration; same 180-second ceiling; WORM equivalence residual | FAIL |

A 180-second pass can support only “recovered within 180 seconds.” It cannot demonstrate a miss between 3 minutes and 4 hours and cannot ratify either four-hour target.

### 5. Security — FAIL

Authentication, closed controller routes, tenant guard, per-run CSPRNG secrets, fixed-time comparison, read-only permissions, strict input grammars, and metadata-only completion projection pass static review. The release threshold is nevertheless failed for lack of critical evidence: no retained hosted artifact proves zero leakage, zero unauthorized mutation, zero silent loss, zero duplicate effects, complete cleanup, or absence of secrets/payloads in the entire operational artifact/log surface.

### 6. Monitorability / Debuggability / Manageability — FAIL

The independent evidence gate, stable reasons, freshness, attempt coherence, cleanup, assertion vocabulary, alert reconciliation, and workflow-level failure envelope pass local/static checks. NFR41 is not established because the current stamps are sandbox-originated rather than product monitoring and no hosted latency exists. The workflow envelope improves diagnosis but cannot substitute for a domain attempt summary.

### 7. QoS / QoE — CONCERNS

The story defines no recovery-specific end-user QoS workload or threshold. General product p95 thresholds cannot be inferred as recovery-lane criteria. Status remains CONCERNS until a threshold is explicitly declared or the category is formally marked out of scope.

### 8. Deployability — PASS

The completion path's exact-head checkout, pins, permissions, and 360/40/10/280/265/330/30-minute envelope pass static verification. Scheduled and release recovery producers now use the checksum-pinned Dapr 1.18.0 pair and current artifact-action majors, create/finalize a metadata-only attempt envelope, upload under `if: always()`, and set `if-no-files-found: error`. Producer or pre-producer failures remain red while leaving a diagnostic artifact on ordinary runner/step failures.

## Findings Summary

**ADR Quality Readiness Checklist: 14/29 criteria met — significant gaps**

| Category | Criteria Met | PASS | CONCERNS | FAIL | Overall |
| --- | ---: | ---: | ---: | ---: | --- |
| Testability & Automation | 4/4 | 4 | 0 | 0 | PASS |
| Test Data Strategy | 2/3 | 2 | 1 | 0 | CONCERNS |
| Scalability & Availability | 0/4 | 0 | 4 | 0 | CONCERNS |
| Disaster Recovery | 0/3 | 0 | 0 | 3 | FAIL |
| Security | 2/4 | 2 | 1 | 1 | FAIL |
| Monitorability, Debuggability & Manageability | 3/4 | 3 | 0 | 1 | FAIL |
| QoS & QoE | 0/4 | 0 | 4 | 0 | CONCERNS |
| Deployability | 3/3 | 3 | 0 | 0 | PASS |
| **Total** | **14/29** | **14** | **10** | **5** | **FAIL** |

## Quick Wins

The four immediate static quick wins identified by this audit are complete: full planner preflight, adversarial sanitizer coverage, corrected hosted-run governance, and fail-closed operational failure-envelope retention.

## Recommended Actions

### Immediate — before story closure or release

1. **Keep A10 and NFRs provisional** — CRITICAL — Architecture/Murat
   - Do not record Murat approval or transition Story 12.15 to `done` until a complete passing exact-commit hosted bundle is independently replayed.

### Short term — next recovery-validation milestone

1. **Collect falsifiable timing evidence** — HIGH — DevOps/Architecture
   - Non-constant RPO loss path, full-window four-hour RTO/rebuild drill, and product-monitoring detection-to-scope stamps.

2. **Run hosted burn-in** — HIGH — QA/DevOps
   - Record iteration count, pass/flake/cleanup rates, and phase/per-scenario duration percentiles.

3. **Align operational deadline reserve** — HIGH — DevOps
   - The Dapr pair is now pinned; apply the completion path's fixed closeout reserve to scheduled/release producers.

### Long term — production-equivalence backlog

- Validate external M365, durable WORM/KMS, AKS/multi-replica control, regional failure, production volume, NFR22 non-AI continuity, and NFR30 backlog isolation against a versioned deployment baseline.

## Monitoring Hooks

- **Recovery attempt status:** alert when a required producer is missing, red, incomplete, or has no retained failure envelope. Owner: DevOps. Deadline: before next required release.
- **Evidence freshness:** alert before the 8-day citation boundary. Owner: Architecture/DevOps. Deadline: before first ratification.
- **Artifact leakage scan:** scan TRX, JSON, filenames, and runner logs for token/payload sentinels. Owner: Security/QA. Deadline: before Murat approval.
- **Phase budgets:** publish setup, producer, cleanup, projection, attestation, validation, and upload durations. Owner: DevOps. Deadline: first successful hosted completion.
- **NFR41 observation:** preserve independent fault-observed and product scope-recorded UTC stamps; missing values remain `unmeasurable`. Owner: Operations. Deadline: before NFR41 ratification.

## Fail-Fast Mechanisms

- Full production-plan grammar validation before destructive setup.
- `if-no-files-found: error` plus a pre-created metadata-only attempt envelope.
- Required live filter with `TreatNoTestsAsError` and zero-skip policy.
- Unconditional cleanup with bounded restoration/cleanup tokens.
- Independent out-of-process evidence replay and freshness/provenance checks.
- Exact-head, single-consumer, pinned-source/path/selector enforcement.

## Evidence Gaps

| Gap | Owner | Deadline | Required evidence | Impact |
| --- | --- | --- | --- | --- |
| Passing hosted Tier-3 bundle and locator | DevOps | Before story closure | Producer artifact + independent gate decision for exact commit | Blocks all hosted authenticity claims |
| Non-constant RPO loss path | Architecture/DevOps | Before A10 ratification | Retained loss/reconstruction measurement | Blocks RPO commitment |
| Four-hour RTO/rebuild falsifiability | Architecture/DevOps | Before A10/NFR57 ratification | Full-window lane or pre-production drill | Blocks both four-hour claims |
| Product NFR41 monitoring stamps | Operations | Before NFR41 ratification | Independent detection and scope-recording timestamps | Blocks NFR41 |
| NFR58/NFR59 full scenario replay | Murat/QA | Before story closure | Passing fresh hosted bundle and leakage scan | Blocks Murat approval |
| Production equivalence | Architecture/Operations | Before production-equivalent claim | External M365, durable WORM/KMS, AKS/multi-replica/regional/scale evidence | Keeps residuals open |
| QoS/QoE threshold | Product/Architecture | Next test-design revision | Explicit threshold or formal N/A decision | Remains CONCERNS |

## Gate YAML Snippet

```yaml
nfr_assessment:
  date: '2026-08-24'
  story_id: '12.15'
  feature_name: 'live recovery evidence and A10 recalibration'
  adr_checklist_score: '14/29'
  categories:
    testability_automation: 'PASS'
    test_data_strategy: 'CONCERNS'
    scalability_availability: 'CONCERNS'
    disaster_recovery: 'FAIL'
    security: 'FAIL'
    monitorability: 'FAIL'
    qos_qoe: 'CONCERNS'
    deployability: 'PASS'
  overall_status: 'FAIL'
  critical_issues: 2
  high_priority_issues: 4
  medium_priority_issues: 0
  concerns: 10
  blockers: true
  quick_wins: 0
  evidence_gaps: 7
  recommendations:
    - 'Keep Story 12.15 and A10/NFR41/NFR57/NFR58/NFR59 provisional until a passing exact-commit hosted bundle is independently replayed.'
```

## Validation Checklist Result

- Thresholds are defined or explicitly UNKNOWN; none were guessed.
- Every PASS is supported by code and fresh local verification.
- Missing critical hosted evidence is classified FAIL, not waived.
- Every CONCERNS/FAIL result has a specific owner, deadline, and evidence request.
- Browser automation is N/A: no browser surface or hosted target URL; no browser session was opened.
- No orphaned CLI/browser sessions exist.
- The report includes all eight readiness categories, remediation, evidence gaps, and a gate snippet.

## Sign-Off

**Murat Test-Architect verdict (2026-08-24): approval withheld.** The static lifecycle concerns from this audit are resolved. Retain and independently replay a complete passing hosted bundle, then obtain the separately falsifiable timing/monitoring/production-equivalence evidence before re-running this NFR audit. *(This specific blocking condition — retain and independently replay a complete passing hosted bundle — was met 2026-08-27. See the addendum below for the current verdict; it is not a bare approval.)*

Recommended next workflow after remediation: `bmad-testarch-trace` for requirement-to-evidence coverage, followed by the required release gate.

## Addendum (2026-08-27)

This audit's category assessments, ADR quality checklist, and gate YAML above are **left unchanged** — they were not re-run and re-grading them is outside the scope of this addendum. This addendum updates only the two claims this audit made that are now factually stale: that no complete passing hosted recovery bundle exists, and that Murat's stated retain-and-independently-replay condition is unmet.

**Hosted recovery authenticity is resolved.** Required release run [33066358280](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/33066358280) (2026-08-27T11:12–11:38 UTC), commit `17aa94d48a79b5260be919e97060290403924fe2` — the exact commit Story 12.15 was reviewed at — passed all four jobs: topology acceptance, the live recovery validation sweep, the independent out-of-process evidence gate (`Total tests: 1, Passed: 1`), and `semantic-release`. Evidence run `01M11EYSDMP1ZF38B7KZA1A6FA` retains nine manifests, nine reports, and a zero-alert attempt summary, independently downloaded and inspected (not taken on the workflow's green checkmark alone). Both continuity drills `met`, projection rebuild `equivalent`, all six scoped dependencies `contained`, zero deviations anywhere. Full detail: `.decision-log.md` (2026-08-27 entry) and Story 12.15's Completion Notes/Debug Log.

**A10/NFR56/NFR57/NFR41 ratification is still not established — and this is not a defect in the new evidence.** The same two structural limits this audit already named under Disaster Recovery (the 180-second measurable-recovery ceiling against the 4-hour RTO/rebuild targets, and RPO being a constant on the no-loss path) are present in the 2026-08-27 run exactly as they were in every prior run: measured RTO 149.6s/60.5s (both comfortably inside the 180s ceiling, so a *miss* of the 4-hour target still cannot occur on this lane), measured RPO `0s` on both drills (still the no-loss-path constant). Nothing about this specific finding changed; only "did a complete passing hosted bundle exist" changed, from no to yes.

**Murat's verdict is precise, not a bare PASS:** the specific 2026-08-24 blocking condition ("retain and independently replay a complete passing hosted bundle") is satisfied and Murat confirms the 2026-08-27 bundle is authentic — genuinely live-topology, correctly attributed to the reviewed commit, internally consistent. A10/NFR56/NFR57/NFR41 numeric ratification remains withheld, as a distinct and still-open governance decision, for the ceiling/RPO-constant reasons above — this is the story's own "remain provisional" outcome (Story 12.15 A10 decision semantics), a legitimate non-blocking state for story closure, not an approval failure. Winston separately ratified the previously-outstanding domain-service projection-identity decision in `docs/adrs/live-recovery-validation-drivers.md` on the strength of this same bundle.

**NFR58/NFR59 note (unchanged from the body above):** the four sandbox-exercised scoped dependencies (`ai-provider`, `command-execution`, `audit-store`, `attachment-processing`) remain sandbox-contract evidence per `RV-PROVIDER-SCALE` — this run does not newly establish product-composed provider-boundary isolation proof for those four. `identity` and `graph` fault real topology boundaries as before.

Overall: **hosted-authenticity blocker resolved; A10/NFR ratification remains provisional by design, not blocking story closure.** This is not "PASS" in the sense the verdict table above used it for other rows — it is a genuinely governed, evidence-linked "provisional" outcome, recorded precisely so a reader does not conflate "authentic evidence now exists" with "the numeric target is confirmed."

## Addendum (2026-08-28 — DW-52)

DW-52 closes the implementation gap behind the prior "RPO constant on the no-loss path" finding without
rewriting the historical assessment. A separate `controlled-loss-path` job now rejects one known sandbox
notification, proves it absent, witnesses retained EventStore commits on both sides, and derives a positive RPO
only from their persisted UTC timestamps. The independent gate recomputes the value, anchors the allowed target
to `RecoveryTargets.MaxRpo`, and rejects zero, missing/reversed bounds, target misses, residual candidate state,
isolation/cleanup failure, stale evidence, commit mismatch, duplicates, and corrupt retention markers.

This is mechanism evidence only: local focused verification passed, but no hosted DW-52 artifact was produced or
cited. The authentic 2026-08-27 bundle contains nine manifests and predates the new fourth channel, so its two
ordinary 0s RPO values remain safety results and cannot ratify the 15-minute target. A10/NFR56 remains
provisional pending a fresh exact-commit hosted controlled-loss artifact, and the 180-second ceiling leaves the
4-hour RTO conclusion unchanged.
