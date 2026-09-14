# Adversarial validation — Story 12.15 recovery-primary provenance

## Lens and evidence

**Lens:** Construct two implementation units one level below Story 12.15 that can each obey every architecture decision literally, yet still compose into an unsafe, ambiguous, or non-completing recovery-primary lifecycle.

**Evidence inspected:**

- `ARCHITECTURE-SPINE.md` and its adjacent `.memlog.md`
- `story-evidence-policy.json`
- `.github/workflows/ci.yml`
- `docs/story-evidence-integrity.md`
- `docs/adrs/live-recovery-validation-drivers.md`
- `_bmad-output/implementation-artifacts/12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10.md`
- `CompletionProductionPlanner`, `ProvenanceAttestor`, `StoryEvidenceValidator`, `TrxEvidenceReader`, `RecoveryTrxSanitizer`, and the `ci` orchestration entry point
- `LiveContinuityAspireE2eTests` and its cleanup/finalization path
- GitHub's current ruleset documentation, which states that required status checks are identified by job/check name and do not take workflow or event trigger type into account: <https://docs.github.com/en/enterprise-cloud@latest/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/troubleshooting-rules#troubleshooting-required-status-checks>

## Verdict

**CHANGES REQUIRED — three High-severity two-unit counterexamples remain: the externally required check has no event-specific identity, the five closeout units share an unallocated deadline, and publication has no producer-to-orchestrator cleanup receipt.**

## Findings

### A-001 — High — A green no-transition run and the required transition gate share one indistinguishable check identity

**Two-unit counterexample:**

1. The CI event unit obeys AD-2's exact-base/head rule. For `schedule` or `workflow_dispatch`, it deliberately resolves `base=head`, detects no completion transition, and reports a successful `story-evidence-integrity` check.
2. The repository-rules unit obeys the spine's repeated “required check” language and the repository documentation by requiring the check named `story-evidence-integrity`, optionally pinned to the GitHub Actions app.

Both units follow every AD: the first does not run destructive recovery on an ordinary/no-transition event, and the second requires the named fail-closed check. They are nevertheless incompatible because GitHub required-status-check matching does not include workflow or event trigger type. A scheduled/manual no-transition success and a pull-request/push transition verdict at the same commit have the same job identity; the rules unit cannot express “require the transition-evaluating event, not the no-transition event.” Depending on the check set GitHub presents, this can make enforcement ambiguous, block on duplicate results, or allow the wrong successful check to stand in for the intended gate.

**Evidence:** `.github/workflows/ci.yml:3-15` attaches the same workflow to pull request, push, schedule, and manual dispatch; `ci.yml:150-158` gives the job the invariant name `story-evidence-integrity`; `ci.yml:209-214` maps schedule/manual to `head..head`; `Program.cs:133-147` emits a successful `no-transition` report. `docs/story-evidence-integrity.md:5` says TE-2 remains incomplete until external branch protection requires the check by that name, while the spine assigns no unique check identity, event restriction, ruleset mode, or external owner. GitHub documents that required status checks do not take workflow or event trigger type into account.

**Consequence:** The provenance chain can be perfectly implemented inside the job yet remain advisory or be enforced against an event that did not evaluate the proposed completion transition. This defeats the trust purpose at the repository boundary.

**Recommended disposition:** **discuss.** Give the required completion-transition job an identity that cannot also report green for schedule/manual no-transition runs, or bind a required-workflow ruleset to a transition-only workflow. Record the protected branches, expected source/app or required-workflow identity, owner, and activation verification. Keep scheduled/manual diagnostics under a distinct job name.

### A-002 — High — The fixed closeout reserve has five consumers but no internal budget owner

**Two-unit counterexample:**

1. The DAPR-cleanup unit obeys AD-2 and AD-5 by starting only after production and using the reserved final 30 minutes; it may legitimately wait almost all 30 minutes for `dapr uninstall --all` to finish because the spine gives it no smaller deadline.
2. The projection/attestation/publication unit also obeys AD-2 and AD-5 by waiting for successful cleanup and then expecting to sanitize, preflight, write sidecars, validate, and upload within that same reserved final 30 minutes.

Each unit individually stays outside the producer budget and in the closeout phase. Composed, cleanup can consume the whole reserve before the other required units run. The outer 360-minute cancellation then prevents the always-intended failure artifacts from being retained and may interrupt cleanup itself.

