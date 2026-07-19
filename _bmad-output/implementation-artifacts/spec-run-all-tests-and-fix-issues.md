---
title: 'Run all ChatBot tests and fix failures'
type: 'bugfix'
created: '2026-07-19'
status: 'in-review'
baseline_commit: '52f28f904983d481ef28777c144d6f157611d3be'
review_loop_iteration: 2
context:
  - '{project-root}/.github/workflows/ci.yml'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The current checkout has no complete, current proof that every ChatBot-owned test project passes. Environment-gated Aspire/DAPR and browser tests can also skip, masking incomplete evidence.

**Approach:** Restore and build the root solution in Release, execute every root-owned test project individually, then exercise the strict topology and browser lanes. Diagnose each failure, make the smallest root-owned correction, add or adjust regression coverage when behavior changes, and rerun the affected and full gates.

## Boundaries & Constraints

**Always:** Preserve all pre-existing user changes; use `Hexalith.ChatBot.slnx`; keep warnings as errors; run all 13 root test projects individually; distinguish product failures from missing environmental prerequisites; retain test assertions and non-vacuous security, tenant-isolation, accessibility, topology, and browser evidence; follow xUnit v3, Shouldly, NSubstitute, and existing project conventions.

**Ask First:** Any edit inside a `references/` submodule; any package/dependency version change; any weakening, exclusion, quarantine, snapshot acceptance, public-contract change, or architectural change; any destructive or persistent machine-wide test-environment change.

**Never:** Revert, clean, reformat, stage, or commit user-owned changes; initialize nested submodules; use a legacy `.sln`; hide failures with warning suppression, skip logic, broader filters, reduced assertions, or disabled analyzers; claim environment-skipped Tier-3/browser evidence as executed.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Full local gate | Release build and 13 root test projects | Every runnable test passes and results identify each project | Stop at the first actionable failure for focused diagnosis, then resume the matrix |
| Root-owned failure | Failure points to `src/`, `tests/`, or root configuration | Minimal correction plus focused regression evidence | Re-run the focused project before the full matrix |
| Dependency failure | Failure originates in `references/` | Preserve dependency contents and report the owning boundary | Halt before any submodule edit and request authorization |
| Missing runtime | Docker, DAPR, or Chromium is unavailable or sandbox-blocked | All independent lanes still run; strict lane reports the exact prerequisite/blocker | Do not convert the lane into a pass or add skip behavior |

</frozen-after-approval>

## Code Map

- `Hexalith.ChatBot.slnx` -- authoritative root build and 13-project test inventory.
- `Directory.Build.props` and `.editorconfig` -- Release, warnings-as-errors, language, and formatting gates.
- `.github/workflows/ci.yml` -- current broad CI and required topology-smoke contract.
- `tests/Hexalith.ChatBot.*Tests/*.csproj` -- individually executable ChatBot-owned test lanes.
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- three environment-gated Aspire/DAPR proofs.
- `src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs` -- source of the named non-proxied DAPR endpoint contract that tests must preserve while removing fixed launch-profile ports.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/` -- Playwright and live-render browser evidence.
- `src/` and `tests/` -- permitted root-owned repair surface, narrowed by observed failures.
- `references/Hexalith.Builds` and `references/Hexalith.PolymorphicSerializations` -- pre-existing dirty dependency content to preserve.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.ChatBot.slnx` -- restore and build Release serially -- establish a warnings-as-errors baseline before tests.
- [x] `tests/Hexalith.ChatBot.*Tests/*.csproj` -- run all 13 projects individually with `DiffEngine_Disabled=true` -- avoid solution-level test ambiguity and record per-project outcomes.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- isolate only the named sidecar-backed project `http` endpoints by reserving all required wildcard-capable TCP ports simultaneously, assigning each distinct reservation to matching `Port`/`TargetPort`, retaining reservations through model build, and releasing them only at the immediate application-start boundary; add focused assertions for exact selection, held uniqueness/availability exclusion, release behavior, and preservation of endpoint name/protocol/proxy semantics; permit a bounded fresh-reservation retry only for verified address-in-use startup contention -- eliminate fixed launch-profile collisions while satisfying Aspire's concrete proxyless-target-port contract.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/` -- run against a resolved Chromium executable and inspect skipped counts -- establish real browser evidence where the host permits it.
- [x] `src/`, `tests/`, and root configuration -- trace observed failures and apply minimal root-owned fixes with regression coverage, preserving the canonical story/technical-enabler mapping corrections that already proved successful -- restore behavior without broad cleanup.
- [x] `tests/Hexalith.ChatBot.*Tests/*.csproj` -- rerun focused failures, Release build, then the complete matrix -- prove no regression or remaining actionable failure.

**Acceptance Criteria:**
- Given the existing dirty workspace, when validation and repairs finish, then every pre-existing user-owned change remains intact and no dependency submodule was edited by this work.
- Given the Release solution, when all 13 root test projects run individually, then every executed test passes and each project has an explicit outcome.
- Given available Docker, DAPR, and Chromium prerequisites, when strict integration/browser lanes run, then all declared tests execute without unexpected skips and pass.
- Given a prerequisite blocked by the host, when the run is reported, then the exact command, failure/skip evidence, consequence, and successful independent lanes are identified separately.
- Given any root-owned defect found by the suite, when fixed, then focused regression evidence fails for the original bad state and passes after the correction.
- Given another process owns the launch-profile ports, when the strict topology starts, then the test holds distinct isolated reservations for the named sidecar-backed project endpoints until startup and all Tier-3 facts execute without changing production AppHost declarations.
- Given the test topology contains proxied, non-HTTP, container, or unrelated project endpoints, when isolation is applied, then those endpoints remain unchanged and the selected DAPR endpoints retain their name, protocol, and non-proxied contract.

