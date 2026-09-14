---
review: configured-version-current-technology-reality
candidate: ../ARCHITECTURE-SPINE.md
companion: ../../architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md
reviewed_at: '2026-09-14'
candidate_sha256: '6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4'
candidate_status: final
companion_sha256: 'e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7'
verdict: PASS
finalization_blocked: false
blocking_findings: 0
architecture_advisory_findings: 0
architecture_recommended_remediation: 0
deferred_implementation_findings: 5
initial_verdict: BLOCK
---

# Configured Version and Current-Technology-Reality Review

## Verdict

**PASS — zero architecture blockers, advisories, or recommended remediations remain.** Candidate SHA-256
`6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4` has `status: final` and correctly distinguishes the current general
Dapr `1.18.0` CLI/runtime wiring from the accepted-but-pending Epic 12 recovery-primary target of CLI `1.18.2` and
runtime `1.18.4`. The Stack's current-upstream context now also records Dapr runtime `1.18.4`, matching GitHub's primary
release metadata for stable `v1.18.4`, published 2026-09-09. Companion SHA-256
`e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7` matches the PRD source-manifest pin. Its
exact Dapr and artifact-action stack is an accepted target, while `activation: pending` and its activation preconditions
truthfully prevent the current repository wiring from being treated as qualifying completion evidence.

This is an architecture-document verdict, not an implementation or release-readiness verdict. Nothing in this review
closes an assumption or qualifies a release. Newer patches are recorded for context only; they are not automatic upgrade
recommendations. The candidate's own dependency-governance rule remains the right control for any upgrade. This pass
does not authorize changing repository wiring or activating the Epic 12 target.

## Final Recheck Summary

| Finding | Final status | Current candidate evidence |
| --- | --- | --- |
| V1 SDK selector/resolution and CI conflict | PASS / closed | Stack identifies `10.0.400` + `latestPatch`, records resolved `10.0.401`, describes `LangVersion=latest`, and explicitly disqualifies CI/release `10.0.302` as uniform/hermetic evidence |
| V2 SDK/CLI/runtime/container planes | PASS / closed | Separate rows correctly identify current general CLI/runtime `1.18.0`, the pending Epic 12 `1.18.2`/`1.18.4` target, application SDK, and floating ASP.NET runtime base; current-upstream context correctly says runtime `1.18.4` |
| V3 prerelease/support boundary | PASS / closed | Keycloak, CommunityToolkit Dapr, and Fluent UI are named as prerelease pins; the upstream Dapr certification gap and no-readiness rule are explicit |
| A1 C# 14 status | PASS / closed | Classified as the effective language under the resolved .NET 10 SDK, not an explicit language pin |
| A2 newer upstream patches | PASS / no remediation | Current patches are accurately recorded only as context; dependency governance retains upgrade authority |
| A3 xUnit assertion drift | DEFERRED implementation drift | Deferred section records catalog `4.0.0` versus four UI checks expecting `3.2.2` and prohibits use of those lanes as evidence |
| A4 vulnerability qualification | DEFERRED implementation drift | Deferred section records `NuGetAudit=false` and prohibits a security-qualified package inference |
| Recovery companion identity | PASS / exact | Companion hash matches the source manifest; `status: final` is explicitly separated from `activation: pending` |
| Recovery immutable versions | PASS / target exact | CLI `1.18.2` digest matches the official Linux x64 asset; runtime `1.18.4` and action tags/SHAs match official releases; current Dapr/action wiring remains non-qualifying until activation |
| Preview/floating boundary | PASS / explicit | All three prerelease package pins are labeled; `aspnet:10.0-alpine` is labeled floating and candidate evidence must bind the resolved image digest |
| Open-gate posture | PASS / preserved | A5, A6, and A13 remain `OPEN`; A10 remains `OPEN / provisional`; A11 remains `OPEN / unsupported` |

The third deferred implementation finding is the checked-in CI/release `10.0.302` selector conflict. The fourth is the
current recovery jobs' general `1.18.0`/`1.18.0` wiring mismatch with the accepted Epic 12 `1.18.2`/`1.18.4` target.
The fifth is the workflows' current mutable `actions/upload-artifact@v7` and `actions/download-artifact@v8` wiring versus
the companion target's reviewed full SHAs. These are fully and accurately disclosed by the main spine's activation
boundary and the companion's explicit preconditions; fixing them is not required to make the architecture truthful, but
those lanes cannot qualify build or recovery completion-authority evidence while the drift remains.

