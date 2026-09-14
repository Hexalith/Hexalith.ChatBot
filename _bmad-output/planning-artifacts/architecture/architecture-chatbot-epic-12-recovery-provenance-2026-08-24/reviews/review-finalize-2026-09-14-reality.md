# Finalize review — reality, version currency, and repository fit

**Lens:** Verify that every committed architecture decision is grounded in current official evidence or current repository reality. Confirm exact versions, hashes, GitHub enforcement semantics, and the fit of the prescribed update with the brownfield code. Treat `activation: pending` as an intentional architecture state, not an implementation defect; flag only language that accidentally presents pending controls as active.

**Reviewed:** 2026-09-14

**Target:** `ARCHITECTURE-SPINE.md` for Epic 12 `recovery-primary` provenance

## Verdict

**PASS WITH ACTIVATION GUARD — the spine's versions, hashes, external-platform semantics, and brownfield mappings are reality-checked, and the document accurately separates settled decisions (`status: final`) from unimplemented controls (`activation: pending`).** No accidental claim of current repository conformance was found. One non-blocking Dapr support caveat remains: the latest CLI/runtime patch releases exist and are correctly identified, but Dapr's official packaged-release matrix does not certify the `1.18.2` CLI / `1.18.4` runtime combination; the existing focused-verification activation precondition is therefore necessary and must not be waived.

## Evidence checked

### Repository and live GitHub state