## Spec Change Log

- **Iteration 1 — bad_spec:** Adversarial review found that the first topology repair bound and released ephemeral ports before startup, could reuse a port, rewrote every non-proxied project endpoint, and had no focused allocation invariant. The implementation task and acceptance criteria now require exact named-endpoint selection, Aspire/DCP-owned atomic distinct allocation, preservation of endpoint semantics, and focused selection/uniqueness regression evidence. This avoids reintroducing listener-timeout flakes, duplicate assignment, and accidental unrelated-endpoint mutation. **KEEP:** preserve the canonical Story 13.2 and TE-1 mapping corrections; preserve all existing assertions and timeouts; preserve strict zero-skip Tier-3/Chromium execution, the 13-project matrix, root-only edits, and untouched dirty submodules.
- **Iteration 2 — bad_spec:** Strict runtime evidence proved Aspire 13.4.6 rejects null `TargetPort` on proxyless endpoints, so DCP-owned atomic allocation cannot satisfy the required DAPR sidecar contract. The topology task now requires simultaneous held OS reservations with concrete matching host/target ports through model build, release only at the immediate startup boundary, exact endpoint selection, focused reservation lifecycle evidence, and bounded retry only for verified address contention. This avoids the known-bad null-target startup rejection while retaining non-proxied DAPR routing. **KEEP:** all Iteration 1 KEEP instructions plus the exact four-resource selection and full unselected-endpoint preservation assertions.

## Design Notes

The root solution includes selected production dependencies from submodules but no submodule test projects. “All tests” therefore means all 13 ChatBot-owned projects plus the strict ChatBot topology/browser execution; testing every independent repository under `references/` would be a separate multi-repository goal.

Tier-3 endpoint isolation must replace fixed launch-profile assignments only on the exact sidecar-backed project `http` endpoints. Reserve all replacement ports at once on wildcard-capable sockets, keep every reservation open while the application model is built, set each selected non-proxied endpoint's `Port` and `TargetPort` to the same reserved value, and release immediately before application startup. A retry must use an entirely fresh builder/reservation set, be bounded, and occur only when evidence identifies address contention; all other failures propagate unchanged. Do not mutate container, proxied, HTTPS, management, debug, or unrelated project endpoints, and do not extend readiness timeouts.

## Verification

**Commands:**
- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` -- expected: restore succeeds.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `for project in tests/Hexalith.ChatBot.*Tests/*.csproj; do DiffEngine_Disabled=true dotnet test "$project" --no-build --configuration Release || exit 1; done` -- expected: all 13 projects pass with per-project results.
- `HEXALITH_CHATBOT_TIER3=1 HEXALITH_CHATBOT_TIER3_REQUIRED=1 ChatBotServiceGrants__LifetimeDays=90 DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --no-build --configuration Release` -- expected: all integration tests, including all three Tier-3 facts, pass with zero skips.
- `CHROME_EXECUTABLE_PATH="$(command -v google-chrome || command -v chromium || command -v chromium-browser)" DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-build --configuration Release` -- expected: browser suite passes with zero unexpected skips.
- `git diff --check` -- expected: no new whitespace errors; pre-existing submodule findings are reported separately.

## Results

- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` succeeded with all projects up to date.
- Initial serialized builds stopped in the then-clean `references/Hexalith.FrontComposer` checkout at `3289f9fc12e6c7d3f1683366ef849b0002483339`: the root solution reported 146 Razor/compiler errors and direct Shell compilation reported 73, beginning with `FcFilterEmptyState.razor(10,19): RZ1021`. The user explicitly authorized focused FrontComposer submodule edits after this dependency boundary was reported.
- Before any authorized source edit, a concurrent external `/pushall` advanced the root and FrontComposer checkout to `550cb0602d506d9fd008a8c09f2cca6b328ec1e3`. None of the previously failing Shell Razor files changed between the failing and current commits, yet the direct Shell Release build and the authoritative root Release build both became green. The earlier Razor cascade therefore no longer reproduced and no speculative dependency source change was made.
- `dotnet build references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --no-restore --configuration Release -m:1 /nr:false --verbosity:minimal` passed with zero warnings and errors. The final `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -m:1 /nr:false` passed with zero warnings and errors in 1 minute 30.94 seconds.
- The complete 13-project Release `--no-build` matrix passed: 2,897 passed, zero failed, and the ordinary integration lane reported only its three expected explicit Tier-3 opt-in skips. Per project: AppHost 12; Architecture 63; CLI 24; Client 36; Conformance 97; Contracts 484; Integration 22 passed + 3 opt-in skips; MCP 30; Server 1,690; Testing 41; UI E2E 139; UI 227; Workers 32.
- The final strict Docker/DAPR integration lane passed 25/25 with zero skips in 3.4893 minutes. Runtime endpoint evidence showed distinct selected ports: `eventstore/http=35461`, `tenants/http=43731`, `chatbot/http=46121`, and `eventstore-admin/http=33649`.
- The three focused reservation lifecycle facts passed 3/3: exact endpoint selection and unselected preservation, wildcard-bind exclusion through model build plus release behavior, and address-contention-only retry classification.
- The final real Chromium UI E2E lane passed 139/139 with zero skips in 33 seconds using `/usr/bin/google-chrome`. Test-generated tracked screenshots were restored to their pre-run contents.
- Focused Release builds passed with zero warnings and errors for Integration, Architecture, and UI E2E; Architecture passed 63/63 and `git diff --check` passed.
- All execution tasks and acceptance gates are green. The concurrent external `/pushall` commit and submodule advances were not performed by this workflow; final validation ran against the resulting clean root checkout, and this workflow did not stage, commit, push, update dependencies, or modify FrontComposer source.
