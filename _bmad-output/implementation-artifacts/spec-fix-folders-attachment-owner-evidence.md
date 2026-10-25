---
title: 'Read owner evidence before Folders attachment submission'
type: 'bugfix'
created: '2026-10-25'
status: 'in-review'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Fix the seven deferred FoldersFolderStoreTests failures by obtaining owner-observed expectedFileVersion and lock-fence evidence through supported Folders APIs. Preserve the authorized uncommitted starting state, owner authorization, fencing, and stable SDK idempotency. Do not infer version zero from absence, invent a fence, stage, commit, or push. Use Debug source references; run focused Folders tests followed by the full Server project and report remaining blockers. Story 1.6 remains in review pending accepted A6 protection and native-isolation evidence.

</frozen-after-approval>

## Implementation Notes

- Baseline: `0468d19e56752bb70add6a8f885cfa8de0124260` plus the explicitly authorized uncommitted changes. Existing modified/untracked file hashes captured in `/tmp/chatbot-folders-starting-files.json`.
- Small ChatBot-only repair: `FoldersFolderStore.cs`, its tests and separate test-double types. Read exact-path `GetFolderFileMetadataAsync` with task snapshot consistency and `GetWorkspaceLockAsync` with read-your-writes consistency. Validate visible positive version, exact scope, task holder, active lease, freshness and positive fence before building the upload descriptor and canonical mutation. Keep observation, mutation building and upload inside safe failure handling. Owner admission remains authoritative for Add versus replay/conflict.
- Owner constraints verified in `references/Hexalith.Folders/src/Hexalith.Folders.Server/Content/AuthoritativeWorkspaceFileContextSource.cs` and `WorkspaceContentAuthority.cs`: 404 conflates missing and forbidden paths; fresh Add requires absent-path version zero; replay requires original preconditions. Current visible metadata cannot establish either. Never manufacture those values or subtract from a version.
- Additional native blocker: `FoldersDomainServiceEndpoints.ToHttpResult(WorkspaceLockStatusQueryResult, ...)` omits `LockFence` from `LockLeaseMetadataResponse`, despite its presence in the supported SDK model. Missing/default fence must fail closed. A stubbed AcceptedCommand tests adapter forwarding only and cannot prove native Add/replay success.
- No new public API, owner changes, dependencies, persistence, lock acquisition/release or authentication changes. No intent gaps or irreversible actions. Story and sprint status files are out of scope.
- Required Aspire baseline attempted with `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`; exit 2, Tenants.Contracts CS0246/CS0103 for missing EventStore contract types/attributes. Log: `/tmp/chatbot-folders-aspire-start.log`. No running ChatBot resources available for baseline inspection.
- Server Debug source build succeeds with zero warnings/errors: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`. Log: `/tmp/chatbot-folders-baseline-build.log`.

- Implemented scoped owner observations and safe failure mapping in `FoldersFolderStore.cs`; extracted the recording client and call record to their own files, added a generated-client HTTP transport fixture, and expanded focused regressions. No authorization, owner SDK, or project-reference configuration was changed.
- The baseline focused class executed 9 tests: 2 passed, 7 failed with the required-observation exception. Evidence: `/tmp/chatbot-folders-baseline.ctrf.json`, `.trx`, and `.log`.
- Independent Blind Hunter review completed. All six findings were checked against the actual client/owner sources and addressed; no review findings remain deferred. Native API gaps below are preexisting implementation blockers, separately retained.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Uncaught observation 408/422/410 statuses | medium | Verified metadata timeout/path failures in FoldersDomainServiceEndpoints and generated lock contract. Patched safe transient/terminal mappings; expanded per-observation status tests. |
| Generated-client malformed HTTP 200 response escapes guards | medium | Verified ReadObjectResponseAsync wraps JsonException in HexalithFoldersApiException using the HTTP status. Generic safe retryable handling now catches it; real generated-client tests cover malformed metadata and lock responses. |
| Metadata 413 is incorrectly retryable | medium | Owner WorkspaceFileContextResultCode.ResponseLimitExceeded explicitly emits retryable:false. Added pre-upload terminal mapping while retaining the existing upload-413 retry contract; tests distinguish both. |
| Observation 409 falsely implies duplicate upload | medium | Neither read establishes a submitted duplicate. Pre-upload conflicts now return unevaluated, retryable evidence-unavailable; upload 409 preserves its existing mapping. |
| Cancellation tests do not assert forwarded token | low | Verified forced exception could mask a missing token argument. Recorder now captures each received token and every cancellation case asserts exact propagation. |
| Missing-fence guard lacks real HTTP fixture | medium | Owner LockLeaseMetadataResponse omits LockFence. Added a generated-client fixture reproducing that shape and asserting evidence-unavailable with exactly two reads and no upload. |


## Verification

- Final Debug source build passed with zero warnings/errors. Command: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`. Log: `/tmp/chatbot-folders-build.log`.
- Focused Folders: **66/66 passed**, zero failed/skipped. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-focused.ctrf.json -class 'Hexalith.ChatBot.Server.Tests.Adapters.Folders.*' -result-trx /tmp/chatbot-folders-focused.trx`. Output: `/tmp/chatbot-folders-focused.log`; CTRF checksum alongside the result.
- Then full Server: **3,421/3,421 passed**, zero failed/skipped. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-full-server.ctrf.json -result-trx /tmp/chatbot-folders-full-server.trx`. Output: `/tmp/chatbot-folders-full-server.log`; CTRF checksum alongside the result. Counts include the authorized uncommitted starting state.
- All 21 preexisting modified/untracked file hashes still match the starting snapshot. Scoped whitespace check passed with `git -c core.whitespace=cr-at-eol diff --check`. Index remains empty. No stage, commit, push, branch, dependency update or submodule mutation was performed.
- The seven recorded adapter-test failures are repaired, but native new-file Add and equivalent replay remain blocked by the owner absence/replay-precondition API gap and omitted HTTP lock fence. These two follow-ups are recorded in `deferred-work.md`; no passing stub is claimed as native persisted-state proof.
- Aspire baseline remains blocked by the exact startup/build failure recorded above; the narrower requested Server build and both test lanes passed.
- This repair stays in review while native owner support is unresolved. Story 1.6 and its sprint entry remain `review`; accepted A6 protection and native-isolation evidence are still required. No completion or acceptance claim is made for Story 1.6.