- The spine and adjacent `.memlog.md`.
- `story-evidence-policy.json`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/scripts/install-dapr-cli.sh`, `global.json`, `Directory.Packages.props`, and the committed `references/Hexalith.Builds` package catalog.
- `CompletionProductionPlanner`, `RecoveryTrxSanitizer`, `ProvenanceAttestor`, `StoryEvidenceValidator`, `LiveContinuityAspireE2eTests`, and their focused contract tests.
- Story 12.15, the TE-2 spec, `sprint-status.yaml`, `docs/story-evidence-integrity.md`, and `docs/adrs/live-recovery-validation-drivers.md`.
- Authenticated read-only GitHub API checks against `Hexalith/Hexalith.ChatBot` on 2026-09-14:
  - `GET /repos/Hexalith/Hexalith.ChatBot/rulesets?includes_parents=true` returned `[]`.
  - `GET /repos/Hexalith/Hexalith.ChatBot/branches/main/protection` returned `404 Branch not protected`.
  - Repository Actions retention returned `days: 30`, `maximum_allowed_days: 30`.
  - The newest CI run still reports a job named `story-evidence-integrity`; a sampled scheduled run and push runs use the same workflow, confirming the current identity collision that AD-6 prescribes removing.
- The existing dirty worktree was read only. No user-owned file other than this assigned review was changed.

### Current official sources

- Microsoft [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json) and [`global.json` roll-forward semantics](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json).
- Dapr [CLI 1.18.2 release](https://github.com/dapr/cli/releases/tag/v1.18.2), [runtime 1.18.4 release](https://github.com/dapr/dapr/releases/tag/v1.18.4), [runtime 1.18.2 security fixes](https://github.com/dapr/dapr/releases/tag/v1.18.2), [`dapr init --runtime-version`](https://docs.dapr.io/reference/cli/dapr-init/), and [supported-release matrix](https://docs.dapr.io/operations/support/support-release-policy/).
- Microsoft [Aspire 13.5.3 release](https://github.com/microsoft/aspire/releases/tag/v13.5.3).
- GitHub [required-check identity/ruleset behavior](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/troubleshooting-rules), [expected-source behavior](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets), [360-minute job timeout](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax), [full-SHA action guidance](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/find-and-customize-actions), and [artifact retention behavior](https://docs.github.com/en/actions/tutorials/store-and-share-data).
- GitHub release/tag data for [upload-artifact v7.0.1](https://github.com/actions/upload-artifact/releases/tag/v7.0.1) and [download-artifact v8.0.1](https://github.com/actions/download-artifact/releases/tag/v8.0.1), resolved independently with the official repositories' tag refs.
- GNU Coreutils [`timeout` signal and `--kill-after` semantics](https://www.gnu.org/software/coreutils/manual/html_node/timeout-invocation.html).

## Findings

### R-001 — Medium, guarded — Dapr patch currency is verified, but the exact mixed patch pairing is not an officially packaged combination

**Claim checked:** AD-8 and the Stack bind Dapr CLI `1.18.2` with runtime `1.18.4`; activation requires focused recovery/provenance verification.

**Evidence:** The official GitHub releases identify CLI `1.18.2` and runtime `1.18.4` as the latest stable releases on 2026-09-14. Runtime `1.18.4` contains recovery-relevant workflow fixes, and runtime `1.18.2` fixed CVE-2025-69725 and CVE-2026-2303 in dependencies used by `daprd`. The official CLI checksum sidecar returns exactly:

```text
ccfff008fd16f50096a9192ad56697ac7052e3add6fa0a07789d87b4c4df8c40 *dapr_linux_amd64.tar.gz
```

However, Dapr's official support page says that only combinations in its packaged-release table are supported and currently lists runtime `1.18.0` with CLI `1.18.0`; it does not list the later patch releases as a packaged pair. The CLI supports an explicit `--runtime-version`, so the proposed pairing is executable in shape, but release existence alone is not proof of this repository's end-to-end fit.

**Disposition:** **already guarded; no spine edit required.** Keep `activation: pending` until the exact `1.18.2`/`1.18.4` pair passes the focused recovery/provenance verification named in AD-8 and Activation Preconditions. Do not describe the pair as “officially packaged” or “officially supported” unless Dapr updates its matrix or maintainers provide equivalent primary evidence.

### R-002 — Info, confirmed — Current GitHub enforcement is absent and the spine says so accurately

**Claim checked:** AD-6 says the transition check must become uniquely named and required from GitHub Actions on protected `main`; it explicitly forbids documentation from saying the check *is required* until independently verified.

**Evidence:** Live repository API checks found no repository or inherited rulesets and no classic protection on `main`. Current CI still exposes one `story-evidence-integrity` job to pull-request, push, schedule, and manual events. GitHub documents that required status checks are matched by check/job name and do not distinguish workflow or event trigger type, while an expected GitHub App can be selected as the source. This directly supports the chosen transition-only `story-transition-evidence-integrity` identity plus distinct scheduled/manual identity.

**Disposition:** **confirmed.** AD-6 is a necessary pending control, not a false statement of current enforcement. If a merge queue is later enabled, the workflow must also emit the required identity for `merge_group`; no merge-queue rule exists today, so this is a conditional implementation note rather than a spine defect.

### R-003 — Info, confirmed — Pending implementation deltas are complete and visibly fenced

**Claim checked:** The spine must not silently present its update as implemented.

**Evidence:** Repository reality still has all of the deltas named under Activation Preconditions:

- The CI job remains `story-evidence-integrity` on all four event classes rather than a transition-only `story-transition-evidence-integrity` check.
- `main` is unprotected and no ruleset requires the check.
- Completion recovery output still uses the broad `completion-recovery-evidence` artifact; there is no `hexalith.chatbot.recovery-primary-diagnostics/v1` closed inventory.
- There is no `recovery-cleanup-policy.json`, aggregate `hexalith.chatbot.recovery-cleanup-receipt/v1`, or sanitizer receipt binding.
- The workflow implements the existing 250/265/285 ladder and minute-330 deadline but not the new absolute 335/350/360 closeout sub-deadlines.
- CI still declares .NET `10.0.302`, Dapr CLI/runtime `1.18.0`/`1.18.0`, and mutable artifact-action major tags.
- Companion docs still carry the superseded Dapr/version/check wording.

The spine's frontmatter says `activation: pending`; its Activation Preconditions enumerate these same deltas; AD-6 uses “must be required” rather than “is required”; and Story 12.15 remains `review` in both its file and sprint status while TE-2 remains `in-review`.

**Disposition:** **confirmed.** The normative present tense inside AD Rules is read as a consistency contract, not a statement of current code. No rendered sentence needs correction for accidental implementation status.

## Version and immutable-reference matrix

| Authority | Spine value | Repository reality | Current official evidence | Assessment |
| --- | --- | --- | --- | --- |
| .NET SDK | `global.json` 10.0.400 feature band + `latestPatch`; 10.0.401 observed | Root `global.json` is exactly 10.0.400/`latestPatch`; local `dotnet --version` is 10.0.401; CI is still 10.0.302 | .NET 10 metadata reports `latest-sdk: 10.0.401`; Microsoft defines `latestPatch` as the highest installed patch in the same feature band | **Correct authority and current observation; CI reconciliation is properly pending** |
| Aspire AppHost SDK/catalog | 13.5.3 | AppHost SDK is 13.5.3; the committed Hexalith.Builds catalog aligns the Aspire packages to the 13.5.3 family | Official 13.5.3 release is marked latest; full release commit is `b5f143315ffb6968ea939a9978797a5b20e4c688` | **Current and fits repository** |
| Dapr CLI | 1.18.2 | Installer remains 1.18.0 pending activation | Official 1.18.2 release is latest; tag commit is `f5cf43ef4b3eb586fa4061413709085ee4e7d24c`; official Linux x64 checksum matches the spine exactly | **Current and authentic; implementation pending** |
| Dapr runtime | 1.18.4 | CI/release initialize 1.18.0 pending activation | Official 1.18.4 release is latest; tag commit is `6d1c53f430205c0c0f3bc3589ce5a3ec3f6f1647`; release notes contain relevant workflow recovery fixes | **Current; exact CLI/runtime pair needs activation verification (R-001)** |
| `actions/upload-artifact` | v7.0.1 at `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | Completion path still uses mutable `@v7` | Official v7.0.1 release is latest; release target and tag ref resolve to exactly that full SHA | **Exact and current; pinning pending** |
| `actions/download-artifact` | v8.0.1 at `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` | Completion path still uses mutable `@v8` | Official v8.0.1 release is current; release target and tag ref resolve to exactly that full SHA | **Exact and current; pinning pending** |
| GitHub Actions budget | 360 minutes | Completion job has `timeout-minutes: 360` | GitHub documents 360 minutes as the default job timeout and notes that runner execution limits can impose an earlier ceiling | **Confirmed** |
| Artifact retention | 30 days | Existing authoritative artifact uses `retention-days: 30`; proposed diagnostics not implemented | Live repo setting is 30 days with maximum 30; GitHub documents per-artifact `retention-days` subject to repo/org limits | **Confirmed and feasible** |

