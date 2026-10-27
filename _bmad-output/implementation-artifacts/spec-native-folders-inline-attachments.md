---
title: 'Native inline attachment Add and replay through Folders'
type: 'feature'
created: '2026-10-26'
status: 'done'
baseline_commit: 'f75d1ded32b1e55dbb4f9f7d8c1148fd12c64e7e'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Enable native inline attachment Add/replay using authoritative Folders evidence, task-owned leases, SDK canonical keys and confirmed publication/file identity. Preserve the existing frozen repair.

## Boundaries & Constraints

Always retain current authorization, policy, tenant and fingerprint checks. Recover accepted replay before requiring a lease; fresh Add requires proven absence, version zero and its task's fence. Keep Story 1.6/sprint in review; A6 protection/native isolation remain outstanding.

Never infer zero from 404, invent fences, recover original preconditions from current metadata, guess file IDs or equate acceptance with publication. No Git staging/commits/pushes, dependency updates, package publication or nested submodule initialization. Keep public v2 disabled and frozen contract spines unchanged.

**Approved decision (2026-10-26):** The user selected option 1 and directed implementation. Extend owner C6 narrowly with explicit native-content task completion after authoritative publication confirmation. Atomically record completion, close the task and release its identified lease; preserve bytes and require a higher fence for the next task. Completion is idempotent and recoverable after response loss/lease expiry, cannot clear another/newer holder, and rejects unresolved preparations or provider-side staged work. Do not synthesize provider CommitSucceeded. Keep provider commit semantics unchanged. This separate follow-up authorizes the new owner APIs/lifecycle and adapter changes excluded by the earlier repair; its frozen intent remains untouched. Proceed without another approval checkpoint.

</frozen-after-approval>

## Code Map

Owner paths are relative to `references/Hexalith.Folders`; ChatBot paths use this root.

- Owner `src/Hexalith.Folders.Server/Content/WorkspaceContentStateReader.cs`, `WorkspaceContentAuthority.cs`: authoritative state/replay; reuse final authorization/policy/source checks.
- Owner `src/Hexalith.Folders/Aggregates/Folder/FolderState.cs`: ContentPublications and IdempotencyFingerprints; binding ExpectedVersion means aggregate head, not file version.
- Owner `docs/exit-criteria/c6-transition-matrix-mapping.md`, `c7-lock-authorization-timing.md`: governed lifecycle/timing.

## Tasks & Acceptance

**Execution:**

- [x] Owner `src/Hexalith.Folders.Contracts/openapi/hexalith.folders.native.v1.yaml`, `src/Hexalith.Folders.Client/nswag-native.json` and Client csproj: additive evidence/lease contract, reproducible native generation/drift checks; preserve SDK canonical helpers.
- [x] Owner `src/Hexalith.Folders.Server/Content/WorkspaceOperationEvidence.cs` and server registration: authorized absence/accepted-key queries, original version/fence/fingerprint, publication/incarnation and final source/policy rechecks.
- [x] Owner `src/Hexalith.Folders/Aggregates/Folder/` lease/completion command/event/service files, C6 documentation and `src/Hexalith.Folders.Server/FolderDomainProcessor.cs`: acquire/renew/release under the approved native-completion lifecycle; owner-issued ownership proof, C7 timing, same-fence renewal, no post-expiry renewal/takeover, safe cleanup.
- [x] Owner `src/Hexalith.Folders.Server/FoldersDomainServiceEndpoints.cs`: serialize LockFence in lock/transition responses and cover generated-client decoding; map native evidence/lifecycle endpoints.
- [x] ChatBot `src/Hexalith.ChatBot.Server/Adapters/Folders/AttachmentStorageIdentity.cs`, `FoldersFolderStore.cs`: versioned ASCII operation/path encoding, replay first, fresh lease lifecycle, native upload, publication confirmation and typed conflict mapping. Preserve folder/workspace addresses and SDK keys.
- [x] ChatBot `src/Hexalith.ChatBot.Server/Adapters/Folders/FoldersFolderStoreServiceCollectionExtensions.cs`, `src/Hexalith.ChatBot.Server/Program.cs`: opt-in external endpoint/tenant/dedicated token binding, destination/tenant guards and partial-configuration rejection; retain safety orchestration.
- [x] Owner `tests/Hexalith.Folders.Server.Tests/WorkspaceOperationEvidenceTests.cs`, ChatBot `tests/Hexalith.ChatBot.Server.Tests/Adapters/Folders/FoldersNativeAttachmentTests.cs` and test csproj: native HTTP/generated client/real processor/persisted events and content; Debug owner Server reference. Retain existing safe-failure regressions. Replace canned fixture acceptance with real processing.

