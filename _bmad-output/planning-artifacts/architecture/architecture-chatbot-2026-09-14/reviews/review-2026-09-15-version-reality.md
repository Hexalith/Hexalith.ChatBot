---
review: configured-version-current-technology-reality
candidate: ../ARCHITECTURE-SPINE.md
reviewed_at: '2026-09-15'
candidate_sha256: '9155211949746540ec7b8bb76113fe8c49c50b9236705a2e8399ef55b65d4bf3'
previous_review: review-version-reality.md
previous_reviewed_candidate_sha256: '6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4'
rechecked_from_candidate_sha256: '46bfeb832eb7bece0b885290edca3e6a70973942c67dcfa6ab5d7e74fe7ba8b8'
verdict: PASS
finalization_blocked: false
high_findings: 0
medium_findings: 0
new_technology_or_version_claims_in_update: 0
---

# Configured Version and Current-Technology-Reality Review — 2026-09-15

## Verdict

**PASS — no version/reality finding remains.** The 2026-09-15 architecture correction introduces no
technology selection, package version, starter, hosting, or deployment change. The current Stack rows and their
support/qualification boundaries are the previously reviewed brownfield pins. Post-review adversarial fixes add no
technology choice or fit assertion; the technology-adjacent edits only make qualification labels and fail-closed
boundaries more precise. The new admission-profile, classifier, correction-manifest, and split-gate decisions are
grounded in the finalized PRD, approved addendum, and approved Sprint Change Proposal rather than technology claims
inferred from training data.

The selected repository pins, upstream comparators, and named integration fits pass current repository and
primary-source checks. In particular, official NuGet pages confirm both `xunit.v3 4.0.1` and `xunit.v3.core 4.0.1`,
published 2026-09-12. The spine accurately distinguishes the repository's selected `xunit.v3 4.0.0` from current
upstream `4.0.1`; dependency governance still owns any upgrade.

This is an architecture truthfulness verdict only. It does not request dependency upgrades, activate the Epic 12
recovery target, close A5/A6/A9a/A10/A11-M1/A11-M2/A13, or establish implementation or release readiness.

## Update-delta classification

The post-adversarial-fix worktree candidate SHA-256 is
`9155211949746540ec7b8bb76113fe8c49c50b9236705a2e8399ef55b65d4bf3`. This recheck supersedes the earlier 2026-09-15
snapshot at SHA-256 `46bfeb832eb7bece0b885290edca3e6a70973942c67dcfa6ab5d7e74fe7ba8b8`; the prior configured baseline review records
candidate SHA-256 `6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4`.

Repository diff inspection shows:

- no Stack row or selected package/tool/runtime/container version changed;
- no new starter, framework, data store, transport, cloud, or deployment technology was introduced;
- the Stack qualification sentence changed only from unsplit `A11` to the now-correct `A11-M2` token;
- AD-2 changed command-admission profiles, AD-7 changed classifier semantics, AD-9 expanded the normative correction
  manifest, and AD-12 split A11-M1/A11-M2 and bound the immutable gate registry; those are source-contract corrections,
  not claims about technology currency or fit; and
- all existing caveats remain: prerelease dependencies are labeled, general Dapr wiring is separated from the pending
  recovery target, floating container identity requires candidate binding, and code/package presence never proves
  production support or qualification.

Accordingly, all committed technology selections, version statements, and fit claims remain repository- or
primary-source checked. The earlier xUnit advisory was based on stale package-index evidence and is withdrawn by this
recheck.

## Findings

None.

## Verified unchanged Stack and currency boundary