## Decision-by-decision reality check

| Decision | Evidence and fit | Result |
| --- | --- | --- |
| AD-1 — current-run-only recovery completion | Policy pins `current-run`, exact TRX/provenance paths, class selector, and lane; `CompletionProductionPlanner` enforces exact locator and at-most-one recovery declaration; focused tests cover retained rejection and path/cardinality drift | **Confirmed existing foundation** |
| AD-2 — exact-head production order | CI already resolves event base/head, checks out exact head, runs repository-owned planning before Dapr, stages raw TRX outside the canonical results root, then sanitizes/attests/validates; the new cleanup-receipt insertion is named as pending | **Confirmed foundation + accurately pending extension** |
| AD-3 — completion vs operational evidence | Scheduled/release live-recovery workflows feed `LiveRecoveryValidationEvidenceGate`; policy accepts only current-run for TE-2 `recovery-primary`; docs and tests reject retained substitution | **Confirmed** |
| AD-4 — disjoint diagnostic/completion artifacts | Current job permissions already resolve to `contents: read` and `actions: read`; broad current artifact shape demonstrates why the new closed-schema split is needed; live retention setting supports 30 days | **Reality-grounded pending decision** |
| AD-5 — absolute closeout deadlines | Current code supplies the 250-minute in-process deadline, 265-minute outer `SIGINT`, 15-minute kill-after, 285-minute step cap, 40-minute admission rule, 10-minute init cap, minute-330 closeout deadline, and 360-minute job cap. GNU semantics support the signal/kill ordering. The new 335/350/publication-floor allocation is not implemented and is explicitly an activation item | **Reality-grounded pending extension** |
| AD-6 — unique transition identity | Current workflow and live rules prove the collision and lack of enforcement; GitHub's documented matching/expected-source semantics support the selected remedy | **Confirmed need and platform fit** |
| AD-7 — aggregate cleanup receipt | Current producer/finalizer split lacks cross-process cleanup proof; sanitizer presently accepts only TRX input and binds no receipt. The chosen single-writer policy/receipt/sanitizer chain fits the existing planner → producer → sanitizer → attestor pipeline without inventing a parallel mutation path | **Reality-grounded pending decision** |
| AD-8 — repository-owned versions and immutable action refs | Every named repository authority exists; all version/hash/SHA values were checked against official sources and current repository files. The only fit caveat is R-001 and is already an activation gate | **Confirmed with guard** |