**Acceptance Criteria:**

- Given authorized absence, when Add completes, then exact persisted bytes/hash and owner file ID match; consecutive attachments retain both publications.
- Given response loss, when replay follows lease release/expiry, then original version/fence/fingerprint recover one publication and incarnation.
- Given changed content/fingerprint, concurrent Add or stale fence, when submitted, then conflicts never imply duplicate success.
- Given forbidden/wrong-tenant requests, when evidence/lease/upload executes, then denial discloses no owner evidence and publishes nothing.

## Design Notes

Legacy replay computes its unchanged SDK key without serializing the invalid legacy mutation, then queries accepted-key evidence. Recover exact confirmed publication without renaming. Unavailable/conflicting/ambiguous legacy evidence blocks fallback; fresh encoding requires authoritative absence at both legacy and new target paths. Preserve original task/path/content semantics. New identity excludes content hash; changed bytes conflict through the SDK fingerprint.

## Implementation Notes

Implemented against ChatBot HEAD `f75d1ded32b1e55dbb4f9f7d8c1148fd12c64e7e`, Folders HEAD `2472a50027e8e4f6581b0690828ea9a14dfa9018` and root-declared EventStore source HEAD `57971a202563b674c90384f55f7d4dc4b995fbaa`. No staging, commits, pushes, dependency/version updates, publication or submodule initialization performed.

- Added a native v1 evidence/lease/Add companion contract and reproducibly generated client, with a typed accepted replay decision. Existing v2 spine, generated canonical helpers and frozen repair remain unchanged.
- Added independently authorized accepted-key evidence and live-task absence proof. Replay retains exact SDK key, source fingerprint, original version/fence, confirmed receipt and owner incarnation. Missing historical key evidence fails closed. A narrow fixed-prefix SHA256 resolver maps public colon folder/workspace addresses to canonical owner addresses before tenant/ACL/source operations without changing shared identifier grammar.
- Added owner-issued native lease proof, same-fence renewal, clean release and explicit completion, with both lock and mutation authority and C7 ceilings. Completion records one source event, closes only its task and releases only its lease; it never emits provider CommitSucceeded. Operational lock projections fold the native events and generated lock/transition responses now carry the actual fence.
- Added an additive default-fail-closed EventStore content seam: completion CAS-seals a task against racing Upload/Prepare; clean release CAS-quiesces only the current fence so a transient pre-upload failure can retry on a higher owner fence. Upload fences are checked against current owner state, preventing stale/future asserted admission. Unresolved preparation, unaccounted upload and provider staging reject cleanup/completion.
- Added the opt-in dedicated endpoint/tenant/token ChatBot binding and native adapter branch. It performs legacy/native replay lookup before acquisition, proves both target paths absent, uploads through unchanged SDK construction/key helpers, confirms exact publication, and completes before returning the real owner file ID. Cancellation performs bounded safe cleanup; native contention and typed fingerprint/target conflicts do not imply duplicate admission.
- Added generated-HTTP/real-processor tests with CAS-persisted retained source events and the real SDK content ledger/bytes. Source references are resolved consistently to root-declared siblings. Test-only UniqueIds source selection excludes the unchanged pinned package assets to avoid mixed runtime assembly identities. This fixture is persisted test-store evidence, not production Dapr/native backend isolation qualification.

