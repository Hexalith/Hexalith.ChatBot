---
title: Pack only declared ChatBot release packages
type: bugfix
created: 2026-10-23
status: in-review
route: oneshot
review_loop_iteration: 0
baseline_commit: 0fe0c477e49e90d3edd2fb80229beac5132feda9
context: []
---

<frozen-after-approval reason="user authorized CI/CD repair without human validation">

## Intent

Make the existing release pack script produce exactly the declared ChatBot package inventory. The solution includes packable dependency-submodule projects, so packing the solution creates unrelated packages and violates the script's own three-package guard. Pack each project declared by release-metadata.json, preserve Release package-reference mode and symbol packages, and verify the exact IDs/version. A prerelease NuGet version must use its numeric core as the assembly file version. No publication or release-gate policy changes belong to this repair.

</frozen-after-approval>

## Implementation Notes

- `dotnet msbuild references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -getProperty:IsPackable -p:Configuration=Release -p:UseHexalithProjectReferences=false` returned true; that project is a member of Hexalith.ChatBot.slnx.
- Source of truth: release-metadata.json inventory.packages (Contracts, Client, Testing). Use it directly rather than copying the inventory.
- Validate by packing temporary 0.0.0-ci artifacts, inspecting all three nuspec IDs/versions, checking dependency versions and symbol inventory, and shell syntax. Do not publish temporary validation packages.
- Review together with the other CI/CD repairs. Existing standard-release M2 atomic support remains blocked and requires the pending user scope decision.

## Verification and Review

- `bash .github/scripts/pack-release-packages.sh /tmp/chatbot-release-investigation/packages-0.0.0-ci 0.0.0-ci`: exit 0. Produced exactly Hexalith.ChatBot.Contracts, Hexalith.ChatBot.Client, and Hexalith.ChatBot.Testing at 0.0.0-ci, with one nupkg and snupkg per ID.
- ZIP/nuspec inspection verified exact ID/version, net10.0 library, and six-artifact inventory. Client and Testing depend on Contracts 0.0.0-ci. Other dependencies are ByteAether.Ulid 1.4.2 and Client's Newtonsoft.Json 13.0.4.
- Independent temporary consumer referenced all three local packages: NuGet restore and Release build exited 0, with no warnings/errors. Temporary packages were never published.
- Unsupported 1.2.3.0 and 01.2.3 versions return exit 1 before packing. `bash -n` and `git diff --check` pass.
- Independent review triage is recorded in [the fixture repair spec](spec-ci-owner-authority-fixtures.md). Patched canonical-version validation and exact symbol-package cardinality; no findings deferred.

Completion posture: implementation and local verification passed; repository completion-evidence enrollment is not claimed. The plan remains in review under docs/story-evidence-integrity.md until its required evidence is available.
