# Configured adversarial review — recovery-primary provenance spine

**Lens:** Construct two units one level below the epic that obey the written ADs independently but still cannot interoperate.

**Verdict:** **CHANGES REQUIRED.** The current-run-only trust decision is coherent and removes the locator/digest cycle, but AD-2 and AD-4 do not yet define an executable producer contract or an enforceable publication boundary. Independent contract, workflow, test-harness, and artifact-upload units can all plausibly follow the spine and still disagree.

## Scope and reality inspected

- `ARCHITECTURE-SPINE.md`
- `.github/workflows/ci.yml`
- `story-evidence-policy.json`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/Program.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ProvenanceAttestor.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs`

## Findings

### ADV-1 — HIGH — Contract authors and the fixed workflow producer do not share an executable path/cardinality contract

**Written rule:** AD-1 allows `file:<declared-trx>` and the path convention permits contract-relative paths below the results root. AD-2 says the job writes "the declared TRX path."

**Two independently compliant units:**

1. Contract unit A declares `story-a/recovery.trx`, `story-a/recovery.provenance.json`, and `file:story-a/recovery.trx`.
2. Contract unit B declares `story-b/recovery.trx`, `story-b/recovery.provenance.json`, and `file:story-b/recovery.trx`.

Both use the adopted `current-run` source, safe relative paths, the recognized lane/selector, and distinct paths as the cross-contract collision guard encourages. The workflow producer nevertheless writes only `recovery-primary/live-recovery-validation.trx` (`ci.yml:239-247`). Neither contract is satisfied. If both authors instead infer that the workflow's fixed path is canonical, `RunCi` rejects their shared TRX as a cross-contract `result-path-collision` (`Program.cs:167-185`). The Boolean `requires_recovery` plan also erases the number and identity of consumers.

**Hole:** No owner defines the recovery producer output shape, and no rule chooses between single-consumer cardinality, fan-out to per-contract paths, or shared immutable input semantics.

**Required tightening:** Amend AD-2 (or add an AD) to make one strategy explicit and enforce it before DAPR starts. The smallest brownfield-compatible choice is:

- the executable lane path is canonical and exact: `recovery-primary/live-recovery-validation.trx`;
- the provenance path is canonical and exact (name it explicitly);
- a transition may have at most one active `recovery-primary` consumer; and
- the planning step rejects any other path or cardinality before production.

If multiple simultaneous consumers are required, replace the Boolean plan with an immutable list of `(storyKey, trx, provenance)` outputs and define how a single producer result is materialized into collision-free contract-owned paths.

### ADV-2 — HIGH — Lifecycle detection and destructive-execution authorization have different owners

**Written rule:** The diagram goes from an "Active completion contract" through requirement detection to the live producer. AD-2 prevents execution on ordinary runs but does not define when a detected contract becomes authorized to launch a destructive, potentially five-hour lane.

**Two independently compliant units:**

1. `TransitionDetector`/`detect` owns lifecycle candidate discovery and returns contract paths; it does not run the pinned policy and attestation-contract preflight (`Program.cs:96-112`).
2. The workflow planning unit parses those contract bytes directly with `jq`; any `.results[]` object containing `source == "current-run"` and `lane == "recovery-primary"` sets the execution Boolean (`ci.yml:186-216`). Strict policy, selector, source-binding, scope, result-path, and cross-contract checks occur only later in `ci`, after the live test and DAPR cleanup (`Program.cs:115-188`).

Both units can claim to follow "active-contract detection," yet a malformed or policy-invalid completion contract can authorize DAPR setup and the live destructive producer before failing closed during attestation. Fail-closed result semantics do not repair the incorrect side-effect authorization or wasted hosted capacity.

**Hole:** The spine names detection but not an authoritative, side-effect-free execution plan.

**Required tightening:** AD-2 must require a repository-owned planning command—not raw workflow `jq`—that validates the pinned policy, strict contract grammar, recognized source/lane/selector binding, canonical paths, scope, and cardinality before emitting `requires_recovery`. Machine-result existence/checksum/freshness remains the post-production preflight. DAPR setup and test execution must consume only that validated plan.

### ADV-3 — HIGH — The metadata-only publication rule is not enforceable for the uploaded raw TRX

**Written rule:** AD-4 forbids message bodies, credentials, tokens, secrets, prompts, and raw claims anywhere in the evidence surface.

**Two independently compliant units:**

1. The live producer launches an Aspire topology using generated controller and mailbox secrets and may emit test/process diagnostics. Its responsibility is to produce a valid TRX.
2. The upload unit retains the whole raw recovery result directory on both success and failure (`ci.yml:301-309`). `TrxEvidenceReader` verifies outcome, selectors, timestamps, and only the namespace of a small set of structural element names (`TrxEvidenceReader.cs:438-457`); it neither rejects nor sanitizes ordinary TRX output/diagnostic elements.

The producer can regard its diagnostics as normal test output while the uploader regards any validator-readable TRX as metadata-only. Both follow their local interpretation, but the published evidence can contain arbitrary captured output. The risk is greater on failure, when exceptions and process diagnostics are most likely and the upload still runs.

**Hole:** AD-4 states a desired property but assigns no component the redaction/allow-list decision and no failure behavior for unsafe TRX content.

**Required tightening:** Bind a single publication-safety owner and format. Before upload, either:

- transform the raw TRX into a deterministic allow-listed evidence projection containing only run times, counters, test IDs/names/outcomes, and the checksum-bound provenance; or
- make the TRX grammar a complete allow-list that rejects output/diagnostic payload elements, and upload raw TRX only after that pass.

On producer/preflight failure, upload only metadata-safe gate reports and a sanitized failure summary; do not upload unchecked raw recovery output. Add a secret/payload mutation test that proves unsafe TRX bytes never enter the artifact.

### ADV-4 — MEDIUM — The completion producer's operational side-products have no trust owner or truthful artifact layout

**Written rule:** AD-3 separates scheduled/release operational evidence from TE-2 completion evidence, but it does not say what happens to the operational reports/manifests that the same live class necessarily emits when invoked by the completion job.

**Two independently compliant units:**

1. `LiveContinuityAspireE2eTests` writes the operational bundle to repository-root `TestResults/live-recovery/<runId>` and, because the workflow sets `HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT`, advertises `artifact:story-evidence-integrity-reports` (`LiveContinuityAspireE2eTests.cs:129-150,445-455`). Its sink advertises `/results.trx` and `/reports` below that artifact.
2. The completion uploader publishes runner-temp story reports, `machine-results/recovery-primary`, and provenance sidecars only; it does not include repository-root `TestResults/live-recovery` and its TRX is named `live-recovery-validation.trx` (`ci.yml:301-309`).

The live-class unit can claim that its normal operational evidence contract is unchanged; the completion workflow can claim that AD-3 keeps operational/A10 evidence out of TE-2. The composed result is dangling locators that explicitly claim evidence is present in an artifact where it is absent.

**Hole:** AD-3 separates trust purposes but not storage ownership, naming, or whether completion-run operational outputs are retained, discarded, or diagnostics-only.

**Required tightening:** Choose and state one rule. Recommended: completion-run operational side-products are diagnostics-only, never eligible for A10, but if their manifests advertise an artifact they must be uploaded under a separate, truthful canonical layout and visibly labelled non-authoritative. Otherwise the completion invocation must suppress those locators/side-products. The TE-2 artifact should not impersonate the scheduled/release A10 layout.

### ADV-5 — MEDIUM — The deferred timeout question permits the producer and cleanup units to allocate the same budget incompatibly

**Written rule:** AD-2 requires cleanup and says timeout fails the check. The stack fixes a 360-minute job budget, while the live class fixes a five-hour in-process workflow deadline (`LiveContinuityAspireE2eTests.cs:148-160`). The spine defers hosted headroom.

**Two independently compliant units:**

1. The harness consumes its permitted five-hour deadline and enters its in-process `finally` only when its own cancellation is observed.
2. The job spends time on checkout, build/test, bound resolution, detection, DAPR setup, retained downloads, attestation, validation, cleanup, and upload around that producer, under the same 360-minute outer deadline.

Both obey the named budgets, but if pre-producer work consumes the nominal reserve, the outer job can terminate before the harness or workflow cleanup and upload units run. A red check is preserved; the stronger cleanup/publication invariant is not.

**Hole:** "Unproven headroom" is not merely an operational measurement; it leaves ownership of the shared deadline undecided.

**Required tightening:** Define a budget inequality and owner: `pre-side-effect bound + producer bound + cleanup/upload reserve < job bound`. Give the producer step a limit that preserves a fixed cleanup/upload reserve, and make an elapsed-time precheck refuse to start the destructive lane when the reserve is already exhausted. Hosted measurements may tune the values but must not decide the ownership rule.

## What is sound

- AD-1's current-run-only source decision eliminates the future artifact-identity / immutable-sidecar cycle.
- `ProvenanceAttestor` remains the sole TE-2 sidecar writer and preflights all current-run lanes before writing any sidecar.
- The scheduled/release evidence cannot accidentally satisfy the now-current-run-only policy binding.
- The job retains read-only repository/Actions permissions, and the live lane is conditional rather than routine.

## Gate recommendation

Do not finalize the spine until ADV-1, ADV-2, and ADV-3 are resolved in enforceable Rules and reflected in workflow/tool tests. ADV-4 should be resolved with the same artifact-boundary change. ADV-5 may retain measured values as deferred, but the budget-owner invariant itself must move out of Deferred.

## Remediation re-review — 2026-08-24

**Final verdict:** **PASS.** Re-running the original two-independent-units attack against the remediated spine and implementation found no remaining ADV-1–ADV-5 interoperability gap. The adopted rules now name each boundary owner, and the workflow consumes those owners in the required order.

### ADV-1 — CLOSED

The producer path and consumer cardinality are now executable contract rules rather than author convention. AD-1 pins the exact TRX, provenance, and locator paths and permits at most one active recovery consumer. `story-evidence-policy.json` pins the same TRX/provenance pair. `CompletionProductionPlanner` enforces `current-run`, the exact paths and locator, the sole selector, the recovery primary-path declaration, `allowSkipped: false`, and rejects more than one recovery declaration before returning `requiresRecovery`. Two independently authored contracts can therefore neither select distinct unsupported outputs nor share the canonical output: the plan fails before production.

### ADV-2 — CLOSED

Authorization now has one side-effect-free owner. The workflow runs the repository-owned `plan` command before any DAPR setup, and only parses that command's emitted booleans and retained locators. The planner validates the pinned policy and invokes `PreflightProductionContract` for strict contract identity, completion status/lifecycle, scope/digest, exact File List, all mandatory checked items, and mapping declarations; it then validates bindings, safe paths, collisions, and recovery cardinality. Raw contract `jq` no longer authorizes the destructive lane.

### ADV-3 — CLOSED

`RecoveryTrxSanitizer` is now the sole recovery publication owner named by AD-4. The live test writes its raw TRX under `${{ runner.temp }}/raw-recovery-results`, which is outside every upload path. The sanitizer constructs a new allow-listed TRX containing only the bound test identity, times, safe ID, outcome, and reconciled counters, and only that canonical projection enters `machine-results/recovery-primary` for attestation and upload. If production or projection fails, the unchecked raw directory is still excluded. `RecoveryTrxSanitizerShouldProjectOnlyBoundMetadata` injects bearer/secret/payload diagnostic output and proves it is absent from the projection.

### ADV-4 — CLOSED

AD-3 and AD-4 now assign the completion-run operational reports/manifests an explicit diagnostics-only disposition: they are not TE-2 evidence, are not eligible for A10, and are not uploaded. The completion invocation no longer labels them as `story-evidence-integrity-reports`; that retained artifact uploads only gate reports, the sanitized recovery directory, and provenance sidecars. The scheduled/release workflows retain their separate operational artifact names and independent evidence gate. Consequently no operational manifest or dangling operational locator reaches the TE-2 artifact, and the two trust layouts no longer impersonate one another.

### ADV-5 — CLOSED

AD-5 now owns a concrete nested budget, and the corrected workflow comment matches the executable values. Job elapsed time is recorded before setup; DAPR initialization is refused after 40 minutes and bounded to 10 minutes; the live step is bounded to 280 minutes while its in-process deadline is 265 minutes, leaving 15 minutes for host unwind; the remaining 30 minutes are reserved for DAPR cleanup, sanitization, attestation, validation, and upload within the 360-minute job ceiling. Hosted measurements are deferred only for tuning and may not remove the reserve inequality or fail-closed refusal.

### Verification evidence

- Deterministic spine lint: 0 findings.
- `Hexalith.ChatBot.StoryEvidenceGate.Tests`: 182 passed, 0 failed, 0 skipped.
- `actionlint .github/workflows/ci.yml`: passed.

No further adversarial remediation is required for ADV-1–ADV-5.
