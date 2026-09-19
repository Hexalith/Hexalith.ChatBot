---
title: 'Align Chatbot CI/CD with the reusable Hexalith release method'
type: 'feature'
created: '2026-09-18'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/references/Hexalith.Builds/.github/workflows/ci-cd-standards.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Chatbot duplicates standard CI/release mechanics, runs release tests without building them, and has no package manifest, locked semantic-release toolchain, NuGet publication commands, or container publication path. Its three declared packages are currently absent from NuGet.org, and the current root gitlink for `Hexalith.Folders` is unavailable on GitHub, so CI fails during submodule initialization.

**Approach:** Adopt the thin reusable-workflow caller and publication tooling proven by Tenants, using current EventStore and FrontComposer implementations as comparison evidence. Keep only Chatbot-specific topology, recovery, and story-evidence gates locally, then validate the exact three NuGet packages and two SDK container mappings without claiming product readiness from package publication.

## Boundaries & Constraints

**Always:** Use Release/package-reference mode, enabled NuGet audit, project-level tests, root-only non-recursive submodule initialization, least-privilege job permissions, non-cancelling manual release concurrency, exact-current-main and exact-green-CI proof, a protected `production` environment, explicit secrets, one reviewed full Builds SHA for release, and fail-closed non-vacuous Chatbot operational gates. Preserve `release-metadata.json` readiness blockers and the current story-evidence contract.

**Never:** Use solution-level tests, `secrets: inherit`, floating release-workflow refs, `--skip-duplicate`, unconditional Dapr teardown after failed setup, nested submodule initialization, or sibling-specific sealed-policy machinery. Do not push, dispatch a release, publish artifacts, move tags, or weaken open readiness gates without explicit authorization.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| CI | Pull request or push to `main` | Shared Release build, package-consumer validation, per-project tests, and required Chatbot evidence run | Any missing, skipped, zero-test, or failed required lane blocks CI |
| Release | Manual dispatch at exact green `main` | Source preflight and Chatbot gates precede the pinned shared release for 3 packages and 2 containers | Stale/non-green source, missing secret, occupied version, or changed inventory fails before publication |
| Setup failure | Submodule or Dapr setup fails | Primary failure remains visible | Cleanup runs only when the tool was installed; no secondary `command not found` masking |
| Publication verification | Release completes | NuGet indexes contain all three IDs at one version and GitHub release assets match | Missing or mismatched IDs/assets fail verification |

</frozen-after-approval>

## Open Questions

- External completion boundary — options: implement and verify locally, then report the remote gitlink and publication steps for the user to perform (no commits, pushes, release dispatch, or publication) / authorize repository reconciliation, commits, pushes, and one protected release dispatch (changes external repositories and may publish immutable NuGet versions and container tags).
- Open readiness gates — options: allow library/container publication while `A5`, `A6`, `A9a`, and `A13` continue to block readiness claims (packages become public but no production-readiness claim is made) / enforce `release-metadata.json` as a publication freeze until those gates close (CI/CD is installed now, packages remain unpublished).

## Code Map

- `.github/workflows/ci.yml` -- replace standard build/test duplication with `domain-ci.yml@main`; retain Chatbot-only topology, recovery, and story-evidence jobs and their artifact contracts.
- `.github/workflows/release.yml` -- manual exact-source preflight, Chatbot operational gates, pinned `domain-release.yml`, explicit secrets, two containers, and post-publication proof.
- `references/Hexalith.Tenants/.github/workflows/{ci,release}.yml` -- canonical thin caller and source/publication verification pattern; adapt rather than copy literal pins or inventory.
- `references/Hexalith.Builds/.github/workflows/{domain-ci,domain-release}.yml` -- reusable input, secret, publication-freeze, and protected-environment contract.
- `.releaserc.json`, `package.json`, `package-lock.json` -- locked semantic-release/commitlint lifecycle; add pack, validate, publish, and GitHub assets.
- `tools/release-packages.json`, `release-metadata.json` -- authoritative publish inventory versus independent readiness posture.
- `scripts/{pack-release-packages.py,validate-nuget-packages.py,validate-consumer-package-references.py,validate-publication-preflight.sh,validate-release-secrets.sh}` -- manifest-driven package and immutable-destination validation.
- `tests/Hexalith.ChatBot.Architecture.Tests/{ScaffoldArchitectureTests.cs,LiveRecoveryValidationArchitectureTests.cs}` -- replace assertions for the obsolete direct release shape and add reusable-workflow/package-publication guards.
- `.github/workflows/{codeql,dependency-review,commitlint}.yml`, `.github/dependabot.yml` -- shared security callers and dependency automation.

## Tasks & Acceptance

**Execution:**
- [ ] Release tooling files above -- add the three-package manifest, locked Node toolchain, pack/metadata/consumer/preflight validators, explicit secret checks, NuGet push, container publication, and package release assets.
- [ ] `.github/workflows/ci.yml` and security automation -- adopt shared CI for standard work, preserve strict Chatbot evidence, guard teardown, and add CodeQL/dependency-review/commitlint/Dependabot callers.
- [ ] `.github/workflows/release.yml` -- implement manual exact-green-source release with a reviewed Builds pin, protected environment, explicit secrets, Chatbot exception gates, shared publication, and exact-result verification.
- [ ] Architecture tests and release metadata -- enforce the new caller, inventory, permissions, concurrency, evidence, and readiness separation.
- [ ] Root/submodule state -- ensure every recorded gitlink exists on its remote before remote CI is treated as valid.
- [ ] Official registries -- recheck NuGet and GitHub release assets after any authorized release; record exact package IDs/version or the publication blocker.

**Acceptance Criteria:**
- Given a pull request or push to `main`, when CI runs, then standard Release/package-mode build, consumer validation, and each declared test project execute through the reusable workflow while Chatbot-specific evidence remains fail-closed.
- Given a manual release of exact current green `main`, when all Chatbot gates and protected approval pass, then exactly Contracts, Client, and Testing plus the server/UI containers are published once from the tested source.
- Given an invalid source, inventory, secret, submodule, test result, or destination collision, when a release boundary is reached, then it fails before the first irreversible publication.
- Given publication completes, when official NuGet indexes and the GitHub release are queried, then all three package IDs share the released version and the expected package assets resolve.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `npm ci --ignore-scripts && npm audit signatures` -- locked release dependencies and provenance pass.
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- package-mode restore succeeds with auditing enabled.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -warnaserror -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- Release build succeeds.
- `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` -- exact package inventory and consumers pass.
- `bash .github/scripts/run-merge-test-lanes.sh` -- all ordinary test projects execute non-vacuously.
- `curl -fsS https://api.nuget.org/v3-flatcontainer/hexalith.chatbot.{contracts,client,testing}/index.json` -- after authorized publication, each official index resolves to the same version.