Pre-code exact-target Aspire start, readiness, describe and stop all exited 0; describe recorded 44 Running/Healthy resources and only the run-owned ChatBot AppHost was stopped. Evidence: `/tmp/chatbot-native-parent-0fdygy9q/aspire-{ps,start,wait,describe,stop}.*`. Implementation evidence: `/tmp/chatbot-native-implementation-RB2Akn`.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / disposition |
| --- | --- | --- | --- |
| B1 native command IDs | high | patch | `native_<hex>` violates the real gateway MessageId validator. Use the existing canonical generator and cover admission. |
| B2 package-only Release | medium | defer | Confirmed seven errors against unchanged Client 1.3.1; publication/pin changes are explicitly excluded. Retain exact blocker, do not claim shipping qualification. |
| B3 asynchronous lease acknowledgement | high | patch | Polling observes prior holder/closure/released-fence state without the submitted command's decision. Match causation, or validate an exact retained idempotent completion/release before submission. |
| B4 final lease source recheck | medium | patch | Validate/idempotent acquire reauthorize after reading source and return the old head. Re-read after authorization and reject changed head/ownership. |
| B5 SDK seal authorization | high | patch | Seal/quiesce currently authorize `Read` with only scope. Existing owner endpoints separately require mutation and lock rights, but the new SDK seam lacks target-specific mutation authority. Bind sealing to task/fence and a mutation decision; add denial coverage. This corrects the existing seal capability. |
| B6 native generation drift | medium | patch | Verify depends on generation into stored output and ordinary compilation regenerates it, violating owner guidance. Keep explicit generation; verify stored output against a temporary file without rewriting it, including a negative drift check. |
| B7 malformed required input | medium | patch | `{}` reaches `request.OperationId.Length`, throws null reference and returns 503. Validate required strings/path members and operation-specific lease fields before use, preserving authorization-first denials. |
| B8 generic Add schema/error documentation | low | reject | Generic JObject is deliberate reuse of the existing SDK canonical mutation builder; the supported adapter validates through that builder and the owner validates actual mutation shape. A second native mutation DTO duplicates the canonical shape. Contract error documentation is limited, but no demonstrated runtime failure follows from this schema choice; expanding it is not needed for this bounded path. |
| B9 malformed native retained events | false | reject | Supported commands allocate nonempty lock IDs, validate 1..3600 seconds, construct exact timestamps and derive completion IDs only from a confirmed publication. The cited malformed events cannot be emitted by the reachable command path. Existing lock-event fold also trusts these generated fields; no demonstrated new reachable fault. |
| B10 complete C7 revocation/stale runtime | medium | defer | Existing C7 explicitly leaves the revocation handler and exact stale-query runtime qualification reference-pending. New native operations deny protected work on every current authorization and maintain the approved interval; broader status propagation remains unqualified rather than implied by profile validation. |
| B11 recorded expiry recovery | high | patch | A retained `LockLeaseExpired` event moves native state to dirty while preserving its exact lease/publication. Completion currently rejects it although clock-only expiry succeeds. Permit narrowly proven native expiry recovery with no provider/unresolved work; test actual retained expiry. |
| E1 native trusted adapter absent | high | patch | Keyed admission resolves through the exact-set adapter registry; ChangeNativeTask is missing. Register its internal canonical intent adapter and exact-set catalog entry and cover gateway admission. |
| E2 conflicting completed replay | high | patch | Verified same polling defect as B3: prior closure can acknowledge a conflicting fingerprint before asynchronous rejection. Preserve exact retained replay identity. |
| E3 lost acquire response then expiry | medium | patch | No accepted publication exists, so adapter reaches acquire; expired own native holder prevents acquire and the caller lost the lease needed for release. Recover only its exact clean owner lease, quiesce it and reacquire higher fence; never clear another holder/staged work. |
| E4 streamed/oversize native Add | medium | patch | Native Add delegates to the general 1MiB/streamed mutation handler while native evidence is bounded to 256KiB. Reject non-inline/oversize requests before upload. |
| E5 seal under read authority | high | patch | Verified same SDK authorization defect as B5; scope-only read authorization does not identify the sealed task or released fence. |
| V1 maintenance failure cancellation gap | medium | patch | Pre-verified reviewer regression gap: current tests cover successful renewal/caller cancellation/initial failures, not failed maintenance canceling pending Add. Add controlled-clock denial and bounded retryable completion/no-publication assertion. |
| V2 native gateway bearer forwarding | high | patch | Human bearer handler permits historical command routes or content stream/status reads; native command POST is neither. Permit only independently authorized native submission and test actual handler chain. Fixture replaces the gateway and missed this boundary. |
| O1 source-only integration compilation | medium | patch | Native integration test unconditionally uses FoldersOwner alias/shared fixtures while references exist only in source mode. Explicitly scope that new in-process owner proof to Debug source qualification; retain package-compatible unit tests and record separate package/runtime lane. |
| O2 required generated proof fields | medium | patch | Native generator marks required properties DisallowNull, allowing omission to default version zero or replay false. Enforce required members in native generated decoding and cover omitted version/fence/replay bit fail-closed behavior. |

Groups after individual verdicts: B3/E2 share asynchronous decision confirmation; B5/E5 share seal authorization. All other patch entries have independent causes. No frozen-intent change or new externally callable operation is required; these corrections complete already approved native boundaries. Package publication and full C7 runtime qualification remain deferred under the original intent.

