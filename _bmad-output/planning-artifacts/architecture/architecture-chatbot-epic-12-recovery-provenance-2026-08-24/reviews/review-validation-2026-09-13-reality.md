# Validation review — reality and version currency

**Lens:** Reality check: every committed decision must be supported by the current repository, a current starter/package surface, or dated official evidence; technology versions and live defaults must not be asserted from model memory.

**Reviewed:** 2026-09-13

**Target:** `ARCHITECTURE-SPINE.md` for Epic 12 `recovery-primary` provenance

**Verdict:** **REVISE** — the provenance flow still maps to real code, but the finalized spine now binds an out-of-support .NET servicing level, a Dapr runtime with subsequently published CVE fixes, and timeout values that no longer match the workflow; its action “pins” and companion Aspire version also need explicit reconciliation.

## Evidence checked

Repository and history:

- `ARCHITECTURE-SPINE.md` and adjacent `.memlog.md`.
- Declared sources: `story-evidence-policy.json`, `.github/workflows/ci.yml`, `docs/story-evidence-integrity.md`, and Story 12.15.
- Declared companions: `_bmad-output/planning-artifacts/architecture.md` and `docs/adrs/live-recovery-validation-drivers.md`.
- Current `global.json`, `.github/scripts/install-dapr-cli.sh`, `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`, the shared package catalog, and the StoryEvidenceGate planner/attestor/validator/sanitizer types.
- `git status`, branch/remotes, recent history, blame, and the version-changing commits `c829ab6`, `a8b6765`, `3101dec`, `13195ef`, and `397507b`. The unrelated untracked `orient-extract.md` was left untouched.
- Public GitHub Actions history: [latest CI runs](https://api.github.com/repos/Hexalith/Hexalith.ChatBot/actions/workflows/ci.yml/runs?per_page=30) and sampled `story-evidence-integrity` jobs through [run 34752405983](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/34752405983/job/103711409524). No sampled post-finalization run executed the transition planner or recovery producer; the spine's hosted-run Deferred item therefore remains unresolved and correctly must not be presented as proven.

Official/current sources:

- Microsoft [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) and [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- Dapr [support policy](https://docs.dapr.io/operations/support/support-release-policy/), runtime releases [1.18.2](https://github.com/dapr/dapr/releases/tag/v1.18.2) and [1.18.4](https://github.com/dapr/dapr/releases/tag/v1.18.4), and CLI releases [1.18.0](https://github.com/dapr/cli/releases/tag/v1.18.0) and [1.18.2](https://github.com/dapr/cli/releases/tag/v1.18.2). Release-asset digests were checked through the official [CLI 1.18.0 release API](https://api.github.com/repos/dapr/cli/releases/tags/v1.18.0).
- GitHub's current [upload-artifact v7.0.1](https://github.com/actions/upload-artifact/releases/tag/v7.0.1), [download-artifact v8.0.1](https://github.com/actions/download-artifact/releases/tag/v8.0.1), [action-reference guidance](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/find-and-customize-actions), [workflow timeout syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax), and [artifact retention policy](https://docs.github.com/en/organizations/managing-organization-settings/configuring-the-retention-period-for-github-actions-artifacts-and-logs-in-your-organization).
- Official [Aspire 13.5.3 release](https://github.com/microsoft/aspire/releases/tag/v13.5.3), plus package API inspection of `Aspire.Hosting` 13.4.6 versus 13.5.3.
- GNU Coreutils [`timeout` semantics](https://www.gnu.org/software/coreutils/manual/html_node/timeout-invocation.html).

## Findings

### V-001 — The pinned .NET SDK is no longer a supported servicing level

- **Severity:** High
- **Location:** Spine Stack line 91; `.github/workflows/ci.yml` lines 173-176; `global.json` lines 2-4; companion architecture lines 313 and 698.
- **Evidence:** The spine and every CI `setup-dotnet` step still name `10.0.302`. Commit `c829ab6` deliberately moved the repository root to feature band `10.0.400` with `rollForward: latestPatch`; the current local resolver selects installed SDK `10.0.401`. Microsoft's dated release metadata identifies SDK `10.0.401` / runtime `10.0.12` as the 2026-09-08 security release, while `10.0.302` belongs to the superseded 2026-07-14 `10.0.10` release. Microsoft's support policy requires the latest patch for support.
- **Consequence:** The “pin” neither represents the repository's canonical SDK nor guarantees a supported hosted toolchain. On a runner without the 10.0.400 feature band preinstalled, installing only 10.0.302 cannot satisfy the root `global.json`; on a runner that already has it, ambient image contents choose the actual SDK. Completion evidence is therefore less reproducible than the Stack claims.
- **Recommended disposition:** **autofix** — align all CI setup declarations and the spine/companions to the current root feature band, then run the focused gate/build checks. Retain `latestPatch` only if automatic patch roll-forward is the intended and documented policy.

### V-002 — Dapr CLI/runtime 1.18.0 is stale, and the runtime predates published CVE fixes

- **Severity:** High
- **Location:** Spine Stack lines 94-95 and verified-evidence line 120; `.github/scripts/install-dapr-cli.sh` lines 5-14; `.github/workflows/ci.yml` line 284.
- **Evidence:** The old CLI archive digest is genuinely correct: the official 1.18.0 release API reports `sha256:2a94739e...1bfe`, exactly matching the spine and installer. Currency is not correct as of this review: the current CLI release is 1.18.2 and current runtime release is 1.18.4. More importantly, the official runtime 1.18.2 notes say its `go-chi` and MongoDB-driver upgrades resolve CVE-2025-69725 and CVE-2026-2303 and identify `daprd` as affected. The Dapr support page still shows the 1.18.0 packaged pair, but it was last refreshed before the later patch releases; it establishes support for the 1.18 minor line, not a reason to remain on the vulnerable initial patch.
- **Consequence:** The recovery-primary lane intentionally exercises Dapr actors/workflows and recovery behavior on an exact runtime with known fixed vulnerabilities and multiple reliability fixes missing. Its evidence can diverge from the maintained 1.18 runtime and unnecessarily exposes the CI sidecar.
- **Recommended disposition:** **discuss** — adopt the maintained 1.18 patch set (currently CLI 1.18.2/runtime 1.18.4), refresh the CLI archive digest, and re-run the live/focused provenance checks. Confirm the CLI/runtime patch pairing against the release notes before changing the spine.

### V-003 — AD-5's committed timeout contract no longer matches the implementation

- **Severity:** Medium
- **Location:** AD-5 line 73; `.github/workflows/ci.yml` lines 155, 288, 292-295, and 308-312.
- **Evidence:** AD-5 calls 265 minutes the in-process deadline and caps the live step at 280 minutes. Since commit `13195ef`, the workflow instead has a 250-minute in-process deadline, a maximum 265-minute external `SIGINT`, and a 285-minute step timeout. Blame and the workflow comment record the reason: separating the inner cleanup guard from the external interrupt so the in-process cleanup path remains reachable. The fixed closeout at minute 330, 40-minute admission refusal, 10-minute initialization limit, and final 30-minute job reserve still match.
- **Consequence:** Independently built units following the final spine could restore the obsolete numbers or write assertions that conflict with the current, deliberately revised cleanup layering. This is exactly the kind of divergence an epic spine is meant to prevent.
- **Recommended disposition:** **autofix** — in an Update run, append the post-finalization decision to the memlog and amend AD-5 to `250-minute in-process / 265-minute external interrupt / 285-minute step`, preserving its absolute minute-330 closeout rule.

### V-004 — Artifact action majors are current, but they are mutable tags rather than immutable pins

- **Severity:** Medium
- **Location:** Spine Stack lines 92-93 and the claim “These pins bind” at line 87; `.github/workflows/ci.yml` completion upload/download uses.
- **Evidence:** `actions/upload-artifact@v7` and `actions/download-artifact@v8` are the current majors. At review time the tags resolve to v7.0.1 commit `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` and v8.0.1 commit `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c`. GitHub's official guidance states that tags can move or be deleted and full commit SHAs are immutable. The workflow therefore accepts future action code without a repository change.
- **Consequence:** A trust-boundary component that downloads completion inputs and uploads retained evidence can change underneath an exact-head run, so the stack is current but not reproducibly pinned in the sense used by the spine.
- **Recommended disposition:** **discuss** — either use full verified SHAs with release comments and automated update review, or narrow the spine wording from “pins” to “tracked major lines” and explicitly accept the moving-tag trust model.

### V-005 — The declared Aspire companion is one release behind the producer it governs

- **Severity:** Medium
- **Location:** `docs/adrs/live-recovery-validation-drivers.md` lines 19, 145, and 290; companion architecture lines 316 and 876; current AppHost project line 1.
- **Evidence:** The ADR and companion architecture still bind Aspire 13.4.6. The current AppHost and centralized `Aspire.Hosting*` catalog use 13.5.3 after commit `a8b6765`; 13.5.3 is also the current official Aspire release. Package API inspection found `ResourceCommandService.ExecuteCommandAsync` and `KnownResourceCommands.StartCommand`, `StopCommand`, and `RestartCommand` in 13.5.3; the 13.4.6→13.5.3 API diff for resource-command types is additive only.
- **Consequence:** The named API still exists and fits, so this is not an implementation blocker, but reviewers are being told that hosted evidence and sandbox authority bind to a version the current producer no longer executes.
- **Recommended disposition:** **autofix** — update the ADR/architecture version evidence to 13.5.3 while preserving the existing resource-command decision; no API redesign is indicated.

## Technology and live-default evidence matrix

| Technology / behavior | Spine claim | Current repository | Official/current evidence | Assessment |
| --- | --- | --- | --- | --- |
| .NET SDK | 10.0.302 | CI says 10.0.302; root says 10.0.400 `latestPatch`; local resolves 10.0.401 | Microsoft release metadata: SDK 10.0.401/runtime 10.0.12 on 2026-09-08; latest patch required for support | **Stale and internally inconsistent — V-001** |
| Dapr CLI | 1.18.0 | Installer pins 1.18.0 and verifies archive | Latest CLI 1.18.2; old archive digest exactly matches official release API | **Authentic artifact, stale patch — V-002** |
| Dapr runtime | 1.18.0 | `dapr init --runtime-version 1.18.0` | Latest runtime 1.18.4; 1.18.2 contains explicit CVE fixes affecting `daprd` | **Known-fixed vulnerable baseline — V-002** |
| `actions/upload-artifact` | v7 | v7 | Latest v7.0.1 (`043fb46d...`) | **Current major; moving tag — V-004** |
| `actions/download-artifact` | v8 | v8 | Latest v8.0.1 (`3e5f45b...`) | **Current major; moving tag — V-004** |
| Aspire hosting/resource commands | Not in Stack; companions say 13.4.6 | AppHost/catalog 13.5.3 | Aspire 13.5.3 is current; required command APIs still exist and diff is additive | **Fits, documentation stale — V-005** |
| GitHub job/step limit | 360 minutes | Job 360; live step 285 | GitHub docs: job default 360, step maximum 360 | **Confirmed** |
| Artifact retention | 30 days, deletable/expiring | `retention-days: 30` | GitHub permits 1–90 days for public repos, subject to repo/org limits | **Confirmed for this public repository** |
| GNU `timeout` signal/kill behavior | `SIGINT`, then `--kill-after` | Used with dynamic deadline and 15-minute kill-after | GNU manual confirms kill-after starts after the first signal and failures are non-zero | **Semantics confirmed; runner tool version floats with `ubuntu-latest`** |
| Recovery-primary hosted execution | Deferred/unproven | Story remains `review`; sampled CI jobs skip planner/producer | Public run/job history through 2026-09-13 | **Deferred remains accurate; do not promote to proven** |

## Reality-checked decisions that still match

- AD-1's current-run-only source, exact paths, selector, locator, and single-consumer cardinality match the current policy/planner code.
- AD-2's plan-before-Dapr order, raw staging boundary, sanitizer, attestor, validator, and fail-closed job ordering remain present in the current workflow and tool code.
- AD-3's separation between completion provenance and scheduled/release A10 evidence remains present in policy, workflow, and documentation.
- AD-4's `contents: read` / `actions: read` permissions and metadata-only recovery projection remain implemented.
- The Dapr 1.18.0 CLI checksum, GitHub's 360-minute limit, 30-day retention input, and GNU timeout semantics were genuinely researched; their evidence is not fabricated. The version-currency findings above arise because the world and repository changed after 2026-08-24.

## Gate disposition

V-001 and V-002 should be resolved before the spine is treated as a current build substrate. V-003 and V-005 are clear brownfield reconciliation updates. V-004 is a deliberate trust-model choice: use immutable SHAs or state explicitly that moving official major tags are accepted. Preserve the hosted-execution limitation under Deferred until a transition-declared `story-evidence-integrity` run actually exercises and validates `recovery-primary`.
