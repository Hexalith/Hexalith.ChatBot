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


| R2-B1: Pinned Folders package lacks the observed-version/fence APIs | high | Confirmed the shared catalog pins 1.3.1 and reproduced CS1061 for LockLeaseMetadata.LockFence and FileUploadDescriptor.ExpectedFileVersion/ExpectedLockFence against that cached package; exact compiler command/output is in /tmp/chatbot-folders-review-gsvpxxxd/package-compatibility.log. Deferred owner SDK publication/catalog coordination under this intent's explicit Debug source-reference validation scope; package compatibility is not claimed. |
| R2-B2: Fresh Add has no admissible absence evidence | high | WorkspaceContentAuthority.ValidateIngressAsync requires an absent target and expectedVersion zero; current visible metadata cannot provide it. Deferred the preexisting owner evidence gap, retaining safe rejection and no invented zero. |
| R2-B3: Owner lock HTTP response omits its fence | high | FoldersDomainServiceEndpoints constructs LockLeaseMetadataResponse without LockFence. Deferred the preexisting owner response gap; the generated-client missing-fence regression remains authoritative. |
| R2-B4: Current evidence cannot recover accepted replay | high | Owner replay checks the original fence and publication version minus one before active-lease checks; current observations cannot reconstruct those preconditions. Grouped with R2-B2 for the owner-issued operation/absence evidence follow-up, retaining original replay identity and no fabricated preconditions. |
| R2-B5: Attachment task lease has no lifecycle owner | high | AttachmentCaptureCoordinator calls the adapter without acquiring/renewing its task-specific lease; the adapter only observes it. Deferred the preexisting orchestration gap; this observation repair does not acquire or release locks. |
| R2-B6: Upload concurrency conflicts appear as duplicate-pending | medium | The owner maps ContentConflictException to concurrency_conflict, while the unchanged upload-409 branch labels every conflict duplicate-pending. Deferred the preexisting classification defect; pre-upload observation conflicts already remain not-evaluated. |
| R2-B7: Recording client infers replay from upload count | low | A second unrelated attachment was labeled a replay by the new fixture. Patched to configure IdempotentReplay explicitly and added a distinct-attachment regression; no owner-admission behavior is simulated. |
| R2-E1: Response-body IOException escapes the adapter | false | Client.ResponseCapture.Initialize enables ReadResponseAsString; response buffering wraps I/O errors in HttpRequestException, already safely handled. Generated-client metadata and lock stream-failure probes both pass without a production I/O catch and assert the failing stream was actually read. |
| R2-V1: Dependency timeout coverage misses an uncanceled caller | medium | Accepted the verification review's demonstrated mutation gap. Added metadata, lock and upload TaskCanceledException cases with an uncanceled caller token, exact forwarded tokens, safe retryable metadata and no later calls. |
| R2-P1: Generated request validation escapes safe failure handling | medium | The generated-client probe reached Oq2RequestValidation.ValidateMutation and threw JsonSerializationException wrapped in TargetInvocationException for the existing colon-bearing operation ID; path validation likewise rejects filenames containing spaces/non-ASCII characters before transport. Patched the exception mapping to terminal folders_request_unavailable and added real generated-client regressions; identity/path contract alignment remains a separate follow-up because silently changing identities would violate stable replay. |



### Third Review Triage (2026-10-25)

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| R3-B1: Positive metadata cannot admit first-time Add | high | carried R2-B2: the metadata guard and Add mutation are unchanged; the preexisting owner absence-evidence gap remains deferred. No zero is inferred. |
| R3-B2: No attachment-task lease lifecycle | high | carried R2-B5: the adapter still only observes the current holder; acquiring/renewing leases remains the previously deferred orchestration gap. |
| R3-B3: Native lock response lacks a fence | high | carried R2-B3: the missing-fence HTTP contract and regression are unchanged; keep the existing owner follow-up without duplicating it. |
| R3-B4: Generated-client identity/path incompatibility | high | carried R2-P1 follow-up: ordinary uploads and space/Unicode paths still fail generated validation safely. The stable-identity migration remains deferred. |
| R3-B5: Replay after lease release/expiry cannot recover | high | carried R2-B4: unchanged current-observation requirements cannot recover the original replay preconditions; retain the existing owner operation-evidence follow-up. |
| R3-B6: Upload 409 is always labeled duplicate-pending | medium | carried R2-B6: unchanged upload conflict classification remains the previously deferred issue. |
| R3-B7: Undocumented 405/415/501 responses retry forever | maybe-false | The generated client uses the correct fixed routes/media types, and the current owner endpoints do not emit these statuses. No reachable production response was demonstrated; a transport/deployment capture of one of these codes would settle this unverified medium claim. Defer without adding speculative status branches. |
| R3-B8: Plain dependency cancellation escapes | medium | A plain OperationCanceledException with an active caller token bypasses the caller-cancellation filter and the TaskCanceledException-only fallback. Patched the fallback to cover the base type; both exception forms, generated-client cancellation and caller cancellation propagation pass. |
| R3-B9: Locally calculated file ID differs from owner identity | high | WorkspaceFileMutationService allocates a fresh owner ID for Add, while the unchanged AttachmentStorageIdentity.FileIdFor hashes the operation ID. This preexisting reference-binding defect must be resolved through owner-confirmed publication evidence; copying metadata from an existing path would bind the wrong incarnation. Defer without changing stable identities here. |
| R3-E1: Plain dependency cancellation escapes | medium | Same demonstrated failure as R3-B8; group under that patch while retaining this independent finding row. |
| R3-E2: Content-hash validation escapes mutation building | medium | FileUpload.BuildInlineFileMutation throws ArgumentException with ParamName contentHashReference for mismatched bytes/hash; MailboxAttachmentContentResult accepts that pair and the adapter does not map the failure. Patched only this demonstrated SDK validation error to terminal safe metadata; the regression proves the hash is not substituted and no upload occurs. |