## Verification

Build individual projects in Debug: `-p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`, with sibling source roots for owner builds. Run focused owner evidence/lease/client checks, focused ChatBot Folders, then full Server via `.github/scripts/run-xunit-v4.sh`, zero skips, single-dash selectors and `-result-trx`. Retain commands/results, logs, CTRF/TRX, verified SHA256 and exact blockers; use baseline fallback checks. Distinguish persisted test-store evidence from native backend/isolation qualification. Report SDK publication/catalog and package-only Release qualification separately; never publish or change pins to mask blockers.

### Approved implementation handoff details

- Workspace root: `/home/administrator/projects/hexalith/chatbot`; Folders owner is its root-declared `references/Hexalith.Folders` checkout. Follow each owning repository AGENTS, allowed root baseline, build/editor/test guidance. Never load skills below references. Preserve all existing work; no Git mutations or dependency/version changes.
- The completed operation lookup is authorized independently of an active lease; compare exact original task, operation, canonical fingerprint and content/path semantics. Missing lookup alone never proves fresh target absence. Legacy keys exclude current preconditions; compute using existing generated canonical helpers without mutation serialization, then query authoritative accepted state. Validate actual legacy and new target absence before selecting new encoding.
- Use supported native v1 HTTP, a reproducibly generated companion SDK and existing FileUpload canonical mutation/key helpers. Existing generated Client targets disabled v2; do not enable v2 or use canned AcceptedCommand responses as native proof. LockFence must be present in both existing HTTP lock response mappings and covered through generated decoding.
- New evidence/lease/completion operations need current owner tenant/task authorization, file policy and sensitivity checks, bounded input/output, safe problems, cancellation and final current-source/policy rechecks after awaits. Fresh absence evidence requires the owning task's live lease; accepted recovery preserves original preconditions/fingerprint after release or expiry.
- Native completion uses source-confirmed SDK publication and no unresolved/native-vs-provider ambiguity; explicit completion cannot mark unrelated staged work clean. Renewal preserves fence and rejects expired leases. C7 renewal is due within 30 seconds, authority revalidation within 15 seconds and fresh before every renewal/mutation; preserve valid shorter tenant ceilings and short lease expiry.
- Keep lease lifecycle inside IFolderStore behind the adapter; coordinator authorization/safety behavior remains. Runtime currently defaults to UnavailableFolderStore, so opt-in configured native binding is required. Existing Projects/Memories credential acquisition rejects the Folders tenant/multi-permission shape: preserve it; use dedicated externally configured Folders endpoint, tenant and static ApiToken with destination/tenant guards. Do not provision or widen identities.
- Tests: add real native HTTP/generated SDK + real FolderDomainProcessor execution with persisted authoritative source events and SDK body state, asserting exact bytes/file IDs/publication counts. Cover release and expiry replay, completion crash recovery, consecutive attachments, identity/Unicode filenames, legacy replay, changed content/fingerprint, concurrent Add, stale fences, foreign tenant/forbidden path, lease lifecycle and cleanup cancellation. Existing WorkspaceContentProductionFixture gateway is canned; replace/extend it for fresh-Add proof. Distinguish persisted test-store evidence from actual backend/native-isolation qualification.
- Capture new evidence directory; run owner-focused checks first, then build `tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false`, run focused Folders and then full Server via `.github/scripts/run-xunit-v4.sh` with CTRF, TRX, logs and SHA256. Owner build roots must point at root-declared sibling EventStore/Commons/Tenants/Memories/etc sources, never nested submodules. Keep exact blockers and use fallback ladder. Attempt required Aspire baseline through the allowed lifecycle skill before code; stop only resources started by this run.
- Source validation and SDK publication/package-only Release qualification are separate. Report pinned 1.3.1 incompatibility and any package-only Release blocker separately; do not publish packages. Retain Story 1.6/sprint review and A6/native-isolation acceptance boundary. Append evidence/dispositions to this spec/deferred ledger without rewriting the historical frozen repair.

### Executed implementation qualification (2026-10-26)

Evidence root: `/tmp/chatbot-native-implementation-RB2Akn`; `commands.json` records exact commands, owning working directories and results. Builds ran sequentially with `--configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nodeReuse:false` and root-declared sibling source resolution. Final EventStore content, owner Server/Client and ChatBot Server test-project builds passed with zero warnings/errors.

