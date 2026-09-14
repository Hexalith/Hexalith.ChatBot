**CHANGES REQUIRED — not valid as a current build substrate.**

# Architecture Spine Validation Report

**Target:** [Architecture Spine — Epic 12 recovery-primary provenance](ARCHITECTURE-SPINE.md)  
**Generated:** 2026-09-13  
**Intent:** Standalone BMad Architecture Validate  
**Mutation policy:** Validation made no changes to `ARCHITECTURE-SPINE.md` or `.memlog.md`.

## Gate summary

| Gate | Result | Meaning |
| --- | --- | --- |
| Mechanical conformance | **PASS** | Deterministic lint completed with zero findings. |
| Semantic and currency gate | **FAIL** | Current repository behavior, external enforcement, lifecycle ownership, and supported technology versions no longer converge with the finalized spine. |
| Deduplicated findings | **1 Critical · 6 High · 2 Medium · 0 Low** | Critical and High findings block use as a current build substrate. |

The spine's core exact-head, exact-path, current-run-only, single-writer attestation model remains coherent. It is not presently authoritative because its required check is neither externally enforced nor uniquely identified by transition context; the published artifact boundary has changed; the toolchain and timeout contract are stale; and cleanup/closeout ownership remains under-specified.

## Decision register — Critical and High

### ARCVAL-001 — Critical — The completion gate is neither externally enforced nor uniquely transition-scoped