The verification-gap reviewer reported no gaps. All three prescribed layers completed before triage. Review input: `/tmp/chatbot-folders-review-r3-evlc9n2w/review.diff`, scoped from canonical baseline `0468d19e56752bb70add6a8f885cfa8de0124260`; resumed HEAD is recorded in `starting-head.txt` beside it.


## Verification

- Final Debug source build passed with zero warnings/errors. Command: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`. Log: `/tmp/chatbot-folders-build.log`.
- Focused Folders: **66/66 passed**, zero failed/skipped. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-focused.ctrf.json -class 'Hexalith.ChatBot.Server.Tests.Adapters.Folders.*' -result-trx /tmp/chatbot-folders-focused.trx`. Output: `/tmp/chatbot-folders-focused.log`; CTRF checksum alongside the result.
- Then full Server: **3,421/3,421 passed**, zero failed/skipped. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-full-server.ctrf.json -result-trx /tmp/chatbot-folders-full-server.trx`. Output: `/tmp/chatbot-folders-full-server.log`; CTRF checksum alongside the result. Counts include the authorized uncommitted starting state.
- All 21 preexisting modified/untracked file hashes still match the starting snapshot. Scoped whitespace check passed with `git -c core.whitespace=cr-at-eol diff --check`. Index remains empty. No stage, commit, push, branch, dependency update or submodule mutation was performed.
- The seven recorded adapter-test failures are repaired, but native new-file Add and equivalent replay remain blocked by the owner absence/replay-precondition API gap and omitted HTTP lock fence. These two follow-ups are recorded in `deferred-work.md`; no passing stub is claimed as native persisted-state proof.
- Aspire baseline remains blocked by the exact startup/build failure recorded above; the narrower requested Server build and both test lanes passed.
- This repair stays in review while native owner support is unresolved. Story 1.6 and its sprint entry remain `review`; accepted A6 protection and native-isolation evidence are still required. No completion or acceptance claim is made for Story 1.6.

## Resumed Review (2026-10-25)

- Resumed the existing in-review spec with all three prescribed independent reviewers; triaged each finding before grouping. The scoped review diff is `/tmp/chatbot-folders-review-gsvpxxxd/review.diff`.
- During initial inspection, another session committed the starting staged changes as `f6e263a6ee2cdd14cc4c65368f0cff9fa98ef8c5`, then advanced the Platform gitlink in `25a269f450f46112b0ebf245977cf96260944b57`. This run made no Git mutations and preserved those changes.
- Patched generated request-validation failure handling and the recording fixture's replay control. Added generated-client request/response failure coverage and transport timeout coverage. Original owner authorization, observations, fences and canonical SDK keys are unchanged.
- The initial upload probe reproduced the escaping JsonSerializationException before the production fix: `/tmp/chatbot-folders-review-gsvpxxxd/stream-regression-before.{ctrf.json,trx,log}` (two observation cases passed, upload case failed before transport). The refined response-stream probe proved the I/O claim false: `/tmp/chatbot-folders-review-gsvpxxxd/stream-regression-confirmed.{ctrf.json,trx,log}` (2/2 passed with ReadAttempted asserted).
- Current Aspire baseline command: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`, exit 2. The new blocker is `references/Hexalith.Platform/src/Hexalith.Platform.Custody/SecurityEventRecorderLookup.cs(7,133): error CS0246: SecurityEventNoRecordProof could not be found`; a transient MSB3026 copy warning also appears. Evidence: `/tmp/chatbot-folders-review-gsvpxxxd/aspire-start.log`. No ChatBot AppHost started; unrelated running AppHosts were left alone.
- Native attachment success, package-mode compatibility and Story 1.6 acceptance remain unproven; keep this repair and Story 1.6 in review.

### Resumed Verification