| Check | Result | Evidence basename |
| --- | --- | --- |
| SDK content ledger, unresolved work, concurrent Upload/Prepare seals and quiesced fences | 22/22, zero skips | `sdk-content` |
| Owner native evidence/lease/publication, current permissions, stale/future fence, fingerprint, timing and completion | 23/23, zero skips | `owner-native-final-4` |
| Existing workspace-lock and transition HTTP generated fence decoding | 2/2, zero skips | `owner-existing-decode` |
| Legacy canonical key equality/Unicode metadata hashing golden | 1/1, zero skips | `owner-client-final` |
| Native companion regeneration/hash drift | Passed | `native-generation-final-verify.log` |
| Focused ChatBot Folders, including unchanged failure regressions and native runtime | 117/117, zero skips | `final-focused-2` |
| Full unfiltered ChatBot Server, runner `-parallel none` | 3,472/3,472, zero skips | `final-full-server-serial` |

The initial default-parallel full run completed 3,471/3,472 with zero skips; the sole failure was existing `ChatBotMetricsTests.AllOperationalInstrumentsPlusGapCounterAreRegisteredOnTheChatBotMeter` observing duplicate process-global meter registrations during concurrent host tests. Its unmodified assertions pass in the complete serial rerun. Both results are retained (`final-full-server.*` and `final-full-server-serial.*`); no tests were skipped or narrowed for the full rerun.

The separate package-only command `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -nodeReuse:false` exits 1 with seven compiler errors and zero warnings: the restored unchanged `Hexalith.Folders.Client/1.3.1` has no native namespace/client/lease types. Exact output: `package-only-release.log`; package assets confirmed the 1.3.1 pin. SDK publication/catalog qualification remains open, with the prior frozen repair package-observation incompatibility still recorded. No pin or publication changed to conceal this blocker.

Final evidence and changed-source SHA256 manifests are retained with `sha256sum -c` verification in the evidence directory. Earlier failed graph/runtime/protocol attempts are preserved separately. Source success proves the persisted test-store protocol and content behavior only; production Dapr CAS/native backend isolation, A6 protection, Story 1.6/sprint acceptance and owner SDK publication remain outstanding. Independent implementation review is owned by the parent workflow.

### Review correction evidence (2026-10-26)

The correction pass retains the original implementation evidence and all failed attempts. Evidence root: `/tmp/chatbot-native-corrections`; exact commands/working directories/results are in `commands.json`. No full Server lane or broad ChatBot Folders lane was run during this correction pass; the parent workflow owns complete requalification.

- B1/E1/V2: native lifecycle submissions use the existing canonical message generator. An internal ChangeNativeTask canonical intent adapter and exact catalog entry cover keyed admission. The existing gateway human-bearer handler admits a native submission only when it matches the exact independently authorized request (message, tenant, target, task, command, key, correlation and payload). Production mutation validation recognizes the bounded native routes for their mutation/lock actions. The fixture now invokes the actual process request handler from a workload principal using a signed human receipt, rather than carrying the public HTTP principal into the processor.
- B3/E2/B4: queued command acknowledgement requires its exact causation-bound decision. Retained completion/release replay validates exact original task/publication/key/fingerprint/fence/lock/proof before returning, independently of a newer holder. Validate, idempotent acquire, matched normal polling and retained replay all recheck authoritative head after final authorization; policy is checked again before returning. Tests cover queued renew, conflicting completed replay with a queue-only gateway, and source release during final authorization in both observation and normal acquisition polling.
- B5/E5: the existing SDK seal capability now authorizes Prepare with a task/fence-bound ContentTaskSealRequest. The owner demands current mutate_files and lock_workspace authority plus the exact native holder/fence, including after expiry. Read-only, foreign-task and future-fence SDK/owner calls cannot seal or quiesce state.
- B6/O2: ordinary compilation does not generate native artifacts; generation is explicit. Verify compares the stored artifact with temporary regeneration without a generation dependency. A deliberate stale stored artifact fails verification with identical stored SHA256 before/after, then the restored artifact passes. Native required DTO members are enforced on decoding; omitted original version, fence/completion flag or accepted replay decision cannot become default zero/false.
- B7/E4: required strings/path members and operation-specific ownership/completion fields are validated after authorization and before use. Empty/null bodies return bounded validation and unauthorized selectors remain safe denials. Native Add rejects streamed/raw-stream and content beyond 256KiB before SDK ingress; tests include valid persisted streamed staging, exact hash/length and no additional stored body/publication.
- B11/E3/V1/O1: exact native LockLeaseExpired dirty state can complete/release with confirmed publication/no unresolved or provider work. Lost-acquire recovery quiesces only the same task's exact clean expired fence, records release and reacquires higher fence in one owner command; staged work and another task reject recovery. Controlled-clock tests prove failed scheduled authority observation cancels a blocked Add promptly and returns retryable without publication. The new in-process owner test class is explicitly excluded from package mode, preserving existing package-compatible tests; SDK publication and package/native runtime qualification remain separate.

