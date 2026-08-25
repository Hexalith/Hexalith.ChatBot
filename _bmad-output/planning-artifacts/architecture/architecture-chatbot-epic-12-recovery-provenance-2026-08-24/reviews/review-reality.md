# Reality and provenance review

**Artifact reviewed:** `ARCHITECTURE-SPINE.md`  
**Lens:** Verify that adopted decisions are supported by current authoritative behavior or brownfield evidence, with special attention to GitHub Actions, .NET, Dapr, artifact handling, and repository claims.  
**Review date:** 2026-08-24

## Verdict

**Revise before treating the spine as an adopted trust boundary.** AD-1 and AD-3 are supported by the current policy, validator, tests, and workflow separation. The .NET SDK and 360-minute GitHub Actions budget are current and correctly stated. AD-2 is only partially implemented, while AD-4's absolute metadata-only claim is contradicted by the raw-TRX path. The Stack table also records action tags but omits their effective runtimes; the resulting Dapr execution is floating and the artifact actions are several current major versions behind.

## Decision evidence coverage

| Decision / claim | Reality-check result | Evidence |
| --- | --- | --- |
| AD-1: `recovery-primary` is current-run only | Confirmed | `story-evidence-policy.json:167-186` permits only `current-run`; `StoryEvidenceValidator.ValidatePinnedPolicy` pins that binding; `StoryEvidenceGateTests.cs:2039-2065` rejects a retained recovery-primary lane. `ProvenanceAttestor.cs:156-167` requires `file:<trx>`. |
| AD-2: exact-head order, preflight before sidecar writes, fail closed | Partially confirmed | `.github/workflows/ci.yml:138-176,232-310` has the stated high-level order. `Program.cs:143-201` preflights every active contract before `WritePlan`. The declared-path, pre-producer validation, external-toolchain binding, and stable cleanup-reason claims have gaps described below. |
| AD-3: operational recovery evidence cannot satisfy completion | Confirmed | Scheduled/release jobs call the independent recovery evidence gate and do not call `ProvenanceAttestor`; TE-2 policy rejects retained `recovery-primary`. The ADR and workflow agree on the split. |
| AD-4: least privilege | Confirmed for `GITHUB_TOKEN` scopes | `.github/workflows/ci.yml:108-110` sets only `contents: read` and `actions: read`; unspecified permissions are not granted. |
| AD-4: all evidence is metadata-only | Contradicted | The JSON grammars are bounded, but the raw TRX is uploaded and its nested output/error text is neither allowlisted nor scanned. Finding R4 details the gap. |
| .NET SDK 10.0.302 | Confirmed current | The root `global.json` and workflow pin 10.0.302. Microsoft's [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) lists SDK 10.0.302. |
| GitHub Actions job budget 360 minutes | Confirmed current | GitHub's [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idtimeout-minutes) documents a 360-minute default job timeout; the job explicitly sets 360. Hosted headroom remains honestly deferred and unproven. |
| Artifact and Dapr action stack | Out of date / incomplete | Findings R1-R3 detail the current action releases and the effective Node/Dapr versions hidden behind the tags. |

## Findings

### R1 — The Dapr producer is not a pinned or supported stack