- Final Debug source build passed, zero warnings/errors: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`. Log: `/tmp/chatbot-folders-review-gsvpxxxd/final-build-2.log`.
- Focused Folders passed **75/75**, zero failed/skipped: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-review-gsvpxxxd/final-focused.ctrf.json -class 'Hexalith.ChatBot.Server.Tests.Adapters.Folders.*' -result-trx /tmp/chatbot-folders-review-gsvpxxxd/final-focused.trx`. Log and SHA256 accompany the CTRF result.
- Then full Server passed **3,430/3,430**, zero failed/skipped: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-review-gsvpxxxd/final-full-server.ctrf.json -result-trx /tmp/chatbot-folders-review-gsvpxxxd/final-full-server.trx`. Log and SHA256 accompany the CTRF result.
- The initial direct JsonException catch did not handle the generated callbacks' reflection wrapper; the focused regression demonstrated three escaping exceptions. `/tmp/chatbot-folders-review-gsvpxxxd/serialization-probe.log` independently confirms TargetInvocationException wrapping JsonSerializationException. The final filter handles only direct JSON validation errors and that specific wrapper; all three real-client regression cases now pass.
- `git -c core.whitespace=cr-at-eol diff --check` passes. The index remains byte-for-byte identical to the resumed-run snapshot, and the final source hashes are recorded in `/tmp/chatbot-folders-review-gsvpxxxd/final-source-hashes.json`. This run staged, committed, pushed, reverted and updated no dependencies/submodules. Story and sprint files remain untouched.
- Presentation retains `in-review` because native owner support remains unresolved, as already recorded by this spec. The frozen intent prohibits staging, committing and pushing, so the workflow's generic commit step is not applied.


## Third Review Result (2026-10-25)

- Completed all three prescribed independent review layers and recorded every finding before grouping. Six findings retained their prior verdicts/routes; two verified failure paths were patched. The owner-allocated file-reference defect and an unverified protocol-status claim were added to the deferred ledger. The verification-gap layer reported no additional gaps.
- `FoldersFolderStore` now maps noncaller `OperationCanceledException` (including its `TaskCanceledException` subtype) to safe retryable metadata while retaining caller cancellation propagation. The SDK's specific `ArgumentException` for a mismatched `contentHashReference` now produces a terminal request-unavailable result without uploading or altering identity/hash evidence. Existing JSON-validation handling remains intact.
- Regression evidence before the production patch: **15 tests, 9 passed, 6 failed**, zero skips. The three plain-cancellation proxy cases, two real generated-client HTTP cancellation cases and mismatched-hash case failed with the exact escaping exceptions. Command: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-review-r3-evlc9n2w/regression-before.ctrf.json -method '*StoreMailboxAttachmentShouldMapDependencyTimeoutToRetryable' -method '*StoreMailboxAttachmentShouldPropagateCallerCancellation' -method '*StoreMailboxAttachmentShouldMapHttpDependencyCancellationToRetryable' -method '*StoreMailboxAttachmentShouldRejectMismatchedContentHashWithoutUploading' -result-trx /tmp/chatbot-folders-review-r3-evlc9n2w/regression-before.trx`. Exit 1; log alongside the result.

### Third Review Verification

- Final Debug source build passed with **zero warnings/errors**: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`. Exit 0; `/tmp/chatbot-folders-review-r3-evlc9n2w/final-build.log`.
- Focused Folders passed **84/84**, zero failed/skipped: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-review-r3-evlc9n2w/final-focused.ctrf.json -class 'Hexalith.ChatBot.Server.Tests.Adapters.Folders.*' -result-trx /tmp/chatbot-folders-review-r3-evlc9n2w/final-focused.trx`. Exit 0; log and SHA256 alongside the CTRF result.
- Then full Server passed **3,439/3,439**, zero failed/skipped: `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-folders-review-r3-evlc9n2w/final-full-server.ctrf.json -result-trx /tmp/chatbot-folders-review-r3-evlc9n2w/final-full-server.trx`. Exit 0; log and SHA256 alongside the CTRF result.
- The previous Aspire startup blocker did **not** reproduce in the current workspace. `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 0; `aspire wait chatbot --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 0; `aspire describe --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` reported 44 resources, including 20 non-parameter resources all Running/Healthy. Evidence: `aspire-start.log`, `aspire-wait.log`, `aspire-describe.json` in the same review directory. This is startup evidence, not native attachment publication/isolation proof. The existing Platform working-tree changes were consumed without modification.
- Stopped only the ChatBot AppHost started by this run: `aspire stop --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`, exit 0 (`aspire-stop.log`). The preexisting FrontComposer AppHost was left alone.
- `git -c core.whitespace=cr-at-eol diff --check` passes. HEAD and the index match the starting snapshot; all preexisting files outside this run's four edited paths retain their starting hashes. `final-source-hashes.json` and the rewritten `review.diff` are in the review directory. No stage, commit, push, branch, dependency update or submodule mutation was performed. Story and sprint files remain untouched.
- Keep this repair `in-review` while native owner support is unresolved, consistent with its existing recorded acceptance boundary. Native Add/replay, owner file-reference binding, package-mode compatibility and Story 1.6 acceptance remain unproven. The frozen intent forbids staging/committing/pushing, so the generic workflow commit step is not applied.