## Final disposition

No architecture correction is required from this lens. Preserve all Activation Preconditions and the `review` / `in-review` lifecycle states until repository changes and external branch enforcement are independently verified. In particular, successful dependency installation is not enough to clear R-001: activation evidence must exercise the exact Dapr CLI/runtime pair through the focused recovery/provenance path.

## Final rerun — 2026-09-14 after Reviewer Gate remediation

**This rerun supersedes the earlier verdict and dispositions in this file.**

### Final verdict

**PASS WITH ACTIVATION GUARD — zero Critical or High findings.** The revised spine remains grounded in current official platform evidence and current repository seams. It now makes the accepted-versus-active distinction explicit in both frontmatter and prose, so none of the newly adopted requirements can reasonably be read as a claim that today's workflow or GitHub rules already enforce them.

### Rerun evidence

- Re-read the full revised `ARCHITECTURE-SPINE.md`, its appended memlog decisions, and the unchanged brownfield workflow/tool/test seams.
- Re-ran authenticated live repository checks: inherited/repository rulesets remain `0`; `main` remains unprotected; Actions retention remains exactly `30/30` days.
- Re-resolved the official Dapr CLI checksum: `ccfff008fd16f50096a9192ad56697ac7052e3add6fa0a07789d87b4c4df8c40` still matches the Stack.
- Re-resolved official action tag refs: upload `v7.0.1` still resolves to `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`; download `v8.0.1` still resolves to `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c`.
- Local `dotnet --version` still resolves to `10.0.401` under the repository's `10.0.400` + `latestPatch` authority; the AppHost and committed package catalog remain aligned to Aspire 13.5.3.
- Rechecked current GitHub documentation for required-check naming, expected GitHub App source, fresh GitHub-hosted runner instances, repository-wide concurrency groups, `cancel-in-progress: false`, and the newer optional `queue: max` behavior.

### Gate-fix reality checks

| Revised area | Reality/version assessment | Final disposition |
| --- | --- | --- |
| AD-4 artifact authority | The two closed artifact channels remain prescriptive, while today's broad `completion-recovery-evidence` artifact supplies concrete brownfield evidence for the split. The live 30-day repository maximum admits both proposed uploads. | **Pass; activation-pending is accurate** |
| AD-5 publication floors | The existing 250/265/285 and absolute minute-330 controls still ground the timing model. The revised 335/350/355/360 ownership is new work, clearly named under Activation Preconditions. `always()` plus separately derived absolute deadlines is supported by GitHub Actions semantics. | **Pass; no current-state overclaim** |
| AD-6 required identity | `story-transition-evidence-integrity (pull_request)` is now an exact globally unique check-run name, always emitted on PRs, with push/schedule/manual identities disjoint. GitHub's name-only matching rule and expected-app feature support this design; live rules remain absent exactly as the activation text says. Merge-queue behavior is explicitly deferred. | **Pass** |
| AD-7 cleanup proof | The observation schema, exact identity tuple, positive postconditions, aggregate receipt, three named TRX properties, exact-byte SHA-256, and validator recheck fit the existing producer → sanitizer → attestor → validator pipeline. The current absence of those types is explicitly an activation item rather than concealed drift. | **Pass** |
| AD-9 destructive-run isolation | GitHub documents that standard `ubuntu-latest` jobs use fresh hosted VMs and that a shared concurrency key serializes jobs repository-wide. Existing recovery code already demonstrates CSPRNG per-run secrets and explicit `Testing` admission. The unified group, run-attempt identity binding, and endpoint/credential exclusions are correctly pending. With `cancel-in-progress: false`, a running cleanup is not canceled; default replacement of an older *pending* run does not endanger an already-mutating topology. | **Pass** |
| Activation record | The revised spine names the TE-2 ledger as the sole durable record and requires branch pattern, exact check name, GitHub Actions source/app, rule identifier, owners, and verification run identity. Current absence of protection therefore cannot be mistaken for activation. | **Pass** |