**Evidence:** AD-5 collectively gives DAPR cleanup, projection, attestation, validation, and upload the final 30 minutes but specifies neither sub-deadlines nor a final publication floor. `.github/workflows/ci.yml:268-320` enforces the producer's absolute deadline; the cleanup command at `ci.yml:321-323` has no step timeout; projection, attestation, validation, and the final upload follow at `ci.yml:338-455` without an absolute remaining-time admission rule. The Retention convention promises upload on success or failure when artifacts are present, but the job-level hard stop can occur before those steps are scheduled.

**Consequence:** A producer failure or slow teardown can yield neither a complete cleanup nor the metadata needed to reconstruct the failure, despite the architecture claiming both a cleanup/publication reserve and failure-path retention.

**Recommended disposition:** **discuss.** Allocate enforceable closeout sub-budgets or absolute deadlines, including a non-consumable finalizer/publication floor. Make each phase refuse or terminate before it can consume the next phase's allocation, and define what minimum metadata the finalizer retains when teardown times out.

### A-003 — High — “DAPR cleanup succeeded” is not a cleanup receipt for the producer's domain mutations

**Two-unit counterexample:**

1. The live-recovery producer unit obeys the written spine by running the bound class and leaving DAPR teardown to the outer job. It treats post-scenario domain compensation, such as fresh-partition erasure, as best-effort because no AD names its state set, postcondition, or receipt schema.
2. The outer workflow unit obeys AD-2 by requiring `dapr uninstall --all` to exit successfully before projection. The sanitizer then sees one passing bound test and emits the canonical metadata-only TRX; the attestor has no cleanup input to bind.

Both units can truthfully report success for the cleanup they own: producer assertions passed and the DAPR runtime was removed. They can still compose after a late/reappearing projection write, a stranded fresh-partition key, or another producer-owned compensation miss. Nothing passed from the producer to the orchestrator proves that all Story 12.15 mutations reached a clean terminal state before publication.

**Evidence:** The story's binding obligation at `12-15-...md:56` says any cleanup failure is stop-ship. The spine narrows the executable boundary to “DAPR cleanup” and gives no cleanup state model or linearization point. `LiveContinuityAspireE2eTests.cs:534-557` catches a compensating partition-erase exception, logs it, and continues to topology disposal; the outer workflow observes only `dapr uninstall --all` (`ci.yml:321-323`). `RecoveryTrxSanitizer.cs:73-110` admits identity, times, counters, and outcome only, and `ProvenanceAttestor.cs:175-190` binds no cleanup status or receipt. Thus the current brownfield seams already demonstrate the ownership split rather than ratifying a hidden stronger contract.

**Consequence:** TE-2 can attest a passing recovery-primary result while test-owned persistent state was not proven clean, poisoning later runs or leaving destructive validation state behind. The sidecar proves the TRX bytes, not cleanup completion.

**Recommended disposition:** **discuss.** Define the producer-owned mutation set and a cleanup-complete receipt with an explicit linearization point after restoration, compensating deletes, quiescence, and postcondition reads. Require that receipt before sanitization/attestation; any missing, false, or timed-out cleanup receipt must keep the check red and flow into the bounded failure summary.

## Attempted attacks that did not expose a hole

- **Alternate recovery paths or two recovery consumers:** no hole. AD-1, the pinned policy, and `CompletionProductionPlanner` agree on the exact TRX/provenance/locator and reject more than one declaration before DAPR starts.
- **Malformed contract authorizes the destructive producer:** no hole. The workflow consumes the repository-owned `plan` result, and planning performs contract, lifecycle, scope, File List, mapping, path, binding, and cardinality checks before setup.
- **Raw producer TRX leaks payload through the TE-2 canonical result:** no hole in that specific boundary. Raw TRX stays under runner-temporary staging; `RecoveryTrxSanitizer` constructs a new allow-listed document and does not copy diagnostic nodes.
- **TRX mutation between attestation and validation yields a false pass:** no demonstrated hole. The current workflow has a single serial canonical writer, and validation re-reads and re-hashes the TRX, so a changed file fails its sidecar checksum rather than passing.
- **A partially written set of sidecars completes the transition:** no hole. Cross-contract paths are preflighted before writes; a write or later validation failure leaves the job red, and policy does not admit a retained `recovery-primary` sidecar as a substitute.
- **The long recovery makes upstream current-run lanes stale by construction:** no current hole. Brownfield policy and `StoryEvidenceValidator` pin a 360-minute per-primary-lane ceiling, so independently produced upstream lanes and the long recovery job share an executable freshness rule.
- **A scheduled/release operational bundle satisfies TE-2:** no hole. AD-3, policy, and validation reject retained recovery-primary evidence even if someone constructs a sidecar manually.