| Focused correction lane | Result |
| --- | --- |
| EventStore SDK content and seal authorization | 28/28, zero skips |
| Folders owner native/receipt/bearer checks | 64/64, zero skips |
| Real EventStore validator, keyed native registry and exact catalog | 8/8, zero skips |
| Native required-member decoders and canonical legacy-key golden | 5/5, zero skips |
| Edited ChatBot native attachment class | 29/29, zero skips |
| Native positive and negative drift verification | Passed; negative run intentionally exits 1 without changing stored bytes |
| Package test compile-item evaluation | New in-process proof excluded; existing tests retained |

Complete source dependency builds passed with zero warnings/errors before test-only recompilations for fixture/test adjustments. Earlier failed compile/test attempts are preserved with their exact results. SHA256 evidence/source manifests and successful checksum verification are retained in the correction directory. Frozen intent, historical repair, Story 1.6, sprint, dependency pins and package publication remain unchanged; B2/B10 and production native backend/A6 acceptance remain deferred as triaged above.


### Final parent qualification and review disposition (2026-10-26)

The parent read the complete cross-repository diff, verified tasks/acceptance against code and persisted-state assertions, ran the three independent review layers, recorded every finding above, and inspected the corrections. All retained source implementation findings are corrected. B2 (SDK/package shipping qualification) and B10 (complete C7 propagation/stale runtime qualification) remain deferred as recorded. No frozen intent or protected Story 1.6/sprint content changed.

Final evidence root: `/tmp/chatbot-native-review-u5g9_rps`; `commands.json` retains exact commands, working directories, exit codes and logs. Final Debug source-reference test-project build passed with zero warnings/errors. The required sequence ran owner checks before focused ChatBot Folders, then the unfiltered full Server project.

| Final check | Result | Evidence basename |
| --- | --- | --- |
| SDK content/seal authorization | 28/28, zero skips | `sdk-content` |
| Owner evidence/lease, signed process receipt, native bearer and authorization | 64/64, zero skips | `owner-native` |
| Existing lock/transition generated fence decoding | 2/2, zero skips | `owner-fence` |
| Actual EventStore validator, native trusted intent and exact catalog | 8/8, zero skips | `owner-admission` |
| Required native decoding and preserved canonical legacy key | 5/5, zero skips | `owner-client` |
| Native generated-artifact verification | Passed, no stored-file rewrite | `native-generation` |
| Complete ChatBot Folders | 121/121, zero skips | `chatbot-folders` |
| Full unfiltered Server, `-parallel none` | 3,476/3,476, zero skips | `chatbot-full` |

The initial parent attempt combined class and method selectors, which intersect in this runner and selected zero tests. The zero-test gate correctly exited 1; exact command and empty CTRF/TRX remain as `owner-focused.*`. Separate class and method lanes above both passed. The original default-parallel full-project metrics race and its exact 3,471/3,472 result remain retained in the original implementation directory; no metrics assertions were changed and the final full run did not omit tests.

SDK publication/catalog qualification remains open: no packages were published or pins changed. Final package-only Release was attempted separately with `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -nodeReuse:false`; exit 1, seven errors, zero warnings, because restored Folders.Client 1.3.1 lacks the native companion namespace/types. Exact final output is `package-only-release.log`; assets confirm the unchanged 1.3.1 package. The source-only in-process owner proof is explicitly separate from future package/native-runtime qualification.

Final `source.sha256`, `SHA256SUMS`, CTRF, TRX, logs and preservation audit are retained in the parent evidence directory with successful checksum verification. Earlier implementation/correction manifests remain immutable snapshots; the final source manifest supersedes them for the final tree. Whitespace checks pass with CRLF recognized per repository editor configuration. Protected repair/Story/sprint hashes, full HEADs, empty staged diffs, frozen v1/v2 contract spines and canonical SDK helpers were rechecked. No staging, commit, push, package publication, dependency update or nested submodule initialization/update was performed. Native backend/Dapr isolation and A6 protection remain unaccepted; Story 1.6 and sprint remain in review.