| Technology plane | Repository/candidate selection | Current source check on 2026-09-15 | Result |
| --- | --- | --- | --- |
| .NET SDK / C# | `global.json` baseline `10.0.400` + `latestPatch`; local/evaluated SDK `10.0.401`; `net10.0`; `LangVersion=latest` → C# 14 | Microsoft lists SDK `10.0.401` and C# 14 for .NET 10; CI/release still explicitly requests conflicting `10.0.302`, which the spine disqualifies | **PASS** |
| Aspire AppHost SDK | `13.5.3` | NuGet lists `13.5.3`; AppHost project uses `Aspire.AppHost.Sdk/13.5.3` | **PASS** |
| Aspire Keycloak hosting | `13.5.3-preview.1.26425.3` | Exact NuGet version exists and is explicitly prerelease; AppHost consumes it through the EventStore security topology | **PASS** |
| CommunityToolkit Aspire Dapr hosting | `13.5.0-preview.1.260825-0345` | Exact NuGet version exists; a later `13.5.1-beta.757` does not invalidate this labeled brownfield prerelease pin; repository uses its Dapr component/sidecar APIs | **PASS — deliberate older prerelease** |
| Dapr application SDK | `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Workflow` `1.18.5` | Catalog and server references agree; NuGet current stable is `1.18.7`, already disclosed as context only | **PASS — deliberate older patch** |
| Dapr general CLI/runtime | `1.18.0` / `1.18.0` | Installer checksum and CI/release `dapr init` declarations agree; correctly labeled non-qualifying current wiring | **PASS** |
| Dapr Epic 12 target | CLI `1.18.2`, runtime `1.18.4`, fixed CLI archive checksum, `activation: pending` | Official latest releases are CLI `v1.18.2` and runtime `v1.18.4`; companion hash matches the source-manifest pin | **PASS — target, not current wiring** |
| ASP.NET runtime container | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` | `Directory.Build.targets` agrees; floating patch is explicitly disclosed and release evidence must bind its resolved digest | **PASS — floating by design** |
| Fluent UI Blazor | `5.0.0-rc.5-26219.1` | Exact prerelease exists and is the latest listed v5 RC; repository UI consumes it; current stable line remains 4.14.4 | **PASS — deliberate prerelease** |
| ModelContextProtocol | `2.2.0` | Exact/current NuGet package exists; MCP adapter consumes it with stdio server transport | **PASS** |
| System.CommandLine | repository `2.0.11`; upstream context `2.0.12` | Catalog/CLI agree on `2.0.11`; NuGet lists `2.0.12` current stable | **PASS — deliberate older patch** |
| xUnit v3 | repository/meta-package `4.0.0`; upstream context `4.0.1` | Catalog/tests agree on the selected `4.0.0`; official NuGet lists `xunit.v3 4.0.1` and `xunit.v3.core 4.0.1`, both published 2026-09-12 | **PASS — deliberate older patch** |

Primary current-version sources:

- .NET downloads and SDK/language status: <https://dotnet.microsoft.com/en-us/download>
- Aspire AppHost SDK: <https://www.nuget.org/packages/Aspire.AppHost.Sdk>
- Aspire Keycloak integration: <https://www.nuget.org/packages/Aspire.Hosting.Keycloak/13.5.3-preview.1.26425.3>
- CommunityToolkit Aspire Dapr integration: <https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr>
- Dapr .NET client: <https://www.nuget.org/packages/Dapr.Client>
- Dapr CLI/runtime: <https://github.com/dapr/cli/releases/tag/v1.18.2>,
  <https://github.com/dapr/dapr/releases/tag/v1.18.4>
- Fluent UI Blazor: <https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components>
- MCP C# SDK: <https://www.nuget.org/packages/ModelContextProtocol>
- System.CommandLine: <https://www.nuget.org/packages/System.CommandLine>
- xUnit v3 meta-package and core: <https://www.nuget.org/packages/xunit.v3/4.0.1>,
  <https://www.nuget.org/packages/xunit.v3.core/4.0.1>

## Named-technology fit verification

| Spine fit claim | Reality evidence | Result |
| --- | --- | --- |
| EventStore domain-service host and inward module boundary | Server calls `AddEventStoreDomainService` / `UseEventStoreDomainService` and references the root-declared EventStore source project; source manifest records the unsupported atomic-audit gaps rather than claiming fit beyond current evidence | **PASS** |
| Dapr Workflow for correction/coordinator activities | Server references `Dapr.Workflow`, registers `AddDaprWorkflow`, and implements hosted workflow/activity types; AppHost provisions a separate actor-capable workflow state store | **PASS** |
| Dapr Redis state/pubsub composition | AppHost declares `state.redis` components for EventStore, ChatBot read/idempotency, and workflow state plus `pubsub.redis`; state and pub/sub roles match the structural seed | **PASS** |
| Keycloak OIDC through the Aspire/FrontComposer boundary | AppHost composes EventStore security/Keycloak and the UI configures FrontComposer OIDC with Keycloak without embedding a browser client secret | **PASS** |
| FrontComposer + Fluent UI v5 composition | UI calls `AddFluentUIComponents`, `AddHexalithFrontComposerQuickstart`, and `AddHexalithDomain`; source manifest confirms FrontComposer's shell/progress fit | **PASS** |
| OpenAPI 3.1 + NSwag typed client | Contracts tests assert the OpenAPI foundation; Client invokes `NSwag.MSBuild`; checked-in generated client identifies the NSwag toolchain | **PASS** |
| MCP stdio and System.CommandLine adapters | MCP calls `AddMcpServer().WithStdioServerTransport()` and CLI uses `System.CommandLine`; both depend on the typed Client rather than data stores | **PASS** |
| SignalR as advisory projection nudge | Server registers SignalR and a metadata-only hub/publisher; contracts/tests preserve re-query authority and prohibit treating the nudge payload as state | **PASS** |
| Memories as optional M2-governed provider | Current local AppHost and server contain Memories adapters, but the spine defers production/provider activation and keeps A5/A6/isolation/correction qualification explicit; local topology presence is not represented as release activation | **PASS** |

The Dapr/Aspire fit is deliberately repository-qualified rather than presented as upstream certification. Aspire's
current integration documentation still describes the Dapr sidecar/component model but names an older audited Dapr
pair than this repository's 1.18 topology, which supports the spine's “requires its own live evidence” boundary:
<https://aspire.dev/integrations/frameworks/dapr/dapr-get-started/>.

## Existing implementation drift correctly disclosed

The following are not new architecture findings because the candidate already records and disqualifies them:

- CI/release `10.0.302` conflicts with the root 10.0.4xx selector;
- current recovery jobs use general Dapr `1.18.0/1.18.0` rather than the pending Epic 12 `1.18.2/1.18.4` target;
- four UI package-catalog assertions still expect xUnit `3.2.2` while the shared catalog owns `4.0.0`;
- vulnerability qualification remains unavailable while `NuGetAudit=false`; and
- mutable `actions/upload-artifact@v7` / `actions/download-artifact@v8` workflow references do not satisfy the
  companion's reviewed full-SHA activation target.

These defects continue to prevent the affected lanes from serving as qualification evidence; they do not make the
spine's explicit non-qualification posture false.

## Review basis

Repository checks covered `global.json`, evaluated MSBuild SDK/TFM/language properties, the shared Hexalith.Builds
package catalog, AppHost/server/UI/CLI/MCP project and composition sources, CI/release Dapr and SDK declarations,
container base configuration, current root-declared submodule revisions, the source manifest, the Epic 12 companion and
its manifest-pinned SHA-256, and the full candidate diff. Current-version checks used official Microsoft, NuGet, Dapr,
Aspire, xUnit, and package-owner sources listed above.

No claim was accepted solely because it appeared in the prior review. Unchanged Stack pins were rechecked against the
repository and current primary sources; new source-contract decisions were traced to the approved planning inputs.

## Acceptance condition

This reviewer is **PASS**. No technology/version/fit correction or dependency upgrade is required. Any future candidate
change must be rebound to a new SHA-256 and rerun through repository and current-primary-source checks.