- **Lens:** reality/provenance
- **Location:** Stack; AD-2; `.github/workflows/ci.yml:226-250`
- **Trigger condition:** `dapr/setup-dapr@v2` is used without a `version` input, followed by bare `dapr init`. The action's authoritative [`v2/action.yml`](https://github.com/dapr/setup-dapr/blob/v2/action.yml) defaults the CLI to **1.13.0** and declares `runs.using: node20`. Current `dapr init` behavior installs the latest runtime unless `--runtime-version` is supplied, as documented in the [Dapr CLI reference](https://docs.dapr.io/reference/cli/dapr-init/). Dapr's current [supported-release table](https://docs.dapr.io/operations/support/support-release-policy/) pairs the 1.18 runtime with a 1.18 CLI and says unlisted combinations are unsupported. The current runtime release is 1.18.x, not 1.13.x.
- **Evidence:** The repository pins neither the CLI nor runtime and does not include either effective version in the TE-2 sidecar. A single repository HEAD can therefore execute against different Dapr runtimes over time. The old 1.13 CLI also predates the current standalone Scheduler cleanup model; its [`uninstall.go`](https://github.com/dapr/cli/blob/v1.13.0/pkg/standalone/uninstall.go) removes placement, Redis, and Zipkin containers but has no Scheduler cleanup. This makes the broad Dapr-cleanup statement version-dependent.
- **Guard snippet:** Pin a currently supported CLI and runtime pair (`with: version: 1.18.x`; `dapr init --runtime-version 1.18.x`), pin the action to a reviewed commit SHA or replace it with a maintained Node 24 installer, assert `dapr --version`, and bind the exact CLI/runtime/action identity into retained metadata. Test cleanup against the pinned standalone topology.
- **Potential consequence:** The same exact-head contract can change behavior without a repository change, fail on a stale CLI/current runtime mismatch, or report cleanup while leaving current Dapr resources outside the old CLI's cleanup model.

### R2 — The GitHub artifact stack is materially out of date and uses a deprecated Node runtime declaration

- **Lens:** reality/provenance
- **Location:** Stack lines 82-84; all `uses: actions/*@v4` entries in `.github/workflows/ci.yml`
- **Trigger condition:** The spine presents upload/download v4 as the stack without marking it as a legacy constraint. As of the review date, the official releases are [`actions/upload-artifact` v7.0.1](https://github.com/actions/upload-artifact/releases) and [`actions/download-artifact` v8.0.1](https://github.com/actions/download-artifact/releases). Both v4 action manifests still declare `node20`: [`upload-artifact@v4`](https://github.com/actions/upload-artifact/blob/v4/action.yml) and [`download-artifact@v4`](https://github.com/actions/download-artifact/blob/v4/action.yml). GitHub began defaulting JavaScript actions to Node 24 on 2026-06-16 and explicitly tells users to move to current Node 24 action versions in its [Node 20 deprecation notice](https://github.blog/changelog/2025-09-19-deprecation-of-node-20-on-github-actions-runners/).
- **Evidence:** This is not merely a newer-version preference: current download v8 also fails digest mismatches by default, while the recorded v4 stack predates that current secure default. The spine contains no compatibility rationale for retaining v4 and no Node-runtime caveat.
- **Guard snippet:** Re-research and pin current compatible artifact actions, preferably by full release commit SHA; state the effective Node runtime and any intentionally retained old-major behavior. If v4 must remain, explicitly record the deprecation risk and verify it on the current hosted runner image.
- **Potential consequence:** A future runner migration/removal can break the completion path, and reviewers may infer current artifact integrity behavior that the selected old majors do not provide.

### R3 — "Immutable artifact upload" overstates what the workflow binds and retains

- **Lens:** reality/provenance
- **Location:** Design Paradigm lines 28 and 37; Deferred line 99
- **Trigger condition:** GitHub documents v4+ artifact archives as immutable **unless deleted**, and the action supports replacement by deletion/recreation with a new ID. Artifacts also expire; this workflow requests 30 days. The upload action exposes `artifact-id`, `artifact-url`, and `artifact-digest`, but `.github/workflows/ci.yml:301-310` captures none of them. The TE-2 sidecar instead binds `artifactLocator: file:<trx>` before upload.
- **Evidence:** The official [`upload-artifact` documentation](https://github.com/actions/upload-artifact) states both that created archives are immutable and that overwrite deletes and recreates an artifact with a new ID. GitHub's [artifact retention documentation](https://docs.github.com/en/organizations/managing-organization-settings/configuring-the-retention-period-for-github-actions-artifacts-and-logs-in-your-organization) confirms expiry/retention is administrative lifecycle, not permanent immutability. The ADR correctly calls upload a downstream retention detail; the spine's diagram drops that qualification.
- **Guard snippet:** Rename this node to "post-validation artifact retention" and state "immutable archive until deletion or expiry." If retained artifact identity is intended to be verifiable, capture the upload's artifact ID and digest in a separate post-upload record and define rerun-attempt identity; do not imply that the pre-upload TE-2 sidecar binds the GitHub artifact.
- **Potential consequence:** Reviewers can mistake a 30-day, deletable, unbound retention copy for a permanent tamper-evident completion input.

### R4 — AD-4 does not enforce metadata-only content for the raw TRX it uploads

- **Lens:** reality/provenance
- **Location:** AD-4 lines 62-66; `.github/workflows/ci.yml:301-310`; `TrxEvidenceReader.cs:119-243`; `CapturingContinuityDrillScenarioRunner.cs:24-28`
- **Trigger condition:** The workflow always includes the raw recovery TRX in the uploaded evidence surface. `EvidenceJson` enforces field names and values only for contract, policy, provenance, and JSON report structures. `TrxEvidenceReader` checks XML structure, counters, test identities, times, checksum, and selectors, but it does not reject nested `Output`, `StdOut`, error messages, stack traces, bearer strings, or payload-shaped text inside a `UnitTestResult`.
- **Evidence:** The brownfield comment in `CapturingContinuityDrillScenarioRunner.cs:26` explicitly acknowledges that an exception message can leak into the uploaded `.trx` outside metadata-only reports. The wrapper then rethrows, so the TRX logger remains free to serialize the original failure. No mutation test demonstrates that a passing or failing TRX containing a credential or tenant payload is rejected before upload.
- **Guard snippet:** Define and enforce a TRX evidence grammar that either rejects all output/error text or applies a strict metadata allowlist before attestation and upload. Add mutations for `StdOut`, error message, stack trace, bearer/token/secret shapes, message body, attachment content, and raw claims. Until then, narrow AD-4 to the JSON contract/sidecar/gate-report surfaces and do not promise raw-TRX sanitization.
- **Potential consequence:** A failing destructive test can upload tenant content or credentials while the completion job still satisfies its least-privilege repository permissions, violating the adopted no-payload invariant.

### R5 — The producer does not actually write a contract-declared TRX path

- **Lens:** reality/provenance
- **Location:** AD-2 line 54; `.github/workflows/ci.yml:186-216,232-247`; `ProvenanceAttestor.cs:89-115,156-167`
- **Trigger condition:** Requirement resolution extracts only Boolean `requires_recovery`; it does not extract the active lane's `trx` or `provenance` path. The producer always writes `recovery-primary/live-recovery-validation.trx`. The contract grammar and attestor accept a safe contract-relative path and then require that exact path. No Story 12.15 completion contract exists yet to make the fixed workflow path and future declaration identical.
- **Evidence:** A structurally valid completion contract can declare a different safe TRX path, cause the fixed producer to run successfully, and then fail attestation because the declared file is missing. That is fail-closed, but it is not the spine's stronger claim that the job "writes the declared TRX path."
- **Guard snippet:** Either pin the recovery-primary TRX/provenance paths in policy and validate them before production, or have a validated planning command emit the exact safe paths as workflow outputs and pass the declared TRX filename/results directory to `dotnet test`. Add a contract/workflow integration test that changes the declared path.
- **Potential consequence:** The first real completion contract, or a later path refactor, can burn the full live-run budget and fail only after production because workflow and contract drift are discovered too late.

### R6 — "Active completion contract" is not fully validated before destructive production

- **Lens:** reality/provenance
- **Location:** Design diagram lines 32-35; AD-2 line 54; `TransitionDetector.cs`; `.github/workflows/ci.yml:177-247`
- **Trigger condition:** `TransitionDetector` resolves exact commits and strict JSON shape/identity, but it does not run the pinned-policy, scope, lifecycle, primary-binding, locator, and mapping validations performed later by `ci`. The workflow then uses `jq` directly on each detected contract and starts Dapr/live recovery when any result object says `source == "current-run"` and `lane == "recovery-primary"`.
- **Evidence:** An independently detected but ultimately invalid completion contract can trigger the destructive five-hour-class producer before the final gate rejects it. The term "active" in the spine therefore means transition-detected, not policy-valid or completion-valid.
- **Guard snippet:** Add a no-results planning/preflight command before Dapr setup that validates pinned policy, lifecycle identity, recovery-primary selector/source/locator/path rules, and collision-free output planning, then emits the requirements consumed by the workflow. Reserve TRX/provenance content validation for the post-producer phase.
- **Potential consequence:** An erroneous completion edit can launch expensive destructive infrastructure work that was never eligible to complete, increasing blast radius and cost despite the final fail-closed verdict.

### R7 — Cleanup failure is red, but it does not have an existing stable reason code

- **Lens:** reality/provenance
- **Location:** Consistency Conventions line 75; `.github/workflows/ci.yml:248-250`; `GateReason.cs`
- **Trigger condition:** If the post-producer `dapr uninstall --all` step fails, GitHub marks the job failed and ordinary subsequent attest/validate steps are skipped; only the final `if: always()` upload still runs. `GateReason` has no cleanup reason, and no gate report is emitted for that workflow-step failure.
- **Evidence:** AD-2's narrower statement that cleanup failure leaves the required check failed is correct. The table's stronger statement that `cleanup-failed` fails with an existing stable reason code is not implemented for workflow-level Dapr teardown.
- **Guard snippet:** Either narrow the convention to "cleanup failure fails the GitHub check before attestation" or wrap teardown so it writes a bounded metadata-only cleanup result with an explicitly versioned reason code, then ensure the gate consumes it without allowing attestation on failure.
- **Potential consequence:** Automated consumers and reviewers expecting a stable gate reason receive only a generic failed shell step and cannot distinguish cleanup failure from unrelated workflow infrastructure failure.

## Confirmed brownfield strengths

- `ProvenanceAttestor` is the only non-test production writer of TE-2 provenance sidecars found in the repository.
- `Program.RunCi` plans every detected contract and detects result-path collisions before any `WritePlan` call.
- Retained sidecars are not rewritten by the attestor, and the recovery-primary binding excludes retained sources.
- The workflow verifies the checked-out event head and uses event-appropriate exact base/head bounds.
- Producer, no-test, cleanup, attestation, validation, and upload failures leave the GitHub job non-successful, even where a stable gate reason is absent.
- Scheduled/release recovery evidence and story-completion evidence remain separate in both code paths and stated governance.
- The spine correctly defers hosted 360-minute headroom, recovery authenticity, production equivalence, and A10 ratification rather than asserting evidence that does not exist.

## Required reality-check disposition

Before adoption, the architecture owner should resolve or explicitly defer R1, R2, R4, R5, and R6 because they affect the claimed trust boundary itself. R3 can be resolved by precise retention terminology unless durable artifact identity is a requirement. R7 requires either a claim correction or a small workflow/reporting contract. The spine should add the authoritative web sources above—or a dated research companion—so future reviews can distinguish verified platform behavior from repository-local design intent.

## Remediation re-review — 2026-08-24

### Verdict

**REVISE.** R3–R7 are resolved in the remediated spine and implementation. R1 and R2 are only partially resolved: the recovery producer inside `story-evidence-integrity` is now current and pinned, and that job's general/final artifact path uses current majors, but the conditionally consumed `topology-acceptance` producer still uses floating `dapr/setup-dapr@v2` plus bare `dapr init` and uploads its completion input with `actions/upload-artifact@v4`. The same legacy Dapr/artifact combinations remain in the scheduled and release operational lanes. The Stack table is therefore accurate only for the recovery-specific portion of the completion job, not for every current-run input that the completion trust boundary may consume.

### R1–R7 dispositions

| Finding | Disposition | Re-review evidence |
| --- | --- | --- |
| R1 — Dapr producer not pinned/supported | **Partially resolved; still open** | The recovery-specific completion producer now runs `.github/scripts/install-dapr-cli.sh`, which pins CLI `1.18.0`, checks the Linux-amd64 archive against `2a94739e0aa101289d88418225319562bc6800db273b3d9cf819a0efd1ea1bfe`, and invokes `dapr init --runtime-version 1.18.0` (`ci.yml:211-247`). The checksum matches the official [Dapr CLI v1.18.0 release asset](https://github.com/dapr/cli/releases/tag/v1.18.0), and Dapr's [supported-release table](https://docs.dapr.io/operations/support/support-release-policy/) identifies runtime/CLI `1.18.0 / 1.18.0` as the supported current pair. However, `topology-acceptance` remains an exact-head, conditionally consumed completion producer and still uses `dapr/setup-dapr@v2` with no version plus bare `dapr init` (`ci.yml:316-368`). The same floating pair remains at `ci.yml:410-413` and `release.yml:47-50,120-123`. The action's official [`v2/action.yml`](https://github.com/dapr/setup-dapr/blob/v2/action.yml) still defaults to CLI 1.13.0, while the [CLI reference](https://docs.dapr.io/reference/cli/dapr-init/) says an omitted runtime version selects the latest runtime. Thus a current-run `aspire-dapr-primary` input can still vary without a repository change and can still use the unsupported 1.13/latest combination identified in R1. |
| R2 — artifact actions out of date / Node 20 | **Partially resolved; still open** | `build` and final story-evidence uploads now use `actions/upload-artifact@v7`, and completion downloads use `actions/download-artifact@v8` (`ci.yml:92-98,129-133,205-210,305-314`). Those are the current official majors; the official release pages list [upload v7.0.1](https://github.com/actions/upload-artifact/releases) and [download v8.0.1](https://github.com/actions/download-artifact/releases), and download v8 fails digest mismatches by default. But the topology artifact consumed at `ci.yml:205-210` is still produced with `actions/upload-artifact@v4` at `ci.yml:360-365`. Other CI and release operational artifact steps also remain on v4 (`ci.yml:424-430,470-474,485-490`; `release.yml:70-75,134-139,181-185,196-201`). R2 is not fully remediated until the topology producer is current too, or the spine explicitly scopes its Stack claim and records the legacy producer as a deferred trust-boundary dependency. |
| R3 — immutable upload overstated | **Resolved** | The spine now calls this node post-validation retention, states a 30-day lifetime, and explicitly defers post-upload identity while recording that GitHub immutability lasts only until deletion or expiry. `ci.yml:305-314` still does not capture artifact ID/digest, which now agrees with the narrowed claim instead of contradicting it. |
| R4 — raw TRX violates metadata-only publication | **Resolved** | The raw live TRX is written to `${{ runner.temp }}/raw-recovery-results`, outside every upload path (`ci.yml:228-254,305-314`). `RecoveryTrxSanitizer` constructs a new TRX from an allowlisted class/method, safe ID, parseable run times, a passing outcome, and reconciled one-test counters; it never copies raw `Output`, `StdOut`, error, stack, attachment, or arbitrary XML content. The canonical recovery directory and sidecars are the only recovery completion inputs retained. `RecoveryTrxSanitizerShouldProjectOnlyBoundMetadata` proves that bearer/secret/payload text embedded in raw output is absent from the published projection. |
| R5 — producer path not contract-derived/pinned | **Resolved** | Policy now pins `recovery-primary/live-recovery-validation.trx` and its provenance path (`story-evidence-policy.json:167-189`); `ValidatePinnedPolicy`, `CompletionProductionPlanner.ValidateRecoveryLane`, and final primary validation all reject drift. The workflow writes the sanitizer output to that exact path (`ci.yml:248-254`). |
| R6 — destructive production before contract validation | **Resolved in source; focused mutation coverage remains thin** | `CompletionProductionPlanner.Plan` loads and pins policy, detects exact transitions, and calls `PreflightProductionContract` for strict story/contract identity, completed mandatory items, status/lifecycle, scope digest, File List, and mapping declarations before emitting requirements. It also resolves collision-safe TRX/provenance paths, validates the exact recovery selector/source/path/locator binding, and rejects multiple recovery consumers. `ci.yml:179-213` consumes only that plan before Dapr setup. The test suite directly covers the no-transition plan and the underlying policy/path/sanitizer invariants, but does not yet contain a positive recovery-transition planner test or a planner-level invalid/multiple-recovery mutation; this is a verification-strength gap, not an observed bypass in the inspected source. |
| R7 — cleanup claimed stable gate reason | **Resolved by claim correction** | The spine now distinguishes validator/attestor stable reason codes from workflow producer/timeout/cleanup failures. `dapr uninstall --all` uses `if: always()` when recovery was planned; a teardown failure keeps the GitHub job red, ordinary sanitizer/attestation steps do not run after the failure, and the final `if: always()` upload cannot include raw staging (`ci.yml:245-314`). No stable TE-2 cleanup code is claimed. |

### Additional committed-decision reality gap

AD-5's **fixed 30-minute reserve** is not literally enforced by the current arithmetic. The guard permits Dapr initialization when integer elapsed time is exactly 2,400 seconds (`elapsed_seconds > 2400`), after which the 10-minute initialization cap and 280-minute live-step cap consume the full first 330 minutes of a 360-minute job. Step-transition and command overhead can therefore reduce the remaining budget below 30 minutes. This is close to the intended reserve and still materially safer than the original workflow, but the spine/docs should say "nominal 30-minute reserve" or move the refusal threshold earlier/use a deadline-derived remaining-time guard before calling it fixed.

### Re-review checks

- `actionlint .github/workflows/ci.yml .github/workflows/release.yml` — passed.
- `dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --no-restore` — 195/195 passed on SDK 10.0.302.
- Focused `StoryEvidenceIntegrityGateShouldRemainFailClosedAndMachineBound` architecture test — passed.
- Full architecture project — 72 passed, 1 failed on an unrelated stale `ModelContextProtocol` version assertion (`1.4.1` expected, `2.2.0` actual); it does not change the R1–R7 dispositions but prevents claiming a clean full architecture suite.

### Final disposition

**REVISE before final acceptance.** Pin the Dapr CLI/runtime used by `topology-acceptance` and move its upload to v7 (or explicitly narrow the architecture Stack/trust-boundary claim and defer that current-run producer with rationale). Also correct AD-5's fixed-reserve wording or enforcement. With those scope mismatches addressed, the inspected R3–R7 remediations are sufficient for a reality-review PASS.

## Second remediation re-review — 2026-08-24

### Current verdict

**PASS.** This section supersedes the preceding remediation verdict for the current files. The completion trust boundary now uses the researched Dapr and artifact stack for every current-run producer it consumes, and AD-5's fixed closeout is enforced by absolute-deadline arithmetic plus bounded interrupt/kill behavior. R1–R7 are resolved within the spine's explicitly stated scope.

### Final dispositions

| Item | Final disposition | Evidence |
| --- | --- | --- |
| R1 — pinned/supported Dapr producer | **Resolved** | The consumed `topology-acceptance` producer now calls the same checksum-verifying `.github/scripts/install-dapr-cli.sh` as recovery and runs `dapr init --runtime-version 1.18.0` (`ci.yml:333-385`). Its CLI archive hash remains the exact hash published on the official [Dapr CLI v1.18.0 release](https://github.com/dapr/cli/releases/tag/v1.18.0), and Dapr's [supported-release table](https://docs.dapr.io/operations/support/support-release-policy/) pairs CLI/runtime 1.18.0. Thus both completion-consumed Dapr producers are fixed to the supported pair. |
| R2 — current artifact actions | **Resolved for the declared completion stack** | `topology-acceptance` now uploads with `actions/upload-artifact@v7`; `story-evidence-integrity` downloads it with v8, while the general producer and final retention upload also use v7 (`ci.yml:92-98,130-134,206-211,322-331,377-382`). These remain the current official majors: [upload-artifact v7](https://github.com/actions/upload-artifact) and [download-artifact v8](https://github.com/actions/download-artifact/releases). The spine now states explicitly that its Stack pins bind the completion job and every current-run producer it consumes, while AD-3's independently governed scheduled/release operational lanes are outside that stack (`ARCHITECTURE-SPINE.md:85-95`). Their remaining v4/floating operational dependencies are therefore visible repository debt, not an unmentioned completion-provenance input or a contradiction in this spine. |
| R3 — artifact immutability wording | **Resolved; unchanged** | Retention remains accurately described as 30-day, deletable/expiring post-validation storage whose GitHub artifact ID/digest is not a TE-2 attestation input. |
| R4 — metadata-only recovery publication | **Resolved; unchanged** | Raw recovery TRX remains outside all upload paths and only the sanitizer-owned canonical projection can enter completion retention. |
| R5 — recovery result path drift | **Resolved; unchanged** | Policy, planner, validator, and workflow remain pinned to the same exact recovery TRX/provenance paths. |
| R6 — destructive work before validation | **Resolved; unchanged** | The side-effect-free production plan remains the sole source of the recovery/topology requirements consumed before destructive setup. The previously noted planner mutation-test opportunity remains non-blocking. |
| R7 — cleanup failure semantics | **Resolved; unchanged** | Cleanup failure is accurately represented as a red workflow check with bounded step diagnostics, not as a stable TE-2 validator reason. |
| AD-5 — fixed 30-minute closeout reserve | **Resolved** | The job records its origin before checkout, refuses setup at `elapsed_seconds >= 2400`, and fixes closeout at `job_start + 330m` (`ci.yml:113-114,215-231`). Immediately before production it recomputes remaining wall time, refuses when no unwind window remains, schedules `SIGINT` at `min(remaining - 15m, 265m)`, and configures `SIGKILL` 15 minutes after that signal (`ci.yml:232-264`). Therefore forced termination occurs at or before fixed closeout even when initialization/step overhead consumes part of the envelope. GNU's authoritative [`timeout` documentation](https://www.gnu.org/s/coreutils/manual/html_node/timeout-invocation.html) confirms that `--kill-after` starts when the initial signal is sent and that timed-out/killed commands return failure statuses. GitHub's [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) confirms the 280-minute step cap and 360-minute job cancellation caps used as outer backstops. |

### Verification performed

- `actionlint .github/workflows/ci.yml .github/workflows/release.yml` — passed.
- Focused `StoryEvidenceIntegrityGateShouldRemainFailClosedAndMachineBound` architecture test — passed.
- Local GNU Coreutils probe with ignored `SIGINT` and `--kill-after` — forced `SIGKILL` at the cumulative deadline and returned 137 as documented.
- The prior 182/182 StoryEvidenceGate test result remains applicable; these final changes are workflow/docs/architecture-test changes, not StoryEvidenceGate production-code changes.

### Residuals accepted by this PASS

The hosted distribution of checkout, initialization, producer, and closeout durations is still unmeasured. The spine explicitly records that limitation in Deferred and requires retaining fixed closeout/fail-closed refusal when tuning values (`ARCHITECTURE-SPINE.md:108-113`). Scheduled/release operational lanes still use legacy action/Dapr combinations, but AD-3 and the Stack scope now state that they are independent A10/operational evidence paths and cannot satisfy TE-2 `recovery-primary`. Neither residual is presented as proven completion evidence, so neither blocks this reality/provenance verdict.

### Final disposition

**PASS for the Epic 12 recovery-primary completion-provenance spine and its current implementation boundary.**