### Remaining non-blocking finding

**R-001 remains Medium and guarded:** Dapr CLI `1.18.2` and runtime `1.18.4` are still the current stable patch releases and the CLI archive hash is exact, but Dapr's published packaged-release matrix still lists `1.18.0`/`1.18.0` rather than certifying the mixed latest-patch pair. The revised Activation Preconditions continue to require focused recovery/provenance verification before activation, so this is not a Critical/High finding and needs no further spine change. The activation record must not characterize the pair as officially packaged unless the upstream matrix changes.

### Superseding disposition

No reality/version correction remains for the spine. Keep `activation: pending`, Story 12.15 at `review`, and TE-2 at `in-review` until every listed repository change, the exact Dapr-pair verification, protected-branch rule/source check, and recorded verification run are real. The revised document is safe to hand off as a prescriptive build substrate.

## Final closure rerun — 2026-09-14 (main target, no concurrency group, and cleanup codec)

**This closure verdict supersedes every earlier verdict and disposition in this review.**

### Closure verdict

**PASS WITH ACTIVATION GUARD — zero Critical or High reality/currency findings.** The latest refinements introduce no unsupported claim about current repository behavior, no invalid GitHub Actions assumption, and no version/hash regression.

### Closure checks

- **Protected-main target:** The exact authoritative identity is limited to pull requests whose base ref is `main`; other bases and event classes are explicitly non-authoritative. GitHub Actions exposes the pull-request base ref and supports an exact job/check name, while GitHub rules can require that named check from GitHub Actions and require pull requests before merging. Live repository checks still show no ruleset and no classic protection on `main`, and the spine continues to label rule creation and independent verification as activation work rather than present reality.
- **No GitHub concurrency group:** The revised AD-9 matches GitHub's documented behavior: even with `cancel-in-progress: false`, a concurrency group permits only one pending member and replaces the prior pending member when another is queued. Omitting the group therefore avoids losing a required pending run. Concurrent execution is technically coherent with the decision's fresh GitHub-hosted VM, job-local ephemeral topology/state, run-ID/attempt binding, generated secrets, `Testing` admission, and production-endpoint exclusions. Reuse of shared or self-hosted infrastructure is correctly fenced behind a new decision and external lease.
- **Cleanup codec:** Period-delimited names are valid unqualified XML attribute names; placing them on the namespace-qualified TeamTest `TestRun` root leaves the attributes unqualified as specified. One repository-owned `RecoveryCleanupTrxBinding` used by both sanitizer and validator is compatible with the existing StoryEvidenceGate seam and removes serializer/parser drift. Exact cardinality, allowed-name rejection, lowercase 64-hex digest validation, retained-receipt revalidation, and exact-byte SHA-256 matching are implementable requirements. The codec does not exist in the current codebase, and Activation Preconditions explicitly identify that work as pending.
- **Currency recheck:** The pinned .NET/Aspire/Dapr and artifact-action versions, full action SHAs, and Dapr CLI archive hash are unchanged from the independently verified matrix above. No newer or contradictory value was introduced by these refinements.

### Remaining disposition

There are **no remaining Critical or High findings**. R-001 remains the sole non-blocking Medium activation guard: the exact Dapr CLI `1.18.2` / runtime `1.18.4` pair must pass the named focused repository verification before activation because Dapr's packaged-release matrix does not currently certify that mixed patch pair. No architecture edit is required by this lens.