- **Supports:** Rubric `R-001`; Adversarial `A-001`.
- **Local evidence:** [AD-2 calls the result “the required check”](ARCHITECTURE-SPINE.md#L51-L55), while the [TE-2 ledger](../../technical-enablers.md#L34-L52), [sprint workflow notes](../../../implementation-artifacts/sprint-status.yaml#L30-L37), and [developer guide](../../../../docs/story-evidence-integrity.md#L3-L5) say activation is still incomplete. The workflow [runs for pull request, push, schedule, and manual dispatch](../../../../.github/workflows/ci.yml#L3-L15) under one job identity, with schedule/manual mapped to `head..head`; that path can emit a successful `no-transition` result. The rubric reviewer queried live GitHub configuration on 2026-09-13: branch protection returned HTTP 404 `Branch not protected`, and effective branch rules and repository rulesets both returned empty arrays.
- **External evidence:** GitHub documents that [required status checks are identified by check/job name and do not distinguish workflow or event trigger type](https://docs.github.com/en/enterprise-cloud@latest/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/troubleshooting-rules#troubleshooting-required-status-checks).
- **Consequence:** A red transition verdict remains advisory, while an indistinguishable green no-transition check may be presented for the same commit. Repository administration and CI can each obey the written contract yet fail to enforce the intended completion transition.
- **Disposition — discuss:** Choose a transition-only required workflow or a distinct transition-gate identity, then record protected branches, expected source/app, owning team, current activation state, and verification evidence. Keep scheduled/manual diagnostics under a different identity. Until this is verified, describe the control as “must be required,” not “required.”

### ARCVAL-002 — High — A pre-validation operational artifact contradicts the declared publication boundary

- **Supports:** Rubric `R-002`.
- **Local evidence:** [AD-2 and AD-4](ARCHITECTURE-SPINE.md#L51-L67) define one post-validation retention path and exclude completion-run operational reports/manifests from upload and A10. Current test code writes an operational bundle under `TestResults/live-recovery/<runId>` and labels it `artifact:completion-recovery-evidence`; the [workflow uploads the `TestResults` tree](../../../../.github/workflows/ci.yml#L349-L395) before attestation/validation, then later uploads `story-evidence-integrity-reports` after validation. The [developer guide](../../../../docs/story-evidence-integrity.md#L112-L118) and [companion ADR](../../../../docs/adrs/live-recovery-validation-drivers.md#L133-L139) still describe the old boundary. The raw producer TRX remains outside upload, so the issue is authority and lifecycle ambiguity rather than a demonstrated raw-payload leak.
- **External evidence:** None required; the contradiction is between the current repository and its spine/companions.
- **Consequence:** Two archives now carry overlapping completion-run material without a shared authority rule. A consumer can mistake pre-attestation diagnostics for completion evidence or cite completion-run operational manifests as A10 evidence.
- **Disposition — discuss:** Either suppress the operational publication and retain only after TE-2 validation, or define a separately named, closed-schema, non-authoritative diagnostic artifact with an owner, retention/failure behavior, truthful layout, and executable exclusion from TE-2 retained evidence and A10.

### ARCVAL-003 — High — The completion toolchain has conflicting version authorities

- **Supports:** Rubric `R-003`; Reality `V-001`, `V-005`.
- **Local evidence:** The spine [pins .NET SDK 10.0.302](ARCHITECTURE-SPINE.md#L85-L95), as do CI setup steps, while [`global.json`](../../../../global.json#L1-L6) requires feature band `10.0.400` with `latestPatch`; the local resolver selected 10.0.401 and architecture tests assert 10.0.400. The live producer's [AppHost project](../../../../src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj#L1) uses Aspire 13.5.3, while the [companion ADR](../../../../docs/adrs/live-recovery-validation-drivers.md#L13-L19) and companion architecture still say 13.4.6; Aspire is absent from the spine's completion stack.
- **External evidence:** Microsoft's [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json) identifies SDK 10.0.401/runtime 10.0.12 as the 2026-09-08 security servicing release, and its [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) requires the latest patch for support. [Aspire 13.5.3](https://github.com/microsoft/aspire/releases/tag/v13.5.3) is current; reviewer package inspection found the required resource-command APIs still present with additive-only changes from 13.4.6.
- **Consequence:** Hosted completion can depend on mutable runner inventory to satisfy `global.json`, while reviewers reason from a different compiler and topology version than the producer actually executes.
- **Disposition — autofix:** Make `global.json` the sole .NET version authority (derive CI setup from it or assert exact agreement), update the spine and companions to the resolved policy, and add or explicitly inherit the AppHost/Aspire version authority.

### ARCVAL-004 — High — AD-5's exact timeout ladder is stale

- **Supports:** Rubric `R-004`; Reality `V-003`.
- **Local evidence:** [AD-5](ARCHITECTURE-SPINE.md#L69-L73) binds a 265-minute in-process deadline and a 280-minute live-step cap. Since commit `13195ef`, the [workflow](../../../../.github/workflows/ci.yml#L285-L320) deliberately uses 250 minutes in-process, a maximum 265-minute external `SIGINT`, and a 285-minute step timeout so the in-process cleanup guard remains reachable. The fixed minute-330 closeout, 40-minute admission refusal, 10-minute initialization limit, and 30-minute job reserve still match. The guide and ADR repeat the superseded values.
- **External evidence:** GNU documents the relevant [`timeout` signal and `--kill-after` semantics](https://www.gnu.org/software/coreutils/manual/html_node/timeout-invocation.html); the failure is repository drift, not an unresolved external semantic.
- **Consequence:** A maintainer following the final spine can restore obsolete values, collapse the 15-minute separation between in-process cancellation and external interruption, and recreate the cleanup race that the current workflow corrected.
- **Disposition — autofix:** In a BMad Architecture Update, append the post-finalization decision to the memlog and ratify `250 in-process / 265 external interrupt / 285 step / 330 closeout / 360 job`, including invariant inequalities and value ownership. Reconcile the guide and ADR in the same change.

### ARCVAL-005 — High — Dapr 1.18.0 is stale and predates published fixes for affected CVEs

- **Supports:** Reality `V-002`.
- **Local evidence:** The spine [pins Dapr CLI/runtime 1.18.0/1.18.0 and a CLI archive digest](ARCHITECTURE-SPINE.md#L89-L95); the installer and workflow still use those versions. Reviewer verification confirmed that the stored 1.18.0 CLI digest is authentic.
- **External evidence:** Current releases are [Dapr CLI 1.18.2](https://github.com/dapr/cli/releases/tag/v1.18.2) and [runtime 1.18.4](https://github.com/dapr/dapr/releases/tag/v1.18.4). The [runtime 1.18.2 notes](https://github.com/dapr/dapr/releases/tag/v1.18.2) identify `daprd` as affected while documenting dependency upgrades that resolve CVE-2025-69725 and CVE-2026-2303. The [Dapr support page](https://docs.dapr.io/operations/support/support-release-policy/) establishes support for the 1.18 minor line but predates the later patches.
- **Consequence:** Recovery-primary intentionally exercises actors/workflows on an exact runtime that omits known security and reliability fixes, so its evidence can diverge from the maintained 1.18 runtime and needlessly exposes the CI sidecar.
- **Disposition — discuss:** Confirm the supported CLI/runtime patch pairing, adopt the maintained 1.18 patches, refresh the CLI digest, and rerun the focused live/provenance checks before rebinding the spine.

### ARCVAL-006 — High — The 30-minute closeout reserve has no internal budget owner

- **Supports:** Adversarial `A-002`.
- **Local evidence:** [AD-5](ARCHITECTURE-SPINE.md#L69-L73) collectively assigns the final 30 minutes to Dapr cleanup, projection, attestation, validation, and upload but assigns no sub-deadlines or protected publication floor. In the [workflow closeout](../../../../.github/workflows/ci.yml#L321-L455), `dapr uninstall --all` has no step timeout, and the later phases have no absolute remaining-time admission rule.
- **External evidence:** GitHub's [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) confirms the surrounding job/step ceiling; the under-allocation is in the local contract.
- **Consequence:** Slow teardown can consume the whole reserve before failure evidence is retained, leaving neither complete cleanup nor the metadata needed to reconstruct the failure despite the spine promising both.
- **Disposition — discuss:** Allocate enforceable closeout sub-budgets or absolute deadlines, including a non-consumable finalizer/publication floor and the minimum metadata retained when teardown times out.

### ARCVAL-007 — High — Dapr teardown is not proof that producer-owned domain mutations were cleaned up

- **Supports:** Adversarial `A-003`.
- **Local evidence:** Story 12.15 treats any cleanup failure as stop-ship, but the spine's executable boundary says only “DAPR cleanup.” The live test's compensation path can log a partition-erase exception and continue to topology disposal; the outer [workflow observes only `dapr uninstall --all`](../../../../.github/workflows/ci.yml#L321-L323). The sanitizer allowlist and attestation sidecar contain no cleanup state or receipt. No AD defines the mutation set, terminal postconditions, quiescence check, or cleanup linearization point.
- **External evidence:** None required; the counterexample is demonstrated by current local ownership seams.
- **Consequence:** TE-2 can attest a passing canonical TRX while test-owned persistent state was not proven clean, poisoning later runs or leaving destructive validation state behind.
- **Disposition — discuss:** Define the producer-owned mutation set and a cleanup-complete receipt after restoration, compensating deletes, quiescence, and postcondition reads. Require the receipt before projection/attestation; missing, false, or timed-out cleanup must keep the check red and enter the bounded failure summary.

## Medium and Low tail

| ID | Severity | Supports | Finding | Recommended disposition |
| --- | --- | --- | --- | --- |
| ARCVAL-008 | Medium | Rubric `R-005` | The known `DW-124` lane-name/freshness-override provenance gap is absent from [Deferred](ARCHITECTURE-SPINE.md#L108-L113). Exact CI planning rejects a falsely named lane, but standalone attest/validate can grant the privileged 360-minute window from a contract-authored lane string without the same binding check. | **defer** — carry the existing owner, risk, and closure test into Deferred, or bind every age-overridden lane during standalone attestation. |
| ARCVAL-009 | Medium | Reality `V-004` | `actions/upload-artifact@v7` and `actions/download-artifact@v8` are current majors, but [the spine calls them “pins”](ARCHITECTURE-SPINE.md#L85-L93). GitHub states that tags may move; full commit SHAs are immutable. | **discuss** — pin reviewed full SHAs with release comments and automated updates, or explicitly accept tracked moving major tags and correct the spine's terminology. |

There are **2 Medium** and **0 Low** deduplicated findings. Neither Medium item alone blocks the current gate, but each needs an explicit update decision or bounded deferral.

## Reviewer-lens matrix

| Lens | Lens verdict | Original findings | Synthesized coverage |
| --- | --- | --- | --- |
| [Good-spine rubric](reviews/review-validation-2026-09-13-rubric.md) | Not valid as a current build substrate | `R-001` Critical; `R-002`–`R-004` High; `R-005` Medium | ARCVAL-001, -002, -003, -004, -008 |
| [Reality and version currency](reviews/review-validation-2026-09-13-reality.md) | Revise | `V-001`–`V-002` High; `V-003`–`V-005` Medium | ARCVAL-003, -004, -005, -009; Aspire portion of -003 |
| [Adversarial two-unit composition](reviews/review-validation-2026-09-13-adversarial.md) | Changes required | `A-001`–`A-003` High | ARCVAL-001, -006, -007 |

Severity was raised where a Medium currency observation corroborated a High brownfield contradiction (`V-003` into ARCVAL-004; `V-005` into ARCVAL-003). `R-001` and `A-001` were merged because both concern the same required-check boundary: one proves the control is absent, the other proves that enabling it by name alone would remain ambiguous.

## Confirmed strengths

- AD-1's current-run-only source, exact path/locator, and single-consumer cardinality still match policy and planner behavior.
- Planning still precedes Dapr setup; contracts, lifecycle, scope, mappings, paths, and cardinality fail closed.
- `ProvenanceAttestor` remains the sole TE-2 sidecar writer; validation re-hashes immutable inputs; raw recovery TRX stays outside upload paths.
- AD-3's separation of completion provenance from scheduled/release A10 evidence remains enforced.
- Read-only repository permissions, the authentic Dapr 1.18.0 CLI digest, the 360-minute job ceiling, and 30-day retention setting were independently confirmed.
- Hosted `recovery-primary` execution remains unproven after finalization and is correctly still Deferred.

## Evidence and method

1. The deterministic command `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24` passed with **0 findings**. This establishes formatting and structural mechanics only.
2. Three independent semantic lenses inspected the spine, adjacent memlog, declared sources/companions, current brownfield implementation, repository history/configuration, and—in the reality and adversarial lenses—dated official external evidence.
3. This report normalized severities, merged overlapping causes, preserved every reviewer ID in the matrix, and selected one disposition per synthesized finding. It does not reproduce every reviewer observation; the linked lens reports remain the evidence record.
4. Validation was read-only for the governed artifacts: `ARCHITECTURE-SPINE.md` and `.memlog.md` were not modified.

## Update decision

Offer: roll these findings into a **BMad Architecture Update**, preserving AD IDs and appending decisions to the existing memlog before re-distilling and rerunning the gate.

The minimum blocker set before the spine can return to **valid as a current build substrate** is:

1. **Enforcement and authority:** resolve ARCVAL-001 and ARCVAL-002—choose a uniquely transition-scoped external gate and one explicit diagnostic-versus-authoritative artifact contract.
2. **Executable lifecycle:** resolve ARCVAL-004, ARCVAL-006, and ARCVAL-007—ratify the current timeout ladder, allocate closeout deadlines, and bind producer-domain cleanup to a required receipt.
3. **Supported version baseline:** resolve ARCVAL-003 and ARCVAL-005—establish single version authorities and move the completion lane to supported .NET/Aspire/Dapr patch levels with focused verification.

ARCVAL-008 and ARCVAL-009 may be resolved in that Update or explicitly bounded under Deferred/accepted policy; they must not remain implicit. After the Update, rerun deterministic lint and all three reviewer lenses. Do not promote the hosted-duration Deferred item until a transition-declared hosted run actually exercises and validates `recovery-primary`.