GitHub identifies `v7.0.1` and `v8.0.1` as the current stable artifact-action releases, and their tag refs resolve to
the companion's full SHAs: [upload-artifact v7.0.1](https://github.com/actions/upload-artifact/releases/tag/v7.0.1) and
[download-artifact v8.0.1](https://github.com/actions/download-artifact/releases/tag/v8.0.1).

### R1 — current-upstream Dapr runtime comparator [CLOSED]

**Candidate evidence:** the Stack correctly records general CI/release CLI/runtime `1.18.0` as non-qualifying current
wiring and separately records the Epic 12 recovery-primary target as CLI `1.18.2`, runtime `1.18.4`, checksum
`ccfff008fd16f50096a9192ad56697ac7052e3add6fa0a07789d87b4c4df8c40`, and `activation: pending`. The companion
recovery spine contains the same target and checksum. The qualification paragraph correctly says current recovery jobs
cannot produce completion-authority evidence until aligned and independently activated.

**Previous defect:** the later sentence beginning “Current upstream patches reviewed on 2026-09-14” listed Dapr runtime
`1.18.2`, while official primary release metadata reported latest stable Dapr runtime `v1.18.4`, published 2026-09-09:
[Dapr runtime v1.18.4](https://github.com/dapr/dapr/releases/tag/v1.18.4). It likewise confirms CLI `v1.18.2` as the
latest stable CLI release: [Dapr CLI v1.18.2](https://github.com/dapr/cli/releases/tag/v1.18.2).

**Final evidence:** the current candidate now says `Dapr runtime 1.18.4` in that context sentence while preserving the
current general `1.18.0` wiring rows, the pending Epic 12 target row and checksum, the no-upgrade-authority wording, and
every qualification/gate boundary. R1 is closed.

## Review Basis

The review compared the full current candidate with:

- root selectors and build policy: `global.json`, `Directory.Build.props`, `Directory.Build.targets`, and
  `Directory.Packages.props`;
- the evaluated shared package catalog at `references/Hexalith.Builds/Props/Directory.Packages.props`;
- AppHost and host project declarations under `src/`;
- SDK, Dapr CLI, and Dapr runtime setup in `.github/workflows/ci.yml`, `.github/workflows/release.yml`, and
  `.github/scripts/install-dapr-cli.sh`;
- repository package-catalog assertions under `tests/`;
- the accepted, activation-pending Epic 12 recovery-provenance companion spine;
- the source-manifest hash that pins that companion;
- official Microsoft, Aspire, Dapr, GitHub Actions, and NuGet sources available on 2026-09-14.

Commands executed from the repository root included:

```text
dotnet --version
dotnet --info
dotnet msbuild src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj -getProperty:NETCoreSdkVersion -getProperty:TargetFramework -getProperty:LangVersion -getProperty:TargetFrameworkIdentifier -getProperty:TargetFrameworkVersion
dotnet msbuild Directory.Packages.props -getProperty:ManagePackageVersionsCentrally -getProperty:CentralPackageVersionOverrideEnabled -getItem:PackageVersion | jq <reviewed-package filter>
rg -n 'dotnet-version: 10\.0\.302|dapr init --runtime-version 1\.18\.0' .github/workflows/ci.yml .github/workflows/release.yml
rg -n 'readonly dapr_version|readonly dapr_sha256' .github/scripts/install-dapr-cli.sh
rg -n 'AssertUiFoundationPins|3\.2\.2|4\.0\.0' tests references/Hexalith.Builds/Props/Directory.Packages.props
rg -n 'PublishContainer|ContainerBaseImage|ContainerRepository|EnableContainer|IsPublishable' Directory.Build.targets src
sha256sum _bmad-output/planning-artifacts/architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
rg -n '1\.18\.2|1\.18\.4|ccfff008|activation: pending' _bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md
curl -fsSL https://api.github.com/repos/dapr/dapr/releases/latest | jq -r '[.html_url,.tag_name,.published_at,.prerelease,.draft] | @tsv'
curl -fsSL https://api.github.com/repos/dapr/cli/releases/latest | jq -r '[.html_url,.tag_name,.published_at,.prerelease,.draft] | @tsv'
curl -fsSL https://api.github.com/repos/actions/upload-artifact/releases/latest | jq -r '[.html_url,.tag_name,.published_at,.prerelease,.draft] | @tsv'
curl -fsSL https://api.github.com/repos/actions/download-artifact/releases/latest | jq -r '[.html_url,.tag_name,.published_at,.prerelease,.draft] | @tsv'
git ls-remote https://github.com/actions/upload-artifact.git refs/tags/v7.0.1
git ls-remote https://github.com/actions/download-artifact.git refs/tags/v8.0.1
curl -fsSL https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json | jq -r '[."latest-sdk",."latest-runtime",."latest-release"] | @tsv'
curl -fsSL https://api.nuget.org/v3-flatcontainer/<package>/index.json | jq <latest-stable filter>
rg -n 'uses: actions/(upload|download)-artifact@' .github/workflows/ci.yml .github/workflows/release.yml
```

The evaluated catalog reported central package management enabled, central overrides disabled, and every reviewed
package defined by the shared Hexalith.Builds catalog. The current machine resolved SDK `10.0.401`, target `net10.0`,
and `LangVersion=latest`.

## Remediation History — Initial Tier 1 Findings

The following findings caused the initial `BLOCK`. Their substantive architecture changes remain present; V2's version-
plane remediation is retained, while its current-upstream comparator is reopened by R1. The original rationale is kept
as concise audit history.

### V1 — The `.NET SDK / target` row describes an exact SDK that the repository does not select exactly

**Initial candidate claim:** Stack line 366 said `.NET SDK / target` was `10.0.400` / `net10.0`; the following paragraph called
`global.json` a non-CPM pin.

**Repository evidence:** `global.json:3-4` requests `10.0.400` with `rollForward: latestPatch`. The current checkout
therefore resolves `10.0.401`, confirmed by both `dotnet --version` and evaluated `NETCoreSdkVersion`. Microsoft defines
`latestPatch` as selecting the latest installed patch in the requested major/minor/feature band, not as an exact binary
pin. The current official .NET 10 download is SDK `10.0.401` with runtime `10.0.12` and C# 14.0:
[global.json roll-forward semantics](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) and
[.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

There is a second, more serious selector in the same repository. All six CI setup entries and all three release setup
entries explicitly request SDK `10.0.302` (`ci.yml:64,92,176,485,585,680` and
`release.yml:47,154,250`). That is the `10.0.3xx` feature band. `latestPatch` for the requested `10.0.400` baseline does
not roll down to or across from that band. A hosted job can appear to work only if its runner image already contains a
satisfying `10.0.4xx` SDK, making the explicit setup value ineffective and the job non-hermetic.

**Why this blocked finalization:** the initial table presented one exact current SDK and said the root file owned the
pin, while the root file actually owns a feature-band selector and the checked-in automation declares a conflicting
feature band. The architecture cannot ratify a uniform current build substrate without recording that distinction.

**Required remediation recorded at initial review:** replace the first two Stack rows and extend the qualification paragraph with
wording equivalent to:

```text
| .NET SDK selector / target | baseline `10.0.400` with `rollForward=latestPatch` (resolved `10.0.401` at the 2026-09-14 review) / `net10.0` |
| C# effective language | `14` under the current .NET 10 SDK; repository setting is `LangVersion=latest`, not an explicit `14` pin |

`global.json` is a 10.0.4xx feature-band selector, not an exact installed-SDK pin. Checked-in CI/release setup still
requests 10.0.302; until automation consumes global.json or installs a satisfying 10.0.4xx SDK, no uniform or hermetic
SDK-selection claim is made and no recovery/build evidence is qualified by this table.
```

Changing the workflows is an implementation/configuration remediation outside this architecture-only review. If they
are changed before finalization, the architecture should instead record the repaired selector and its evidence.

**Final recheck:** closed. The current Stack uses “SDK selector / target,” records baseline, policy, and resolved SDK,
and the following paragraph plus Deferred explicitly records and disqualifies the CI/release conflict.

### V2 — Application SDK, CLI, sidecar runtime, and container runtime are distinct version planes but only one is shown

**Initial candidate claim:** Stack line 369 listed only `Dapr .NET SDK 1.18.5`; lines 375-377 said exact recovery producers may
retain tool/runtime pins different from the application SDK. The structural diagram called the deployment artifacts
`SDK container images`.

**Repository evidence:**

- The centrally evaluated `Dapr.Client`, `Dapr.AspNetCore`, and `Dapr.Workflow` application packages are `1.18.5`.
- `.github/scripts/install-dapr-cli.sh:5-6` pins Dapr CLI `1.18.0` plus SHA-256
  `2a94739e0aa101289d88418225319562bc6800db273b3d9cf819a0efd1ea1bfe`.
- CI and release initialize sidecar runtime `1.18.0` in five jobs. These include ordinary topology acceptance as well
  as recovery jobs, so the tool/runtime pins are not recovery-only.
- Dapr CLI `1.18.0` is a real upstream release. The current Dapr runtime is `1.18.4`, while the current stable
  `Dapr.Client` package is `1.18.7`. The repository values are therefore deliberate brownfield/evidence pins, not the
  latest runtime and SDK patches:
  [Dapr CLI 1.18.0](https://github.com/dapr/cli/releases/tag/v1.18.0),
  [Dapr runtime 1.18.4](https://github.com/dapr/dapr/releases/tag/v1.18.4), and
  [Dapr.Client versions](https://www.nuget.org/packages/Dapr.Client).
- Server and UI images are produced by the .NET SDK container publisher but run on
  `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (`Directory.Build.targets:5`). That is a floating ASP.NET runtime image
  tag, not SDK `10.0.400`; the current .NET runtime patch is `10.0.12`. Microsoft distinguishes SDK publishing from the
  runtime base image in its
  [SDK container-publishing documentation](https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish).

**Why this blocked finalization:** readers could not determine which Dapr version governed compiled APIs, which installed the
local control plane, which sidecar actually executes topology/recovery tests, or which .NET runtime executes a published
image. The generic recovery exception is factually too narrow. It also weakens the candidate-binding language in AD-11
and AD-12 because a release evidence bundle must bind actual runtime/tool/image identity, not just the application SDK.

**Required remediation recorded at initial review:** add separate rows and an explicit evidence qualification, equivalent to:

```text
| Dapr application .NET SDK | `1.18.5` (shared catalog; current upstream stable is `1.18.7`; deliberate repository pin) |
| Dapr CLI | `1.18.0` plus checked-in SHA-256 (topology and recovery tooling) |
| Dapr sidecar runtime | `1.18.0` in CI/release topology and recovery jobs (current upstream runtime is `1.18.4`; deliberate evidence pin) |
| Published ASP.NET runtime base | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (floating 10.0 patch; bind resolved digest in candidate evidence) |
```

Replace “exact recovery producers may retain” with “topology/recovery producers own separately declared CLI, runtime,
and image identities.” State that evidence must record the resolved SDK, Dapr CLI/runtime, and container digest; none of
those identities is implied by `net10.0` or the Dapr NuGet version. Do not upgrade any pin merely to match the current
upstream patch without the repository's dependency-governance evidence.

**Final recheck:** closed. The version-plane and current-versus-target split is exact, R1 is corrected, and candidate
evidence must still bind the resolved identities and image digest.

### V3 — Two topology-defining packages and the Fluent UI line are prerelease dependencies without an explicit status boundary

**Initial candidate claim:** the Stack listed stable Aspire AppHost SDK `13.5.3` and Dapr .NET SDK `1.18.5`, while the local
topology showed Keycloak and Dapr sidecars. Fluent UI's `rc` suffix was visible but not classified.

**Repository evidence:** the AppHost directly references two additional topology-defining packages from the central
catalog:

- `Aspire.Hosting.Keycloak` = `13.5.3-preview.1.26425.3`;
- `CommunityToolkit.Aspire.Hosting.Dapr` = `13.5.0-preview.1.260825-0345`.

Both official NuGet pages explicitly mark those versions prerelease:
[Aspire.Hosting.Keycloak](https://www.nuget.org/packages/Aspire.Hosting.Keycloak/13.5.3-preview.1.26425.3) and
[CommunityToolkit.Aspire.Hosting.Dapr](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr/13.5.0-preview.1.260825-0345).
The current Aspire integration documentation also labels Keycloak “Preview” and the Dapr integration “Community
Toolkit.” More importantly, its published Dapr prerequisite statement says the toolkit package was tested with Dapr
runtime `1.15.3` and CLI `1.15.0`; it does not upstream-certify this repository's `1.18.0`/`1.18.0` combination:
[Aspire Dapr prerequisites](https://aspire.dev/integrations/frameworks/dapr/dapr-get-started/) and
[Aspire Keycloak integration](https://aspire.dev/integrations/security/keycloak/).

The UI package `Microsoft.FluentUI.AspNetCore.Components` really is pinned to `5.0.0-rc.5-26219.1`, and NuGet explicitly
marks it prerelease. The current stable line is `4.14.4`; the selected v5 RC is the newest listed v5 RC, not a GA v5
release: [Fluent UI Blazor 5.0.0 RC](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.5-26219.1).

**Why this blocked finalization:** the local topology depended on the omitted prerelease hosting integrations, not only
the stable Aspire AppHost SDK. The initial table could be read as a stable supported stack. The upstream Dapr statement is
absence of certification for the selected combination—not proof of incompatibility—but the architecture must not turn
repository package presence into an upstream support or qualification claim.

**Required remediation recorded at initial review:** add these rows and the boundary below them:

```text
| Aspire Keycloak hosting integration (preview) | `13.5.3-preview.1.26425.3` |
| CommunityToolkit Aspire Dapr integration (prerelease) | `13.5.0-preview.1.260825-0345` |
| Microsoft Fluent UI Blazor (prerelease) | `5.0.0-rc.5-26219.1` |

These are deliberate brownfield prerelease pins. The Dapr 1.18 topology combination is repository-local and requires
its own live topology evidence; current upstream integration documentation does not certify it. Package restore,
compilation, or topology declaration does not establish production support, release readiness, A10, A11, or A13.
```

**Final recheck:** closed. The current Stack names and classifies all three prereleases and includes the required
upstream-certification and no-readiness boundary.

## Final Tier 2 Recheck — Closed or Explicitly Deferred

### A1 — C# 14 is accurate as the current effective language, but not as a repository pin

`Directory.Build.props:25` says `LangVersion=latest`. With the resolved .NET 10.0.401 SDK, the official language support
is C# 14.0, so the candidate's value is currently correct:
[C# 14 documentation](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14). The V1 wording change is
needed so later readers do not mistake the effective result for an explicit language-version lock.

**Final recheck:** closed by the “C# effective language” row.

### A2 — Several exact repository pins intentionally trail newer stable patches; no automatic upgrade is warranted

The candidate consistently calls these repository pins and its Deferred section routes upgrades through dependency
governance. Current primary package reality is:

| Package | Repository/candidate | Current stable on 2026-09-14 | Review result |
| --- | --- | --- | --- |
| `Aspire.AppHost.Sdk` | `13.5.3` | `13.5.3` | exact and current |
| `Dapr.Client` family | `1.18.5` | `1.18.7` | deliberate older patch |
| Dapr runtime | current general wiring `1.18.0`; Epic 12 pending target `1.18.4` | `1.18.4` | split and current-upstream context are correct |
| `ModelContextProtocol` | `2.2.0` | `2.2.0` | exact and current |
| `System.CommandLine` | `2.0.11` | `2.0.12` | deliberate older patch |
| `xunit.v3` | `4.0.0` | `4.0.1` | deliberate older patch |

Primary version sources:
[Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk/13.5.3),
[ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol),
[System.CommandLine](https://www.nuget.org/packages/System.CommandLine), and
[xunit.v3](https://www.nuget.org/packages/xunit.v3).

There is no architecture defect merely because a newer patch exists. Retain exact repository values unless an upgrade
passes restore, build, conformance, and dependency governance.

**Final recheck:** no remediation remains. Upgrade authority remains correctly with dependency governance; R1 corrected
the factual comparator without requesting or authorizing an upgrade.

### A3 — The xUnit catalog adoption is internally inconsistent with four executable UI contract assertions

The candidate correctly matches the authoritative evaluated catalog at xUnit `4.0.0`. However,
`tests/PackageCatalogTestHelper.cs:26` still requires `3.2.2`, and
`AssertUiFoundationPins()` is called by four UI contract tests. This is deterministic repository drift, not an ambiguity
about the effective package version. It means neither the `4.0.0` table row nor a successful catalog evaluation is
evidence that the UI test suite passes.

No architecture version correction is required: `4.0.0` is the actual central pin. The repository test expectation
should be reconciled separately through dependency governance. Until then, preserve the candidate's existing rule that
version/package presence does not imply executable support or readiness. No test was run in this read-only reviewer task.

**Final recheck:** explicitly deferred in the current spine; no architecture correction remains.

### A4 — Dependency vulnerability status was not established

`Directory.Build.props:28` sets `NuGetAudit=false`. Official package pages were checked for release/version status, but
this review did not produce an audited vulnerability disposition. The candidate does not currently claim that it did,
so this is not a finalization blocker. Do not infer a security-qualified dependency set from this review.

**Final recheck:** explicitly deferred in the current spine; no architecture correction remains.

## Tier 3 — Topology and Capability Claim Check

The non-version topology language is appropriately bounded and should be preserved:

- `Hexalith.ChatBot.AppHost.csproj` is `IsPublishable=false` and calls itself a local-development shim. The current
  Structural Seed and AD-17 say the same and do not claim that the AppHost is the deployed production topology.
- The AppHost actually composes ChatBot Server/UI, EventStore, Tenants, Memories, Redis-backed Dapr components,
  Keycloak, and sidecars. The local topology seed is therefore grounded in code.
- Server and UI projects opt into .NET SDK container publishing and name container repositories. The M2 image node is a
  plausible target, but no deployment or release readiness follows. Its remediated label, “SDK-produced Server/UI
  runtime images,” avoids implying that published images contain the SDK.
- Candidate AD-2 explicitly says the atomic EventStore/idempotency/audit/fencing target is not current support. AD-3,
  AD-6, AD-7, AD-12, and AD-15 keep owner mappings and executable cross-context behavior A13-pending. The diagrams also
  label the atomic target and owner command A13-gated. These boundaries prevent package/topology presence from implying
  the missing capability.
- Candidate AD-11 keeps recovery activation pending and A10 provisional. The remediated runtime/tool/digest wording
  makes that evidence boundary operationally unambiguous. Current general `1.18.0`/`1.18.0` wiring remains distinct from
  the Epic 12 `1.18.2`/`1.18.4` target, whose activation is still pending.
- The source-manifest-pinned recovery companion also keeps its reviewed artifact-action SHAs behind explicit activation
  preconditions. Current mutable major-tag workflow references therefore establish neither companion activation nor
  completion authority.
- AD-18 adopts the OpenAPI authority and pre-merge/release parity obligations without claiming the generator, parity
  tests, owner mappings, or release qualification are implemented; the document-wide adopted-versus-ready boundary and
  A13 gate remain controlling.
- Release Gates correctly retain A5 `OPEN`, A6 `OPEN`, A13 `OPEN`, A10 `OPEN / provisional`, and A11
  `OPEN / unsupported`. No line reviewed supplies evidence to change any of them.

## Finalization Gate

**PASS.** All initial architecture blockers and R1 are remediated. Selector versus resolution, explicit CI conflict,
prerelease boundaries, container/runtime separation, and the current-versus-target recovery split remain exact. A3, A4,
the CI SDK conflict, current recovery-wiring drift, and mutable artifact-action references are truthful, bounded
implementation/configuration deferrals that the architecture expressly excludes from qualifying evidence. The local AppHost remains
non-publishable, the M2 topology remains a production-shaped target rather than a deployed claim, AD-18 does not assert
implementation readiness, and atomic/owner behavior remains A13-gated.

A5 remains `OPEN`; A6 remains `OPEN`; A10 remains `OPEN / provisional`; A11 remains `OPEN / unsupported`; A13 remains
`OPEN`. This reviewer PASS confirms the final architecture document only; `status: final` supplies no implementation,
qualification, or release-readiness evidence.
